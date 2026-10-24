# درس 070 — بازبینی امنیت و سناریوهای منفی

## خروجی قابل مشاهده

بخش امنیت را با یک ماتریس شواهد تحویل می‌دهی: چه چیزی اجرا و بررسی شده، چه ریسکی باقی است و چه چیزی هنوز پیاده نشده است.
دستور CLI فهرست routeهای واقعی و policy و budget آن‌ها را بررسی می‌کند؛ یک public route ساختگیِ ناخواسته نیز باید رد شود.
پیش‌نیاز: [درس ۶۹](../069-secrets/README.md) و تمام تمرین‌های بخش امنیت.

## منظور از بازبینی چیست؟

«تست‌ها موفق‌اند» برابر با «هیچ آسیب‌پذیری وجود ندارد» نیست.
محدودهٔ این تحویل دو host آموزشی نهایی و snapshotهای پیشین است، نه deployment عمومی، پنل حساب محصول یا آزمون نفوذ کامل.
از مدل تهدید درس ۶۱ شروع کن و برای هر مرز اعتماد یک مورد مثبت و یک مورد منفی انتخاب کن.
عدم دسترسی باید علاوه بر status با عدم تغییر داده تأیید شود؛ فقط دیدن 403 یا 404 کافی نیست.

## مرزهای نسخهٔ نهایی

AccountAccess هویت و tenant فعلی را از DB می‌خواند. handler منبع، owner و tenant و operation را بررسی می‌کند.
مسیرهای تیکت BearerOnly هستند و cookie-action و logout و login cookie قرارداد CSRF دارند.
AuditReadHandler، permission مستقل audit.read را از DB می‌سنجد و endpoint audit سازمان فعلی را فیلتر می‌کند.
write و audit ماندگار یک commit دارند و logger نتیجه را پس از commit می‌گوید.
Title هنوز مدل کوچکِ authorization lab است؛ lifecycle کامل aggregate نسخهٔ ۶۰ را به‌طور خودکار پیوند نزده‌ایم.
TicketService در این snapshot آداپتر کاربردی HTTP است و IResult می‌دهد؛ برای پروژهٔ نهایی قرارداد Application را طبق بخش معماری از پاسخ HTTP جدا کن.

## فهرست مسیرهای عمومی را عمدی نگه دار

پنج مسیر عمداً AllowAnonymous دارند: /health، /health/ready، /auth/csrf، /auth/login و /auth/refresh.
RequireAuthorization بدون نام و FallbackPolicy یک نقش ندارند؛ route بدون metadata نیز باید رفتار default deny داشته باشد.
Review/SecurityReview.cs فهرست RouteEndpoint واقعی را از DataSources می‌خواند؛ لیست دست‌نویسِ جدا از برنامه را موفق اعلام نمی‌کند.
بررسی می‌کند مجموعهٔ publicها دقیق باشد، routeهای تیکت BearerOnly و دارای budget باشند، audit policy مستقل داشته باشد و login/refresh budget داشته باشند.
سپس یک RouteEndpoint ساختگی AllowAnonymous به مجموعه اضافه می‌کند؛ validator باید آن را رد کند.
این endpoint آزمایشی روی HTTP ثبت نمی‌شود.

## اجرای CLI review

پس از تنظیم env نمونه و ساخت، بدون راه‌اندازی HTTP اجرا کن:

```powershell
dotnet run --project samples/69-security-review/SupportDesk.csproj -- --review
```

خروجی شامل PASS و JSON فهرست routeها، methodها، anonymous، policies و rateLimit است؛ connection و کلید و credential در آن نیست.
این دستور به اتصال زندهٔ DB نیاز ندارد ولی configuration معتبر می‌خواهد.
metadata نمی‌تواند منطق handler، SQL scope، اعتبارسنجی CSRF دستی یا امنیت client را ثابت کند؛ به همین دلیل HTTP و دیتابیس واقعی هم لازم‌اند.

## ماتریس سناریوهای منفی

| تهدید یا خرابی | انتظار | شاهد |
| --- | --- | --- |
| X-User/X-Role یا bearer ساختگی | 401 و بدون دسترسی خصوصی | همهٔ smokeهای امنیت |
| فقط role یا فقط permission | 403 برای report | security_next_smoke |
| حذف grant با token موجود | از دست رفتن دسترسی مربوط | next و final |
| شناسهٔ owner از peer یا tenant دیگر | 404 و عدم تغییر داده | next و final |
| body دارای ownerId یا tenantId | 400 و عدم درج | next و final |
| SQL-like search و wildcard | query محدود و دادهٔ literal | next |
| origin غیرمجاز | نبود header مجازکننده؛ auth مستقل | next |
| cookie بدون antiforgery | 400؛ bearer-only با cookie باز نشود | security_smoke و next |
| ۴۰ درخواست هم‌زمان با budget ۳۲ | ۳۲ موفق و ۸ پاسخ 429 | next |
| forwarded header یا email تصادفی | budget تازه نسازد | next |
| تنظیمات حساس یا schema اشتباه | startup ناموفق و پیام بدون canary | final |
| payload بزرگ | 413 پیش از پردازش حساب | final |
| خرابی ذخیرهٔ audit پس از update | 500 عمومی و rollback عنوان | final |
| audit سازمان دیگر یا بدون audit.read | داده افشا نشود | final |
| secret در title/query/credential | در app log و audit تست‌شده نباشد | final |
| restart با DB و key ثابت | داده و audit و token معتبر باقی بمانند | security_smoke و final |
| public route ناخواسته | شکست CLI review | final و --review |

برای هر تست توضیح بده «اگر این کنترل حذف شود، کدام assertion شکست می‌خورد؟»
خود CLI review یک mutation check دارد. ساخت موفق تنها به معنی اجرای سناریوهای DB نیست.
آزمون fault injection فقط روی schema تصادفی متعلق به تست اجرا و همان schema cleanup می‌شود.

## اجرای نمونه

[آزمایشگاه PostgreSQL](../../../handbook/postgresql-lab.md) و SDK 10 و Python 3 لازم‌اند.
یک database اختصاصیِ قابل دور ریختن استفاده کن. schema این نسخه با نمونهٔ قبلی مشترک نیست.
فرمان init فقط در CLI مورد اعتماد و حالت Development کار می‌کند و ایمیل واقعی ارسال نمی‌کند.

```powershell
$schema = 'auth70_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-private-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/69-security-review/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/69-security-review/SupportDesk.csproj -- --urls http://127.0.0.1:5070
```

TEAMDESK_KEYS باید absolute و خصوصی باشد؛ اعتبارسنجی absolute بودن، ACL یا encryption واقعی را ثابت نمی‌کند.
Include Error Detail و Log Parameters باید false باشند؛ فعال کردن آن‌ها باعث شکست startup می‌شود.
schema باید auth70_ و ۳۲ رقم hex کوچک باشد؛ public یا schema نسخهٔ دیگر پذیرفته نمی‌شود.
startup عادی جدول نمی‌سازد و init روی جدول موجود شکست می‌خورد، نه اینکه آن را جایگزین کند.
از رمز موقت برای تمام شش حساب همان آزمایش استفاده می‌شود؛ این روش ساخت حساب محصول نیست.
owner و peer و staff و roleonly و claimonly در alpha هستند و other در beta است.
staff نقش SupportAgent و permissionهای tickets.manage و audit.read دارد. roleonly permission ندارد و claimonly فقط tickets.manage دارد.
جدول کامل حساب‌ها و ورود bearer را در [درس ۶۵](../065-authorization/README.md) ببین؛ token را در URL یا log قرار نده.
از POST /auth/login، accessToken بگیر و در header با شکل Authorization: Bearer <accessToken> بفرست.
Cookie و CSRF و protected bearer و محدودیت‌های refresh و logout درس ۶۴ حفظ شده‌اند؛ token این نمونه JWT/OAuth استاندارد نیست.


## بررسی خودکار

```powershell
python scripts/check_content.py
python scripts/security_final_smoke.py --build-only
python scripts/security_final_smoke.py
```

--build-only هر دو snapshot را می‌سازد و startup نامعتبر، anonymous HTTP و بررسی CLI درس ۷۰ را اجرا می‌کند؛ audit واقعی DB را تأیید نمی‌کند.
حالت کامل به SUPPORTDESK_PG و psql نیاز دارد. متغیرهای SUPPORTDESK_TEST_PGHOST، SUPPORTDESK_TEST_PGPORT، SUPPORTDESK_TEST_PGDATABASE، SUPPORTDESK_TEST_PGUSER و SUPPORTDESK_TEST_PGPASSWORD را مطابق همان اتصال تنظیم کن؛ [فهرست بخش](../README.md) راهنما دارد.
اسکریپت schema و key ring موقت خودش را می‌سازد، خروجی publish را از پوشهٔ جدا اجرا می‌کند و فقط schema خودش را حذف می‌کند.
برای بازبینی کامل بخش، security_smoke و security_next_smoke را نیز اجرا کن؛ این اسکریپت جایگزین سناریوهای درس‌های قبلی نیست.


سه runner امنیت به‌ترتیب ۶۱–۶۴، ۶۵–۶۸ و ۶۹–۷۰ را پوشش می‌دهند.
CI ساخت و boundary را روی Windows/Linux و سناریوهای DB را روی PostgreSQL واقعی اجرا می‌کند.
نتیجهٔ هر commit را جدا ببین؛ موفقیت commit قبلی تأیید commit جدید نیست.

## ریسک‌های باقی‌ماندهٔ صریح

| مورد | وضعیت این تحویل | کار لازم برای محصول |
| --- | --- | --- |
| ثبت‌نام، ارسال ایمیل، MFA و بازیابی self-service | آزمایش‌های محدود و CLI حساب موقت | flow واقعی و delivery و rate limit و abuse test |
| نقش و سازمان و انتقال مالکیت | grant و tenant ثابت آزمایش؛ API مدیریت نداریم | authorization و audit و concurrency عملیات مدیریت |
| refresh reuse و ابطال فوری session | ledger مصرف نداریم؛ logout cookie، همهٔ bearerها را revoke نمی‌کند | سیاست session و revocation قابل آزمون |
| تغییر مجوز هم‌زمان با write | query فعلی بررسی می‌شود؛ transaction عضویت سراسری نداریم | مدل سازگاری و race test متناسب با حساسیت |
| key ring و secretهای production | پوشهٔ خصوصی/env آزمایش؛ vault/at-rest طراحی نشده | provider و ACL و backup و rotation |
| rate limit | حافظهٔ process؛ reset در restart؛ چند replica مشترک نیستند | gateway یا کنترل distributed و load test |
| audit | append در سطح API؛ DB role آزمایش DDL دارد | runtime role محدود، retention، آرشیو و tamper controls |
| diagnostics | app log منتخب اسکن شده | proxy/DB/telemetry خصوصی و redaction مستقل |
| مرورگر و XSS و proxy/TLS | HTTP header آزمایش شده؛ frontend نداریم | browser test، client rendering و deployment واقعی |
| انقضای زمانی و load و faultهای وسیع | همهٔ پنجره‌ها با زمان واقعی منتظر نمی‌مانند | time-provider test، load و resilience test |

این‌ها را با عنوان «باگ صفر» پنهان نکن. برای هر پذیرش ریسک، مالک تصمیم و شرط بازبینی بنویس.
هیچ گواهی امنیت یا مجوز انتشار production از موفقیت این lab نتیجه نمی‌شود.

## تمرین پایان بخش ششم

یک feature مستقل مانند note بساز؛ body فقط text داشته باشد و parent ticket از کاربر دیگر قابل تغییر نباشد.
scope خواندن و نوشتن و audit و eventهای کم‌داده و CSRF/scheme و budget را برای آن طراحی کن.
یک fault پس از تغییر و قبل از audit ایجاد کن و rollback را ثابت کن.
سپس یک گزارش کوتاه با assetها، actorها، ماتریس تست، نتیجه و ریسک باقی‌مانده بنویس.

### سرنخ

ابتدا actor و operation و resource و tenant را مشخص کن؛ بعد route و UI را اضافه کن.
کپی کردن status codeها بدون بررسی state، تحویل نیست.

## معیار عبور پایان بخش

- تست‌های سه runner و CLI review را اجرا و دامنهٔ هرکدام را توضیح بدهی.
- یک مسیر عمومی ناخواسته یا حذف کنترل دسترسی را عمداً ایجاد و شکست تست را مشاهده کنی.
- write شکست‌خورده نه state نیمه‌کاره و نه رویداد موفق داشته باشد.
- secret را برای debug چاپ نکنی و مشاهده‌پذیری خصوصی را جدا طراحی کنی.
- گزارش ریسک صادقانه و تمرین مستقل note را تحویل بدهی.

## منابع رسمی

بررسی در 2026-10-18:

- [ASP.NET Core security](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [Resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)

[قبلی](../069-secrets/README.md) · [فهرست بخش](../README.md)
[ادامه: درس ۷۱ و unit test](../../07-testing/071-unit/README.md). درس‌های ۷۱ تا ۷۴ آماده‌اند.
