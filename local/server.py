import os, sys, shutil, subprocess, threading, time, uuid, zipfile, urllib.request
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
    global proc
    if proc and proc.poll() is None:
        proc.kill()
        proc.wait()
    proc = None
    shutil.rmtree(BUFFER_DIR, ignore_errors=True)


def stop_stream():
    with lock:
        _kill()


def start_stream(url, quality):
    """Kill whatever is running, start a fresh ffmpeg in its own folder, and return its id once playable."""
    global proc, last_request
    h, vbr, maxr, bufs = QUALITY[quality]

    with lock:
        _kill()
        sid = uuid.uuid4().hex[:12]
        playlist = os.path.join(BUFFER_DIR, sid, "live.m3u8")
        os.makedirs(os.path.dirname(playlist))
        # 2s keyframe-aligned segments so the first playlist is ready quickly; 180 x 2s keeps the 6-minute buffer.
        proc = my_proc = subprocess.Popen([
            FFMPEG, "-i", url,
            "-c:v", "libx264", "-preset", "veryfast", "-tune", "zerolatency",
            "-force_key_frames", "expr:gte(t,n_forced*2)",
            "-b:v", vbr, "-maxrate", maxr, "-bufsize", bufs,
            "-vf", f"scale=-2:{h}",
            "-c:a", "aac", "-b:a", "128k",
            "-f", "hls", "-hls_time", "2", "-hls_list_size", "180",
            "-hls_flags", "delete_segments+program_date_time+independent_segments",
            playlist,
        ], stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
           creationflags=subprocess.CREATE_NO_WINDOW)
        last_request = time.monotonic()

    # A newer /start kills this ffmpeg, which ends the wait too.
    deadline = time.monotonic() + PLAYLIST_TIMEOUT
    while time.monotonic() < deadline and my_proc.poll() is None:
        if os.path.exists(playlist):
            return sid
        time.sleep(0.25)
    return None


def stop_when_idle():
    while True:
        time.sleep(5)
        if proc and time.monotonic() - last_request > IDLE_TIMEOUT:
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
            super().do_GET()
        except (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            pass

    def do_POST(self):
        try:
            path = urlparse(self.path).path
            if path == "/start":
                q = parse_qs(urlparse(self.path).query)
                url = q.get("url", [""])[0].strip()
                if not url:
                    self.send_response(400)
                    self.end_headers()
                    self.wfile.write(b"missing required query parameter: url")
                    return
                quality = q.get("quality", ["320"])[0]
                if quality not in QUALITY:
                    quality = "320"
                sid = start_stream(url, quality)
                if not sid:
                    self.send_response(502)
                    self.end_headers()
                    self.wfile.write(b"stream failed to start")
                    return
                self.send_response(200)
                self.end_headers()
                self.wfile.write(sid.encode())
            else:
                self.send_error(404)
        except (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            pass


if __name__ == "__main__":
    ensure_ffmpeg()
    kill_stray_ffmpeg()
    shutil.rmtree(BUFFER_DIR, ignore_errors=True)
    os.makedirs(BUFFER_DIR, exist_ok=True)
    threading.Thread(target=stop_when_idle, daemon=True).start()
    server = ThreadingHTTPServer(("0.0.0.0", 5050), Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        stop_stream()
