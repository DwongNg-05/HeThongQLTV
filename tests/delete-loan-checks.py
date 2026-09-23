"""Run against a fresh isolated demo database, not the live library."""
import html
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

base = sys.argv[1].rstrip('/')

def get(client, path):
    return html.unescape(client.open(base + path).read().decode())

def post(client, path, fields, page):
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    return html.unescape(client.open(base + path, urllib.parse.urlencode({**fields, '__RequestVerificationToken': token}).encode()).read().decode())

def login(username):
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    post(client, '/Account/Login', {'username': username, 'password': 'ThuVien@123'}, get(client, '/Account/Login'))
    return client

staff = login('thuthu')
page = get(staff, '/Loans')
assert 'action="/Loans/Delete/1"' in page
assert 'action="/Loans/Delete/7"' not in page
page = post(staff, '/Loans/Delete/7', {}, page)
assert 'Chỉ được xóa phiếu mượn đã trả sách.' in page
reader = login('docgia')
reader_page = get(reader, '/Loans')
assert '/Loans/Delete/' not in reader_page
try:
    post(reader, '/Loans/Delete/1', {}, reader_page)
    raise AssertionError('Reader can delete loans')
except urllib.error.HTTPError as error:
    assert error.code == 403
try:
    staff.open(base + '/Loans/Delete/1', b'id=1')
    raise AssertionError('CSRF accepted')
except urllib.error.HTTPError as error:
    assert error.code == 400
page = post(staff, '/Loans/Delete/1', {}, page)
assert 'Đã xóa phiếu mượn đã trả.' in page and 'PM0001' not in page
page = post(staff, '/Loans/Return/7', {}, page)
page = post(staff, '/Loans/Delete/7', {}, page)
assert 'thu khoản phạt còn nợ' in page
page = post(staff, '/Loans/Pay/7', {}, page)
page = post(staff, '/Loans/Delete/7', {}, page)
assert 'Đã xóa phiếu mượn đã trả.' in page and 'PM0007' not in page
print('PASS returned loan deletion, active-loan rejection, unpaid fine protection, reader authorization and CSRF')
