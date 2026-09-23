"""Run against an isolated demo database: python reservation-http-checks.py URL."""
import html
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

base = sys.argv[1].rstrip("/")
def session(username):
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    page = client.open(base + "/Account/Login").read().decode()
    send(client, "/Account/Login", {"username": username, "password": "ThuVien@123"}, page)
    return client

def send(client, path, fields, page):
    fields = dict(fields)
    fields["__RequestVerificationToken"] = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    return html.unescape(client.open(base + path, urllib.parse.urlencode(fields).encode()).read().decode())

def get(client, path):
    return html.unescape(client.open(base + path).read().decode())

reader = session("docgia")
page = get(reader, "/Reservations")
assert 'href="/Reservations/Create"' in page
page = get(reader, "/Reservations/Create")
assert 'id="book-picker"' in page and "Xác nhận đặt trước" in page
for book_id in [6, 8]:
    page = get(reader, "/Reservations/Create")
    page = send(reader, "/Reservations/Create", {"Items[0].BookId": book_id, "Items[0].Quantity": 1}, page)
    assert "Đặt trước sách online thành công" in page
print("PASS online form reserves available and unavailable books")
page = get(reader, "/Reservations/Create")
page = send(reader, "/Reservations/Create", {"Items[0].BookId": 6, "Items[0].Quantity": 1}, page)
assert "Bạn đã đặt trước sách này." in page and 'id="book-picker"' in page
page = send(reader, "/Reservations/Create", {"Items[0].BookId": 0, "Items[0].Quantity": 1}, page)
assert "validation-summary-errors" in page
print("PASS duplicate and missing selection errors displayed in form")
try:
    reader.open(base + "/Reservations/Create", b"bookId=6")
    raise AssertionError("Missing CSRF accepted")
except urllib.error.HTTPError as error:
    assert error.code == 400
staff = session("thuthu")
page = get(staff, "/Reservations")
assert len(re.findall(r"<tr>", page)) >= 3
assert 'href="/Reservations/Create"' not in page
try:
    get(staff, "/Reservations/Create")
    raise AssertionError("Staff allowed to create reader reservation")
except urllib.error.HTTPError as error:
    assert error.code == 403
print("PASS staff sees requests; reader-only form and CSRF enforced")
page = get(reader, "/Reservations")
path = re.search(r'action="(/Reservations/Cancel/\d+)"', page).group(1)
page = send(reader, path, {}, page)
assert "Đã hủy yêu cầu đặt trước" in page
print("PASS reader cancels own request")
