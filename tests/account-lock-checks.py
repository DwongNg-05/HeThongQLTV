"""Run only against an isolated demo database: python account-lock-checks.py URL."""
import html, http.cookiejar, re, sys, urllib.request, urllib.parse, urllib.error
base = sys.argv[1].rstrip("/")
def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def get(c, path):
    r=c.open(base+path)
    return r.geturl(), html.unescape(r.read().decode())
def post(c,path,fields,page=None):
    fields=dict(fields)
    if page is not None:
        fields["__RequestVerificationToken"]=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',page).group(1)
    r=c.open(base+path,urllib.parse.urlencode(fields).encode())
    return r.geturl(),html.unescape(r.read().decode())
def login(name):
    c=client()
    _,p=get(c,"/Account/Login")
    post(c,"/Account/Login",{"username":name,"password":"ThuVien@123"},p)
    return c
def rejects(code,fn):
    try: fn()
    except urllib.error.HTTPError as e:
        assert e.code==code, e.code
        return
    raise AssertionError("Request accepted")
admin=login("admin")
reader=login("docgia")
staff=login("thuthu")
_,page=get(admin,"/Users")
row=next(r for r in re.findall(r"<tr>.*?</tr>",page,re.S) if "<td>docgia</td>" in r)
path=re.search(r'action="([^"]+)"',row).group(1)
selfrow=next(r for r in re.findall(r"<tr>.*?</tr>",page,re.S) if "<td>admin</td>" in r)
selfid=re.search(r'/Users/Edit/(\d+)',selfrow).group(1)
assert "SetActive" not in selfrow
rejects(400,lambda:post(admin,path,{"active":"false"}))
rejects(400,lambda:post(admin,path,{},page))
rejects(400,lambda:post(admin,path,{"active":"invalid"},page))
rejects(404,lambda:post(admin,"/Users/SetActive/999999",{"active":"false"},page))
for c in [reader,staff]:
    _,p=get(c,"/Account/Login")
    rejects(403,lambda:post(c,path,{"active":"false"},p))
_,p=post(admin,"/Users/SetActive/"+selfid,{"active":"false"},page)
assert "Không thể tự khóa" in p
_,p=post(admin,path,{"active":"false"},page)
assert "Đã khóa tài khoản docgia." in p
url,_=get(reader,"/Loans")
assert "/Account/Login" in url
blocked=client()
_,p=get(blocked,"/Account/Login")
url,p=post(blocked,"/Account/Login",{"username":"docgia","password":"ThuVien@123"},p)
assert "/Account/Login" in url and "tài khoản đã khóa" in p
_,page=get(admin,"/Users")
_,p=post(admin,path,{"active":"false"},page)
assert "Đã khóa tài khoản docgia." in p
_,p=post(admin,path,{"active":"true"},p)
assert "Đã mở khóa tài khoản docgia." in p
reader=login("docgia")
url,_=get(reader,"/Loans")
assert "/Account/Login" not in url
print("PASS lock/unlock, blocked login, existing session rejected, restored login, admin-only access, self-lock protection, CSRF, invalid input, missing user, repeated lock")
