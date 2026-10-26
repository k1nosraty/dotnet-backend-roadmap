# درس 065 — policy و نقش و مجوز

## خروجی قابل مشاهده

کاربر واردشده با کاربر مجاز یکی نیست: owner از گزارش کارکنان 403 می‌گیرد و staff پاسخ 200 می‌گیرد.
داشتن فقط نقش یا فقط permission کافی نیست. حذف grant در دیتابیس، دسترسی همان bearer قبلی به گزارش را قطع می‌کند.
پیش‌نیاز: [درس ۶۴](../064-authentication/README.md)، DI و Identity.

## مسئلهٔ پروژه

گزارش کارکنان یک قابلیت حساس است. قرار دادن دکمه فقط در UI یا ارسال X-Role نمی‌تواند آن را محدود کند.
ابتدا تصمیم دسترسی را بنویس: حساب تأییدشدهٔ موجود، نقش SupportAgent و permission با مقدار tickets.manage، هر سه لازم‌اند.
این گزارش فقط یک status آزمایشی دارد؛ فهرست اطلاعات سازمان‌ها یا کاربران را افشا نمی‌کند.

| درخواست به GET /api/staff/report | نتیجه |
| --- | --- |
| بدون credential یا bearer خراب | 401 |
| owner با credential معتبر | 403 |
| roleonly یا claimonly | 403 |
| staff با هر دو grant | 200 |
| owner با X-Role و X-Permission جعلی | 403 |

401 یعنی authentication کافی نداریم. 403 یعنی principal شناخته شده، policy برقرار نیست.
برای منبع حساس در درس بعد، 404 را برای پنهان کردن وجود منبع انتخاب می‌کنیم؛ پاسخ‌ها در همهٔ endpointها الزاماً یکسان نیستند.

## تقسیم مسئولیت

| فایل | مسئولیت |
| --- | --- |
| Accounts/AccountServices.cs | AddRoles و store حساب، بدون دریافت نقش از body |
| Authorization/AccountAccess.cs | requirement، handler و ثبت policy |
| Hosting/LabProvisioning.cs | ساخت نقش و grant آزمایش در CLI مورد اعتماد |
| Program.cs | اعمال policy روی route و fallback برای route فراموش‌شده |

ManageTicketsRequirement قرارداد تصمیم است و ManageTicketsHandler آن را اجرا می‌کند.
handler از UserManager حساب، role membership و permission فعلی را می‌خواند و فقط در صورت موفقیت context.Succeed را صدا می‌زند.
عدم Succeed مجوز ایجاد نمی‌کند. handler به‌صورت Scoped ثبت می‌شود چون UserManager و DbContext scoped هستند.
ثبت singleton برای چنین handlerی، lifetime نامعتبر و state ناخواسته ایجاد می‌کند.

## policy چه چیزی را جمع می‌کند؟

RequireAuthenticatedUser و requirement سفارشی در یک policy، هر دو باید برقرار باشند.
در این lab نقش و permission داخل یک handler با شرط AND بررسی می‌شوند.
در طراحی چند handler برای یک requirement مراقب OR ناخواسته باش؛ موفقیت یک handler می‌تواند برای آن requirement کافی باشد.
نام policy باید intent را نشان بدهد؛ endpoint نباید منطق خام role و claim را چند بار تکرار کند.

DefaultPolicy برای RequireAuthorization بدون نام است. FallbackPolicy برای endpoint بدون metadata صریح authorization کاربرد دارد.
/api/private-default عمداً RequireAuthorization ندارد تا فعال بودن fallback با HTTP بررسی شود.
/health و /auth/login عمداً AllowAnonymous دارند؛ fallback مانع AllowAnonymous صریح نمی‌شود.
این انتخاب‌ها را با فهرست routeها مرور کن، صرف ثبت fallback پوشش همهٔ رفتارها را ثابت نمی‌کند.

## grant قدیمی در cookie یا token

role/claim داخل credential ممکن است تا refresh یا صدور دوباره قدیمی بماند.
این نمونه برای گزارش حساس به آن snapshot تکیه نمی‌کند و grant فعلی دیتابیس را می‌خواند.
تست، permission را در DB حذف می‌کند و همان token قبلی را دوباره می‌فرستد؛ پاسخ باید 403 شود.
این کار هزینهٔ query دارد؛ cache بعدی باید قرارداد ابطال و آزمون revocation داشته باشد.
این خواندن‌ها یک transaction سراسری برای تغییر grant هم‌زمان نیستند؛ تصمیم در لحظهٔ بررسی گرفته می‌شود.
برای عملیات بسیار حساس، سازگاری زمان بررسی و commit را جدا طراحی کن.
فقدان دیتابیس نباید مسیر را آزاد کند؛ خطای فنی 500 عمومی است، نه موفقیت پیش‌فرض.

## راه‌اندازی آزمایشگاه

ابتدا [راهنمای PostgreSQL](../../../handbook/postgresql-lab.md) را انجام بده.
یک database اختصاصی با role دارای مجوز ساخت schema لازم است؛ از database محصول استفاده نکن.
هر نمونه schema و key ring مستقل دارد و جدول موجود را جایگزین نمی‌کند.
فرمان init، ساخت حساب را فقط در CLI محلیِ Development انجام می‌دهد؛ endpoint ثبت‌نام یا اعطای نقش وجود ندارد.

```powershell
$schema = 'auth65_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-access-keys-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/64-policies/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/64-policies/SupportDesk.csproj -- --urls http://127.0.0.1:5065
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


## مسیر دستی یادگیری

یک بار با owner وارد شو؛ /api/private-default باید 200 و /api/staff/report باید 403 باشد.
بعد roleonly و claimonly را امتحان کن؛ هر دو 403 هستند. staff باید 200 بگیرد.
X-Role را به درخواست owner اضافه کن و نشان بده نتیجه عوض نمی‌شود.
کد handler را از route تا DI و سپس UserManager دنبال کن؛ credential به‌تنهایی permission نیست.

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


## تمرین مستقل

policy دوم با نام ViewAudit طراحی کن که permission جدا بخواهد و با ManageTickets یکی نباشد.
پیش از کدنویسی جدول چهار حالتِ بدون grant، فقط نقش، فقط permission و هر دو را بنویس.
route اعطای role عمومی اضافه نکن؛ چرخهٔ مدیریت مجوز خودش authorization، audit و کنترل ارتقای سطح دسترسی می‌خواهد.

### سرنخ

requirement را از endpoint مستقل نگه دار و تغییر grant را با token موجود آزمایش کن.
تست UI برای پنهان بودن دکمه کافی نیست؛ درخواست مستقیم HTTP را بزن.

## معیار عبور

- 401 و 403 را در دو وضعیت متفاوت بازتولید کنی.
- نقش تنها و permission تنها هیچ‌کدام گزارش را باز نکنند.
- حذف permission، دسترسی همان token قبلی را قطع کند.
- route بدون metadata با fallback ناشناس را رد کند.
- هزینهٔ query و محدودیت سازگاری مجوز هنگام commit را توضیح بدهی.

## منابع رسمی

بررسی در 2026-10-18:

- [Policy و requirement و handler](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
- [Role-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0)

[قبلی](../064-authentication/README.md) · [فهرست بخش](../README.md) · [بعدی](../066-ownership/README.md)

پیش از این درس می‌توانی [مکمل JWT](../064a-jwt/README.md) و [مکمل OIDC](../064b-oidc/README.md) را اجرا کنی؛ policyهای این درس همچنان روی نمونهٔ Identity خودش هستند.
