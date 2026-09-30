import os, re, sys, shutil, subprocess, threading, time, zipfile, urllib.request
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import parse_qs, urlparse

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
BUFFER_DIR = os.path.join(BASE_DIR, "buffer")
FFMPEG = os.path.join(BASE_DIR, "ffmpeg", "bin", "ffmpeg.exe")
PLAYLIST_TIMEOUT = 25
# No one fetching the playlist for this long means the player is gone.
IDLE_TIMEOUT = 30

QUALITY = {
    "240":  (240, "150k", "200k", "300k"),
    "320":  (320, "220k", "290k", "440k"),
    "360":  (360, "260k", "340k", "520k"),
    "480":  (480, "400k", "520k", "800k"),
    "576":  (576, "550k", "715k", "1100k"),
    "720":  (720, "900k", "1170k", "1800k"),
}

lock = threading.Lock()
proc = None
session = None
seen_sessions = set()
last_request = 0.0


def ensure_ffmpeg():
    """Download ffmpeg if it doesn't exist."""
    if os.path.exists(FFMPEG):
        return
    print("ffmpeg not found — downloading...")
    zip_path = os.path.join(BASE_DIR, "ffmpeg.zip")
    urllib.request.urlretrieve("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", zip_path)
    with zipfile.ZipFile(zip_path) as zf:
        zf.extractall(BASE_DIR)
    os.remove(zip_path)
    for name in os.listdir(BASE_DIR):
        if name.startswith("ffmpeg-") and name.endswith("-essentials_build"):
            os.replace(os.path.join(BASE_DIR, name), os.path.join(BASE_DIR, "ffmpeg"))
            break


def kill_stray_ffmpeg():
    """Kill any leftover ffmpeg from a previous run so they don't fight over the buffer."""
    subprocess.run(
        ["taskkill", "/F", "/IM", "ffmpeg.exe"],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
    )


def _kill():
    global proc, session
    if proc and proc.poll() is None:
        proc.kill()
        proc.wait()
    proc = session = None
    shutil.rmtree(BUFFER_DIR, ignore_errors=True)


def stop_stream():
    with lock:
        _kill()


def open_stream(url, quality, sid):
    """A new session kills whatever is running and starts a fresh ffmpeg; returns the playlist path once playable."""
    global proc, session
    h, vbr, maxr, bufs = QUALITY[quality]
    playlist = os.path.join(BUFFER_DIR, sid, "live.m3u8")

    with lock:
        started = sid != session
        if started:
            # A late request from an old session must not restart it over the new one.
            if sid in seen_sessions:
                return None
            seen_sessions.add(sid)
            _kill()
            os.makedirs(os.path.dirname(playlist))
            print(f"[{sid[:8]}] starting {quality}p: {url}", flush=True)
            # 2s keyframe-aligned segments so the first playlist is ready quickly; 180 x 2s keeps the 6-minute buffer.
            proc = subprocess.Popen([
                FFMPEG, "-i", url,
                "-c:v", "libx264", "-preset", "veryfast", "-tune", "zerolatency",
                "-force_key_frames", "expr:gte(t,n_forced*2)",
                "-b:v", vbr, "-maxrate", maxr, "-bufsize", bufs,
                "-vf", f"scale=-2:{h}",
                "-c:a", "aac", "-b:a", "128k",
                "-f", "hls", "-hls_time", "2", "-hls_list_size", "180",
                "-hls_flags", "delete_segments+program_date_time+independent_segments",
                "-hls_base_url", f"/{sid}/",
                playlist,
            ], creationflags=subprocess.CREATE_NEW_CONSOLE)
            session = sid
        my_proc = proc

    # A newer session kills this ffmpeg, which ends the wait too.
    deadline = time.monotonic() + PLAYLIST_TIMEOUT
    while time.monotonic() < deadline and my_proc.poll() is None:
        if os.path.exists(playlist):
            if started:
                print(f"[{sid[:8]}] playing", flush=True)
            return playlist
        time.sleep(0.25)
    if started:
        print(f"[{sid[:8]}] failed to start (ffmpeg exit code {my_proc.poll()})", flush=True)
    return None


def stop_when_idle():
    while True:
        time.sleep(5)
        if proc and time.monotonic() - last_request > IDLE_TIMEOUT:
            print("idle, stopping ffmpeg", flush=True)
            stop_stream()


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=BUFFER_DIR, **kw)

    def log_message(self, *a): pass

    def handle_error(self, request, client_address):
        exc_type = sys.exc_info()[0]
        if exc_type in (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            return
        super().handle_error(request, client_address)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_GET(self):
        global last_request
        last_request = time.monotonic()
        try:
            parsed = urlparse(self.path)
            if parsed.path == "/live.m3u8":
                self.serve_live(parse_qs(parsed.query))
            else:
                super().do_GET()
        except (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            pass

    # GET /live.m3u8?session=&quality=&url= ; a session id the server hasn't seen starts a fresh stream.
    def serve_live(self, q):
        url = q.get("url", [""])[0].strip()
        sid = q.get("session", [""])[0].strip().lower()
        quality = q.get("quality", ["320"])[0]
        if quality not in QUALITY:
            quality = "320"
        if not url or not re.fullmatch(r"[0-9a-f]{32}", sid):
            self.send_error(400, "url and 32-hex session required")
            return

        playlist = open_stream(url, quality, sid)
        try:
            if not playlist:
                raise OSError
            with open(playlist, "rb") as f:
                data = f.read()
        except OSError:
            self.send_error(502, "stream unavailable")
            return
        self.send_response(200)
        self.send_header("Content-Type", "application/vnd.apple.mpegurl")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


if __name__ == "__main__":
    ensure_ffmpeg()
    kill_stray_ffmpeg()
    shutil.rmtree(BUFFER_DIR, ignore_errors=True)
    os.makedirs(BUFFER_DIR, exist_ok=True)
    threading.Thread(target=stop_when_idle, daemon=True).start()
    if subprocess.run(["tailscale", "status"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode != 0:
        print("WARNING: Tailscale is disconnected, the phone won't be able to reach this server.", flush=True)
    server = ThreadingHTTPServer(("0.0.0.0", 5050), Handler)
    print("Listening on port 5050, waiting for the app...", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        stop_stream()
