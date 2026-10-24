# درس 066 — کنترل مالکیت و جلوگیری از IDOR

## خروجی قابل مشاهده

owner یک تیکت ایجاد می‌کند و می‌خواند و عنوانش را عوض می‌کند.
peer از همان سازمان و other از سازمان دیگر، حتی با شناسهٔ درست، اجازهٔ خواندن و تغییر ندارند.
پیش‌نیاز: [درس ۶۵](../065-authorization/README.md)، query و mapping و مدل داده.

## تهدید واقعی

IDOR یعنی client با عوض کردن شناسه به منبعی دست پیدا کند که مجوزش را ندارد.
GUID قابل حدس نبودن را بهتر می‌کند، ولی مجوز نیست؛ لینک لو رفته یا شناسهٔ یک export همچنان نباید در را باز کند.
مالکیت را با userId پایدار Identity می‌سنجیم، نه email قابل تغییر یا X-Owner-Id.
TenantId نیز از claim فعلیِ server-side در دیتابیس می‌آید؛ client نمی‌تواند سازمان را در body انتخاب کند.

## قرارداد داده و مسیرها

این مدل کوچک برای تمرین authorization است و همهٔ invariantهای lifecycle نسخهٔ ۶۰ را کپی نمی‌کند.
انتقال مالکیت و حذف در این snapshot وجود ندارد.

| مسیر | رفتار |
| --- | --- |
| POST /api/tickets | فقط Title؛ ساخت OwnerId و TenantId از actor سروری؛ 201 |
| GET /api/tickets | فقط داده‌های همان مالک و سازمان؛ حداکثر ۵۰ ردیف |
| GET /api/tickets/{id} | بررسی منبع و مالک و سازمان؛ 200 یا 404 |
| POST /api/tickets/{id}/title | همان کنترل دسترسی، اعتبارسنجی title، تغییر محدود؛ 204 یا 404 |

همهٔ این routeها BearerOnly هستند؛ cookie تنها برای mutation کافی نیست.
body با فیلد ownerId یا tenantId به‌دلیل قرارداد DTO صریح و Disallow برای اعضای ناشناخته، 400 می‌گیرد.
RouteHandlerOptions.ThrowOnBadRequest را صریح false کرده‌ایم تا خطای binding در Development نیز 400 بماند و middleware خطای عمومی آن را 500 نکند.
به entity دارای OwnerId از body bind نمی‌کنیم و entity client را Attach/Update نمی‌کنیم.
پاسخ فقط Id و Title دارد؛ اطلاعات داخلی حساب و سازمان serialize نمی‌شوند.

## مرز کلاس‌ها

| فایل | دلیل وجود |
| --- | --- |
| Tickets/OwnedTicket.cs | persistence model و DTOهای محدود |
| Authorization/AccountAccess.cs | actor فعلی از حساب موجود و tenant معتبر |
| Tickets/TicketOwnerHandler.cs | requirement منبع برای Read و Rename |
| Tickets/TicketService.cs | scope خواندن، authorize و write محدود |
| Tickets/TicketEndpoints.cs | نگاشت HTTP و scheme، بدون تصمیم مالکیت تکراری |
| Persistence/AccountContext.cs | index مالک/سازمان و FK حساب |

handler سفارشی AuthorizationHandler<TicketOperation, OwnedTicket>، resource واقعی را می‌بیند.
شرط موفقیت: operation شناخته‌شده و UserId برابر OwnerId و TenantId برابر TenantId منبع.
نقش SupportAgent استثنای سراسری نیست؛ گزارش کارکنان با دسترسی به تیکت دیگران یکی نیست.
برای دسترسی کارکنان آینده، scope و operation و سیاست مستقل لازم است.

## جریان یک درخواست

authentication principal معتبر را می‌سازد. endpoint scheme را محدود می‌کند.
service منبع را می‌خواند و IAuthorizationService.AuthorizeAsync را با requirement مربوط صدا می‌زند.
handler actor فعلی را بررسی می‌کند. سپس service فقط projection مجاز یا تغییر محدود را اجرا می‌کند.
داشتن RequireAuthorization روی endpoint به‌تنهایی اطلاعات OwnerId منبع را بررسی نمی‌کند.

در query فهرست، شرط tenant و owner پیش از search و Take و projection اعمال می‌شود.
اگر ابتدا همه را بخوانیم و در UI فیلتر کنیم، داده قبلاً از مرز اعتماد عبور کرده است.
در rename نیز predicate SQL شامل Id و OwnerId و TenantId است؛ update فقط یک ردیف مورد انتظار را هدف می‌گیرد.
مالکیت در HTTP قابل تغییر نیست. اگر بعداً انتقال مالکیت اضافه شد، رقابت transfer و update و عضویت سازمان باید با قرارداد concurrency یا transaction بررسی شود.
این lab کنترل هم‌زمانی ویرایش عنوان ندارد؛ دو rename مجاز می‌توانند last-write-wins باشند.

## پاسخ 404 و محدودیت آن

نبودن منبع و نداشتن مجوزِ منبع هر دو 404 با body خالی می‌دهند تا وجود منبع را مستقیم اعلام نکنیم.
این تصمیم تضمین یکسان بودن زمان پاسخ یا حذف تمام side channelها نیست.
کاربر ناشناس همچنان 401 و body نامعتبر 400 می‌گیرد.
فهرست دیگران آرایهٔ خالی است؛ تعداد کل منابع تمام سازمان‌ها هم برگردانده نمی‌شود.
تیکت تازه‌ای در نقش مستقیم database reader دیده می‌شود؛ این برنامه DB row-level security پیاده نکرده است.
مرز این درس، API و سرویس کاربردی آن است، نه محدود کردن کاربر دارای credential دیتابیس.

## راه‌اندازی آزمایشگاه

ابتدا [راهنمای PostgreSQL](../../../handbook/postgresql-lab.md) را انجام بده.
یک database اختصاصی با role دارای مجوز ساخت schema لازم است؛ از database محصول استفاده نکن.
هر نمونه schema و key ring مستقل دارد و جدول موجود را جایگزین نمی‌کند.
فرمان init، ساخت حساب را فقط در CLI محلیِ Development انجام می‌دهد؛ endpoint ثبت‌نام یا اعطای نقش وجود ندارد.

```powershell
$schema = 'auth66_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-access-keys-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/65-resource-ownership/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/65-resource-ownership/SupportDesk.csproj -- --urls http://127.0.0.1:5066
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


## سناریوی دستی

با owner وارد شو و یک title مانند «اختلال شبکه» ثبت کن. id پاسخ را نگه دار.
با peer همان id را بخوان و rename کن؛ هر دو باید 404 شوند.
با other و staff نیز تکرار کن. سپس با owner بخوان و ثابت کن تغییر غیرمجاز ذخیره نشده است.
در body یک ownerId دیگر بفرست؛ 400 بگیر و دوباره فهرست را بررسی کن تا درج جدیدی رخ نداده باشد.
تست خودکار همچنین tenant فعلی owner را در DB تغییر می‌دهد و با token قبلی از دست دادن دسترسی را می‌سنجد.

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

قابلیت note را اضافه کن. client فقط text بفرستد؛ انتخاب ticket متعلق به دیگری نباید امکان ثبت note بدهد.
برای دریافت، فهرست و mutation هرکدام تست منفی مستقل بنویس.
نسخهٔ منبع را به تغییر title اضافه کن و قرارداد برخورد دو rename مجاز را توضیح بده.

### سرنخ

handler مالکیت را برای operation جدید بازبینی کن؛ شرط ناشناخته را خودکار موفق نکن.
برای تست صرفاً status را نسنج؛ با مالک اصلی دوباره بخوان و عدم تغییر داده را ثابت کن.

## معیار عبور

- peer و other و staff با شناسهٔ درست نتوانند منبع owner را بخوانند یا تغییر دهند.
- شرط owner و tenant در فهرست و شرط write وجود داشته باشد.
- tenant عوض‌شده با token قدیمی دسترسی قدیمی ندهد.
- فیلدهای مالکیت از DTO پذیرفته نشوند.
- پنهان کردن دکمه و استفاده از GUID را جایگزین authorize ندانی.

## منابع رسمی

بررسی در 2026-10-18:

- [Resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)
- [ExecuteUpdate و تغییر محدود](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)

[قبلی](../065-authorization/README.md) · [فهرست بخش](../README.md) · [بعدی](../067-web-security/README.md)
