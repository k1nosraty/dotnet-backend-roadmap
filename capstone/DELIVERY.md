# اجرای محلی و تحویل نهایی

فرمان‌ها از ریشهٔ مخزن هستند. مسیر خودکارِ کامل و قابل تکرار، `python scripts/capstone_delivery.py` است. این اسکریپت منابع Docker با نام تصادفی و مخصوص خودش می‌سازد، تست و restore را اجرا می‌کند و در پایان همان منابع آزمایشگاهی را پاک می‌کند. آن را روی دادهٔ واقعی تنظیم نکن.

## اجرای دستی با PowerShell

Linux Docker را فعال کن. رمزها را در همین session بساز؛ آن‌ها را در Git، history متنی، screenshot یا خروجی گزارش قرار نده.

```powershell
$env:CAPSTONE_DB_PASSWORD = 'Aa9!' + [guid]::NewGuid().ToString('N')
$env:CAPSTONE_LAB_PASSWORD = 'Aa9!' + [guid]::NewGuid().ToString('N')
$env:CAPSTONE_SCHEMA = 'capstone_' + [guid]::NewGuid().ToString('N')
$env:CAPSTONE_COMMIT = 'local'
$env:CAPSTONE_TLS_PORT = '8443'
$env:CAPSTONE_DATABASE = 'desk'
$composeFile = 'samples/89-servicedesk-delivery/compose.yaml'
docker compose -p capstone-lab -f $composeFile config --quiet
docker compose -p capstone-lab -f $composeFile build app
docker compose -p capstone-lab -f $composeFile up -d db
docker compose -p capstone-lab -f $composeFile exec db pg_isready -h 127.0.0.1 -U course -d desk
```

اگر آخرین فرمان ready نیست، آن را تا آماده‌شدن DB تکرار کن. سپس فقط یک بار در **schema تازهٔ همین آزمایشگاه**:

```powershell
docker compose -p capstone-lab -f $composeFile run --rm --no-deps -e ASPNETCORE_ENVIRONMENT=Development -e Desk__LabHttp=true -e CAPSTONE_LAB_PASSWORD app --init-lab
docker compose -p capstone-lab -f $composeFile up -d app proxy
docker compose -p capstone-lab -f $composeFile cp proxy:/data/caddy/pki/authorities/local/root.crt ./capstone-root.crt
```

اگر CA هنوز ساخته نشده، چند لحظه بعد فرمان cp را تکرار کن. خروجی bootstrap باید `schema=ready; accounts=4; credentials=not-printed` باشد. helper جدول‌ها را فقط در schema نو می‌سازد؛ migration برای ارتقای دادهٔ زنده نیست. روی schema موجود شکست آن را با حذف داده «رفع» نکن.

گواهی `capstone-root.crt` فقط CA عمومی همین محیط محلی است. برای درخواست دستی از curl با trust صریح استفاده کن؛ `-k` یا غیرفعال‌کردن validation مجاز نیست:

```powershell
curl.exe --cacert ./capstone-root.crt https://localhost:8443/health/ready
curl.exe --cacert ./capstone-root.crt https://localhost:8443/version
```

برای اجرای خودکار login و گردش درخواست و آزمون بازیابی، اسکریپت delivery از همین CA با hostname validation استفاده می‌کند. چهار حساب `requester@alpha.test`، `peer@alpha.test`، `agent@alpha.test` و `requester@beta.test` رمز session بالا را دارند؛ فقط agent کارشناس است. رمز تصادفی را صرفاً در client محلی وارد کن. login با POST `/auth/login` و JSON شامل email/password است؛ accessToken را در Authorization: Bearer قرار بده و چاپ نکن.

## اجرای مسیر با client

| ترتیب | درخواست | نتیجه |
| --- | --- | --- |
| ۱ | POST /requests؛ عنوان و priority=urgent، header Idempotency-Key با Guid تازه | 201، version=1 و شناسه |
| ۲ | تکرار دقیق مرحلهٔ ۱ با همان صاحب | 200، همان شناسه |
| ۳ | POST /requests/{id}/claim با حساب صاحب و body version=1 | 403 |
| ۴ | همان claim با حساب agent | 200، version=2 |
| ۵ | POST /requests/{id}/close با agent؛ version=2 و resolution معتبر | 200، version=3، Closed |
| ۶ | GET /requests/{id}/audit با صاحب | سه رویداد بدون متن درخواست |
| ۷ | POST /requests/{id}/reopen با صاحب؛ version=3 | 200، version=4، Open |

body نمونهٔ create برابر `{"title":"Printer is unavailable","priority":"urgent"}` است. body تغییر برابر `{"version":2,"resolution":"Printer connection repaired"}` است. در claim و reopen فقط version لازم است. همکار یا سازمان دیگر باید get/audit را 404 بگیرد. برای هر پاسخ غیرمنتظره ابتدا status و health و commit را بررسی کن؛ token و payload را در log عمومی نریز.

## ارسال واقعی درخواست‌ها بدون چاپ credential

پس از آماده‌شدن محیط دستی بالا، در همان ترمینال، Python 3 را اجرا کن. کد زیر را یک‌جا بفرست؛ از CA عمومی کپی‌شده استفاده می‌کند و رمز را از متغیر session می‌خواند. در این آزمایش حساب peer درخواست جداگانهٔ خودش را ثبت می‌کند و agent آن را می‌پذیرد و می‌بندد؛ حساب peer در حالت عادی به درخواست‌های صاحب دیگر دسترسی ندارد.

```powershell
$env:CAPSTONE_CA = (Resolve-Path ./capstone-root.crt).Path
$env:CAPSTONE_BASE = 'https://localhost:8443'
```

کد Python زیر را در Python اجرا کن، یا در PowerShell بین `@'` و `'@ | python -` قرار بده. این دو delimiter باید روی خط جدا باشند. برای خواندن کد لازم نیست Python یاد بگیری؛ ترتیب URL، method، body و نتیجه را با جدول بالا مقایسه کن.

```python
import json
import os
import ssl
from urllib import request, error
from uuid import uuid4

base = os.environ['CAPSTONE_BASE']
context = ssl.create_default_context(cafile=os.environ['CAPSTONE_CA'])
opener = request.build_opener(request.ProxyHandler({}), request.HTTPSHandler(context=context))
password = os.environ['CAPSTONE_LAB_PASSWORD']

def send(path, method='GET', body=None, token=None, key=None):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    if key:
        headers['Idempotency-Key'] = key
    data = None if body is None else json.dumps(body).encode('utf-8')
    req = request.Request(base + path, data=data, method=method, headers=headers)
    try:
        with opener.open(req, timeout=10) as response:
            status, raw = response.status, response.read()
    except error.HTTPError as response:
        status, raw = response.code, response.read()
    return status, json.loads(raw) if raw else None

def login(email):
    status, result = send('/auth/login', 'POST', {'email': email, 'password': password})
    assert status == 200, 'Login failed; check readiness and lab credentials.'
    return result['accessToken']

owner = login('peer@alpha.test')
agent = login('agent@alpha.test')
key = str(uuid4())
body = {'title': 'Manual client proof', 'priority': 'urgent'}
status, created = send('/requests', 'POST', body, owner, key)
assert status == 201
status, replay = send('/requests', 'POST', body, owner, key)
assert status == 200 and replay['id'] == created['id']
route = '/requests/' + created['id']
assert send(route + '/claim', 'POST', {'version': 1}, owner)[0] == 403
assert send(route + '/claim', 'POST', {'version': 1}, agent)[0] == 200
status, closed = send(route + '/close', 'POST', {'version': 2, 'resolution': 'Connection repaired'}, agent)
assert status == 200 and closed['state'] == 'Closed' and closed['version'] == 3
status, audit = send(route + '/audit', token=owner)
assert status == 200 and len(audit) == 3
print('PASS: manual client: create, replay, denied owner claim, agent close, audit=3.')
```

خروجی نهایی باید یک خط PASS باشد؛ password، accessToken یا refreshToken را print نکن. اجرای دوباره درخواست مستقل تازه‌ای می‌سازد چون key تازه تولید می‌شود. اگر مرحله‌ای assertion داد، مرحله‌های بعد را ادامه نده؛ health و status همان مرحله را بررسی کن. هنگام انقضای token، کل مثال را دوباره اجرا کن تا login تازه انجام شود.

## بازیابی و توقف

اسکریپت delivery پس از close، از schema آزمایشگاهی `pg_dump -Fc` می‌گیرد، در DB جدید restore می‌کند و همان image را با اتصال DB جدید راه می‌اندازد. SHA256 فایل DLL باید یکسان، version درخواست ۳، state بسته و تعداد audit سه باشد. token قبلی با key volume قبلی معتبر می‌ماند. این بازیابی در DB تازه است؛ بازنویسی DB زنده نیست. برای ارتقای واقعی schema باید migration، سازگاری نسخهٔ قبلی و بازیابی را جدا طراحی کرد؛ صرف عوض‌کردن image، rollback دیتابیس نیست.

توقف بدون حذف داده:

```powershell
docker compose -p capstone-lab -f $composeFile stop
```

فقط پس از پایان همین آزمایشگاهِ دورریختنی، پاک‌سازی صریح داده‌ها:

```powershell
docker compose -p capstone-lab -f $composeFile down --volumes --remove-orphans
Remove-Item ./capstone-root.crt
Remove-Item Env:CAPSTONE_DB_PASSWORD, Env:CAPSTONE_LAB_PASSWORD
```

## مرز استقرار واقعی و دفاع

تنها proxy روی loopback پورت دارد؛ DB و app خصوصی‌اند. app با UID غیر root اجرا می‌شود. IP ثابت proxy در شبکهٔ جداگانه، trust boundary همین Compose است؛ در محیط دیگر آن را از روی شبکهٔ واقعی تعیین کن. CIDR نمونه ممکن است با شبکهٔ میزبان تداخل کند؛ subnet و ProxyIp را با هم تغییر بده. چند اجرای دستی هم‌زمان با همین subnet نداشته باش.

کلیدهای Data Protection روی volume خصوصی پایدارند ولی رمزگذاری at-rest آن‌ها پیکربندی نشده است. حساب DB آزمایشگاهی برای ساخت جدول و restore اختیار زیاد دارد. استقرار عمومی به TLS عمومی، DB role محدود، حفاظت/backup کلیدها، مدیریت حساب و بازیابی رمز، secret manager، log/alert عملیاتی و سیاست نگهداری audit نیاز دارد. محدودکننده‌ها درون هر process هستند؛ login ده درخواست در دقیقه به صورت مشترک و API صد درخواست در دقیقه برای هر شناسه دارد؛ این طراحی توزیع‌شده نیست. غیرفعال‌کردن log خام EF برای حذف دادهٔ حساس، جایگزین پایش عملیاتی نیست.

در دفاع، مسیر یک درخواست را از endpoint تا Domain، دو SaveChanges داخل transaction و unique index توضیح بده. چرا replay باید به owner و tenant محدود باشد؟ چرا role از DB خوانده می‌شود؟ اگر audit fail شود چه چیز rollback می‌شود؟ اگر حجم زیاد شد اول کدام اندازه‌گیری را انجام می‌دهی؟ چه چیزی با restore DB بدون key ring خراب می‌شود؟ پاسخ‌ها را با شاهد اجرا همراه کن.
