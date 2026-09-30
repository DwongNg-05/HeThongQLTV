"""Publish to artifacts/docker-publish first; uses an isolated temporary database."""
import html
import http.cookiejar
import os
from pathlib import Path
import re
import socket
import sqlite3
import subprocess
from contextlib import nullcontext
import uuid
import time
import urllib.parse
import urllib.request

root = Path(__file__).resolve().parents[1]
publish = root / "artifacts" / "docker-publish"
assert (publish / "HeThongQLTV.dll").exists(), "Publish the application first"

scratch_path = root / "artifacts" / ("production-demo-" + uuid.uuid4().hex)
scratch_path.mkdir(parents=True)
# Retain isolated test data and logs under ignored artifacts/ for diagnosis.
with nullcontext(scratch_path) as scratch:
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        port = sock.getsockname()[1]
    base = f"http://127.0.0.1:{port}"
    env = os.environ.copy()
    env.update({
        "ASPNETCORE_ENVIRONMENT": "Production",
        "ASPNETCORE_URLS": base,
        "RENDER_EXTERNAL_HOSTNAME": "hethongqltv.onrender.com",
        "ConnectionStrings__Library": f"Data Source={Path(scratch) / 'library.db'}",
        "DataProtection__KeysPath": str(Path(scratch) / "keys"),
        "Logging__EventLog__LogLevel__Default": "None",
    })
    env.pop("SeedDemo", None)
    # Repeat against the same database to verify startup does not duplicate accounts.
    for attempt in range(4):
        if attempt >= 2:
            database = Path(scratch) / f"no-demo-{attempt}.db"
            env["ConnectionStrings__Library"] = f"Data Source={database}"
            if attempt == 2:
                env["SeedDemo"] = "false"
            else:
                env.pop("SeedDemo", None)
                env["RENDER_EXTERNAL_HOSTNAME"] = "another-service.onrender.com"
        with open(Path(scratch) / f"server-{attempt}.log", "w") as log:
            process = subprocess.Popen(
                ["dotnet", str(publish / "HeThongQLTV.dll")],
                cwd=publish, env=env, stdout=log, stderr=subprocess.STDOUT,
            )
            try:
                for _ in range(120):
                    if process.poll() is not None:
                        raise AssertionError("Application exited during startup")
                    try:
                        urllib.request.urlopen(base + "/Account/Login", timeout=1).close()
                        break
                    except OSError:
                        time.sleep(0.5)
                else:
                    raise AssertionError("Application did not start")
                if attempt >= 2:
                    with sqlite3.connect(database) as connection:
                        assert connection.execute('SELECT COUNT(*) FROM Users').fetchone()[0] == 0
                    print("PASS Explicit opt-out" if attempt == 2 else "PASS Other production host has no demo users")
                    continue
                for username, protected_path in [
                    ("admin", "/Users"), ("thuthu", "/Members"), ("docgia", "/Reservations")
                ]:
                    client = urllib.request.build_opener(
                        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
                    )
                    with client.open(base + "/Account/Login", timeout=10) as response:
                        page = response.read().decode()
                    token = html.unescape(re.search(
                        r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page
                    ).group(1))
                    body = urllib.parse.urlencode({
                        "username": username, "password": "ThuVien@123",
                        "__RequestVerificationToken": token,
                    }).encode()
                    with client.open(base + "/Account/Login", body, timeout=10) as response:
                        assert "/Account/Login" not in response.url, username
                    with client.open(base + protected_path, timeout=10) as response:
                        assert response.status == 200 and "/Account/Login" not in response.url, username
                    print(f"PASS Production demo login {username}, startup {attempt + 1}")
            finally:
                process.terminate()
                try:
                    process.wait(timeout=15)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
