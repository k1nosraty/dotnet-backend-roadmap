# درس 068 — rate limiting و کنترل سوءاستفاده

## خروجی قابل مشاهده

۴۰ درخواست هم‌زمان از یک کاربر به تیکت خودش، در پنجرهٔ تازه ۳۲ پاسخ 200 و ۸ پاسخ 429 می‌گیرند.
کاربر دوم بودجهٔ مستقل دارد و عوض کردن X-Forwarded-For بودجهٔ مهاجم را تازه نمی‌کند.
پیش‌نیاز: [درس ۶۷](../067-web-security/README.md)، middleware و HTTP و سناریوی رقابتی.

## چه چیزی را محدود می‌کنیم؟

| مسیر | کلید partition | بودجهٔ نمونه |
| --- | --- | --- |
| login و refresh | RemoteIpAddress واقعی اتصال | مشترک، ۱۲ درخواست در یک دقیقه |
| گروه /api/tickets | userId از principal احرازشده؛ برای ناشناس IP | مشترک بین routeهای تیکت، ۳۲ درخواست در یک دقیقه |
| /health | در این نمونه محدود نشده | بررسی زنده بودن process |

این اعداد آموزشی‌اند؛ اندازهٔ مناسب محصول از load test، هزینهٔ query و رفتار کاربر به‌دست می‌آید.
GET و POST و search گروه تیکت از یک بودجه مصرف می‌کنند، نه یک bucket تازه برای هر شناسه.
login بر اساس email body تقسیم نمی‌شود؛ مهاجم با email تصادفی bucket تازه نمی‌سازد.
remote IP هم هویت کاربر نیست؛ چند کاربر پشت NAT ممکن است سهم login مشترک داشته باشند.

## ساختار پیاده‌سازی

Abuse/AbuseLimits.cs، AddRateLimiter و دو policy نام‌دار را ثبت می‌کند.
RateLimitPartition.GetFixedWindowLimiter از امکانات framework است؛ شمارندهٔ معمولی static یا Dictionary بدون هماهنگی نمی‌سازیم.
QueueLimit صفر است تا اضافه‌بار به صف نامحدود و latency پنهان تبدیل نشود.
OnRejected پاسخ عمومی 429 و Retry-After با تعداد ثانیه می‌دهد.
اگر metadata زمان در دسترس نباشد header اجباریِ حدسی تولید نمی‌کنیم.

پس از UseRouting و UseCors، ابتدا UseAuthentication اجرا می‌شود و سپس UseRateLimiter و UseAuthorization.
بنابراین userId پیش از partition خواندن شناخته شده است؛ credential جعلی user partition معتبر نمی‌سازد.
درخواست ناشناسِ بودجه‌دار ممکن است قبل از authorization، 429 بگیرد؛ همیشه انتظار 401 برای درخواست اضافه‌بار نداشته باش.
RequireRateLimiting روی login و refresh و کل گروه تیکت اعمال می‌شود؛ صرف ثبت service محدودیت ایجاد نمی‌کند.
مسیرهای دیگر مانند report و csrf در این lab بودجهٔ route اختصاصی ندارند؛ به معنی محافظت کامل تمام سرویس نیست.

## خطای رایج در proxy

این host forwarded header processing ندارد و X-Forwarded-For را برای partition نمی‌خواند.
اگر این header را مستقیم بپذیری، client با عوض کردن آن می‌تواند bucket تازه بسازد.
پشت proxy واقعی باید forwarding فقط از proxy یا شبکهٔ مورد اعتماد پذیرفته شود و سپس key واقعی را به‌درستی تعیین کنی.
تنظیم deployment در این درس انجام نشده است؛ تا آن زمان HTTP موقت فقط روی loopback استفاده می‌شود.
IP-based limiting برای IPv6، proxyهای متعدد و NAT به تحلیل بیشتری نیاز دارد؛ raw IP نسخهٔ کامل کنترل abuse نیست.

## چند کنترل مکمل

Identity lockout خطاهای متوالی یک حساب را محدود می‌کند. limiter، تعداد درخواست رسیدن به route را محدود می‌کند.
این دو یک چیز نیستند: account lockout به‌تنهایی جلوی فشار با نام‌های کاربری تصادفی را نمی‌گیرد.
محدودیت فقط IP نیز جلوی حملهٔ توزیع‌شده به یک حساب را کامل نمی‌گیرد و می‌تواند کاربران NAT را معطل کند.
برای محصول، محدودیت چندلایه، بودجهٔ per-account با حفظ حریم، تشخیص رفتار و حفاظت upstream لازم است.

Fixed window در مرز دو پنجره می‌تواند burst بیشتری بدهد؛ برای جریان هموارتر sliding window یا token bucket را بررسی کن.
Concurrency limiter تعداد عملیات هم‌زمان را محدود می‌کند، نه مجموع درخواست در دقیقه؛ برای query سنگین مسئلهٔ متفاوتی است.
تعداد partitionهای فعال نیز حافظه مصرف می‌کند؛ key دلخواه از query string یا email ناشناس نساز.
هیچ‌کدام از این‌ها authorization یا کنترل مالکیت را حذف نمی‌کنند.

## محدودیت process محلی

budget در حافظهٔ همین process است و با restart تازه می‌شود؛ این رفتار عمداً در تست برای ساخت پنجرهٔ تمیز استفاده می‌شود.
دو replica بودجهٔ مشترک ندارند؛ این نمونه distributed rate limiter یا DDoS protection کامل نیست.
کلید Data Protection و حساب‌های DB ممکن است پایدار بمانند، ولی شمارندهٔ limiter پایدار نیست.
برای چند instance، کنترل مشترک در gateway یا طراحی distributed با قرارداد اتمیک و failure behavior لازم است.
health موفق هم به معنی آماده بودن DB نیست؛ /health/ready قرارداد جدا دارد.

## راه‌اندازی آزمایشگاه

ابتدا [راهنمای PostgreSQL](../../../handbook/postgresql-lab.md) را انجام بده.
یک database اختصاصی با role دارای مجوز ساخت schema لازم است؛ از database محصول استفاده نکن.
هر نمونه schema و key ring مستقل دارد و جدول موجود را جایگزین نمی‌کند.
فرمان init، ساخت حساب را فقط در CLI محلیِ Development انجام می‌دهد؛ endpoint ثبت‌نام یا اعطای نقش وجود ندارد.

```powershell
$schema = 'auth68_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-access-keys-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/67-rate-limits/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/67-rate-limits/SupportDesk.csproj -- --urls http://127.0.0.1:5068
```

این lab شش حساب موقت می‌سازد؛ رمز انتخابی فقط برای همین آزمایش است. رمز را در تاریخچهٔ فرمان ننویس.
همهٔ حساب‌ها همان رمز موقت دارند و CLI تأییدشان می‌کند؛ ایمیل واقعی ارسال نمی‌شود.
حساب‌ها و نقش‌ها توسط client تعیین نمی‌شوند:

| حساب | سازمان | نقش SupportAgent | permission=tickets.manage |
| --- | --- | --- | --- |
| owner@example.test | alpha | خیر | خیر |
| peer@example.test | alpha | خیر | خیر |
| other@example.test | beta | خیر | خیر |
| staff@example.test | alpha | بله | بله |
| roleonly@example.test | alpha | بله | خیر |
| claimonly@example.test | alpha | خیر | بله |

برای ورود، POST /auth/login با JSON شامل email و password بزن و accessToken را فقط در حافظهٔ client نگه دار.
درخواست بعدی، header با شکل Authorization: Bearer <accessToken> دارد؛ token را چاپ، commit یا در URL قرار نده.
مسیرهای cookie، CSRF، refresh و logout درس ۶۴ حفظ شده‌اند.
بدون flag محلی، HTTP رد می‌شود و cookie Secure است؛ در Production این flag باعث شکست startup می‌شود.
key ring خصوصی است؛ HTTPS، حفاظت at-rest و تنظیم proxy واقعی هنوز مسئولیت استقرارند.
محدودیت refresh reuse، عمر access token و اثر محدود logout همان درس ۶۴ است.
این نمونه fresh-schema آموزشی دارد؛ migration محصول را از بخش data پیاده کن.


## رفتار client

پس از 429، Retry-After را بخوان و از retry فوری در حلقه خودداری کن.
برای عملیات قابل تکرار، backoff و jitter و سقف تلاش داشته باش؛ retry یک POST بدون طراحی idempotency می‌تواند اثر دوباره ایجاد کند.
password یا bearer را برای عیب‌یابی در log ننویس؛ برای تحلیل از تعداد 429 و latency و نوع route استفاده کن.
جزئیات audit و دادهٔ حساس در درس ۶۹ ادامه می‌یابد.

## اجرای بررسی

```powershell
python scripts/check_content.py
python scripts/security_next_smoke.py --build-only
python scripts/security_next_smoke.py
```

اجرای کامل SDK 10، Python 3، PostgreSQL، psql و SUPPORTDESK_PG می‌خواهد.
متغیرهای SUPPORTDESK_TEST_PGHOST، SUPPORTDESK_TEST_PGPORT، SUPPORTDESK_TEST_PGDATABASE، SUPPORTDESK_TEST_PGUSER و SUPPORTDESK_TEST_PGPASSWORD را مطابق همان اتصال تنظیم کن؛ جزئیات در [فهرست بخش](../README.md) است.
اسکریپت برای هر snapshot یک schema تصادفی و key ring موقت می‌سازد، خروجی publish را از پوشهٔ جدا اجرا می‌کند و در پایان فقط schema خودش را حذف می‌کند.
--build-only ساخت و HTTP ناشناس/جعلی و fallback را بررسی می‌کند و آزمون مجوز واقعی دیتابیس نیست.
password و token در خروجی تست چاپ نمی‌شوند.


تست هم‌زمان با ThreadPoolExecutor و HTTP واقعی اجرا می‌شود؛ فقط مقدار config را نمی‌خواند.
در یک host تازه، token معتبر صادرشده پیش از restart استفاده می‌شود و ۴۰ GET رقابت می‌کنند.
بعد user دوم پاسخ 200 می‌گیرد، user اول با header جعلی همچنان 429 می‌گیرد و health پاسخ می‌دهد.
۱۲ login ناموفق با email و X-Forwarded-For متفاوت از یک اتصال loopback، بودجهٔ مشترک login/refresh را تمام می‌کنند.
انتظار برای replenishment واقعی یک دقیقه و تست load گسترده اجرا نمی‌شود؛ این‌ها آزمون مستقل بعدی‌اند.

## تمرین مستقل

یک operation کندِ کنترل‌شده را فقط در آزمایش اضافه کن و تفاوت concurrency limit و fixed window را با مشاهدهٔ تعداد هم‌زمان بسنج.
برای محصول یک جدول هزینهٔ route بنویس و انتخاب عددها را با اندازه‌گیری توضیح بده.
طرح چند replica را با سناریوی قطع سرویس شمارنده و رفتار fail-open/fail-closed مکتوب کن.

### سرنخ

تعداد موفق و ردشده را هم‌زمان بشمار؛ تست ترتیبی فقط بخشی از خطاهای شمارنده را آشکار می‌کند.
قبل از انتخاب partition، منبع اعتمادِ key را روی کاغذ مشخص کن.

## معیار عبور

- تعداد 200 و 429 در رقابت واقعی دقیق باشد.
- کاربر دوم از مصرف سهم کاربر اول آسیب نبیند.
- email و forwarded header جعلی محدودیت login را دور نزنند.
- تفاوت lockout، rate limit، concurrency و محافظت DDoS را توضیح بدهی.
- reset شدن budget در restart و مستقل بودن replicaها را محدودیت طراحی بدانی.

## منابع رسمی

بررسی در 2026-10-18:

- [Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Forwarded headers و proxy](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)

[قبلی](../067-web-security/README.md) · [فهرست بخش](../README.md)
[ادامه: درس ۶۹ و secrets و audit](../069-secrets/README.md).
