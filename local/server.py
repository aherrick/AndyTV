import os, time, subprocess, threading, zipfile, urllib.request
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import parse_qs, urlparse

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
BUFFER_DIR = os.path.join(BASE_DIR, "buffer")
FFMPEG = os.path.join(BASE_DIR, "ffmpeg", "bin", "ffmpeg.exe")

QUALITY = {
    "240":  (240, "150k", "200k", "300k"),
    "320":  (320, "220k", "290k", "440k"),
    "360":  (360, "260k", "340k", "520k"),
    "480":  (480, "400k", "520k", "800k"),
    "576":  (576, "550k", "715k", "1100k"),
    "720":  (720, "900k", "1170k", "1800k"),
}

proc = None


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


def start_stream(url, quality):
    global proc
    h, vbr, maxr, bufs = QUALITY.get(quality, QUALITY["320"])

    if proc and proc.poll() is None:
        proc.terminate()
        try:
            proc.wait(timeout=3)
        except subprocess.TimeoutExpired:
            pass
    kill_stray_ffmpeg()
    time.sleep(0.5)

    os.makedirs(BUFFER_DIR, exist_ok=True)
    for f in os.listdir(BUFFER_DIR):
        try: os.remove(os.path.join(BUFFER_DIR, f))
        except OSError: pass

    proc = subprocess.Popen([
        FFMPEG, "-i", url,
        "-c:v", "libx264", "-preset", "veryfast",
        "-b:v", vbr, "-maxrate", maxr, "-bufsize", bufs,
        "-vf", f"scale=-2:{h}",
        "-c:a", "aac", "-b:a", "128k",
        "-f", "hls", "-hls_time", "6", "-hls_list_size", "60",
        "-hls_flags", "delete_segments+program_date_time+independent_segments",
        os.path.join(BUFFER_DIR, "live.m3u8"),
    ], creationflags=subprocess.CREATE_NEW_CONSOLE)


def stop_stream():
    global proc
    if proc and proc.poll() is None:
        proc.terminate()
        try:
            proc.wait(timeout=3)
        except subprocess.TimeoutExpired:
            pass
    kill_stray_ffmpeg()
    proc = None


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=BUFFER_DIR, **kw)

    def log_message(self, *a): pass

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_GET(self):
        try:
            super().do_GET()
        except (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            pass

    def do_POST(self):
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
            threading.Thread(target=start_stream, args=(url, quality), daemon=True).start()
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"ok")
        elif path == "/stop":
            stop_stream()
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"ok")
        else:
            self.send_error(404)


if __name__ == "__main__":
    ensure_ffmpeg()
    os.makedirs(BUFFER_DIR, exist_ok=True)
    kill_stray_ffmpeg()
    server = ThreadingHTTPServer(("0.0.0.0", 5050), Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        stop_stream()
