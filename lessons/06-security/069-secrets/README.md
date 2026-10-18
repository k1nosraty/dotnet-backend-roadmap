# درس 069 — secrets و audit و دادهٔ حساس

## خروجی قابل مشاهده

تغییر تیکت و audit آن با هم ذخیره می‌شوند. اگر جدول audit موقتاً در دسترس نباشد، rename نیز rollback می‌شود.
رویداد امنیتی ساختار ثابت دارد و عنوان، ایمیل، رمز، query و token در آن چاپ نمی‌شوند.
پیش‌نیاز: [درس ۶۸](../068-rate-limits/README.md)، transaction و تنظیمات برنامه.

## secret با configuration معمولی فرق دارد

port و نام policy معمولاً secret نیستند؛ password دیتابیس، private key و credential حساب باید چرخهٔ حفاظت و rotation داشته باشند.
در این snapshot اتصال فقط از SUPPORTDESK_PG و پوشهٔ کلید فقط از TEAMDESK_KEYS خوانده می‌شوند؛ مقدار نمونهٔ واقعی در appsettings یا repo نداریم.
LabSettings یک class معمولی است، نه record با ToString تولیدشده که ممکن است همهٔ propertyها را چاپ کند.
این انتخاب مجوز serialize کردن configuration نیست؛ آن را به endpoint یا logger نفرست.
پیام startup نامعتبر ثابت است و raw connection یا inner exception parser را نگه نمی‌دارد.

env، secret manager رمزگذاری‌شده نیست؛ process و مدیر سیستم می‌توانند به آن دسترسی پیدا کنند.
User Secrets برای توسعه می‌تواند secret را بیرون از repo نگه دارد، ولی خودش encryption یا خزانهٔ production نیست.
این نمونه مستقیم env می‌خواند؛ اجرای user-secrets set به‌تنهایی منبع تنظیمات آن را عوض نمی‌کند.
برای اتصال User Secrets یا vault باید IConfiguration/Options را صریح به LabSettings وصل و precedence را تست کنی.
در production از secret provider کنترل‌شده، دسترسی حداقلی و روش rotation مناسب استفاده کن؛ انتخاب سرویس مشخص در این lab انجام نشده است.
اگر secret وارد git شد، حذف فایل از commit بعدی کافی نیست؛ credential را revoke/rotate و تاریخچه و محل‌های انتشار را بررسی کن.

## فایل‌های مهم

| فایل | مسئولیت |
| --- | --- |
| Hosting/LabSettings.cs | دریافت env و رد تنظیمات خطرناک بدون چاپ مقدار |
| Audit/AuditEntry.cs | مدل audit و vocabulary محدود رویداد عملیاتی |
| Audit/AuditAccess.cs | permission مستقل و query audit محدود به سازمان |
| Persistence/AccountContext.cs | mapping و index رکوردهای audit |
| Tickets/TicketService.cs | ذخیرهٔ اتمیک تغییر و audit، رویداد موفق پس از commit |
| Program.cs | policy logging، خطای عمومی و سقف body |

## دو خروجی متفاوت

| خروجی | کاربرد | دادهٔ مجاز نمونه |
| --- | --- | --- |
| structured security event در console | مشاهدهٔ موفقیت/رد/خطای عمومی | Code ثابت و در صورت نیاز ID داخلی actor و target |
| AuditEntries در PostgreSQL | شاهد ماندگار تغییر موفق تیکت | Id، زمان UTC، ActorId، TenantId، Kind، TargetId |

SecurityCode یک enum است؛ تابع logger پیام، body، header یا exception دلخواه نمی‌پذیرد.
رویدادهای LoginSucceeded و LoginRejected فقط نتیجه را می‌گویند و شناسهٔ ورود ادعاشده را ذخیره نمی‌کنند.
رویداد AccessDenied وجود قطعی یک منبع را ادعا نمی‌کند؛ منبع غایب نیز می‌تواند به همان 404 برسد.
رویداد ServerFailure جزئیات connection یا SQL را بیرون نمی‌دهد.
همهٔ تلاش‌های امنیتی و تغییرات role در این نمونه audit کامل ندارند؛ scope آن را به‌عنوان audit محصول معرفی نکن.

ILogger در transaction دیتابیس نیست. نوشتن console می‌تواند گم شود و حذف log هم ممکن است؛ شاهد durable این درس جدول auditِ همان transaction است.
شناسه‌های داخلی همچنان دادهٔ قابل ارتباط با اشخاص‌اند؛ کم کردن فیلدها مساوی ناشناس‌سازی کامل نیست.
خط‌مشی نگهداری، دسترسی، حذف/آرشیو و حفاظت از backup باید تعیین شود. این lab job retention ندارد.

## چرا ثبت بعد از update به‌تنهایی کافی نیست؟

Create، entity تیکت و AuditEntry را پیش از یک SaveChanges اضافه می‌کند؛ provider تراکنشی هر دو را با هم commit می‌کند.
Rename ابتدا transaction صریح باز می‌کند، ExecuteUpdate محدود به مالک/سازمان را انجام می‌دهد و سپس audit را با SaveChanges ذخیره می‌کند.
فقط بعد از CommitAsync رویداد TicketRenamed منتشر می‌شود.
اگر audit fail شود، dispose شدن transaction تغییر قبلی را rollback می‌کند؛ موفقیت ظاهری بدون شاهد ذخیره‌شده نداریم.
این قرارداد هزینه و availability دارد: در این نمونه خرابی audit یعنی شکست mutation.
اگر audit را به سرویس خارجی بفرستی، transaction محلی آن را اتمیک نمی‌کند؛ outbox و تحویل دوباره و idempotency طراحی دیگری می‌خواهد.

## دسترسی به audit

GET /api/staff/audit به ViewAudit و نقش SupportAgent و permission فعلی audit.read نیاز دارد.
داشتن tickets.manage به‌تنهایی کافی نیست. حذف audit.read باید همان token فعلی را از audit محروم کند ولی گزارش معمول staff را باز نگه دارد.
query فقط TenantId فعلی staff و حداکثر ۵۰ رکورد اخیر را برمی‌گرداند؛ دادهٔ tenant دیگر وارد response نمی‌شود.
projection عمومی TenantId، عنوان، ایمیل و credential را بیرون نمی‌دهد.
این API نوشتن، حذف یا ویرایش audit ندارد، ولی role دیتابیس آزمایش مجوز DDL دارد؛ جدول tamper-proof یا WORM نیست.
در محیط واقعی role مهاجرت و role runtime را جدا و دسترسی به audit و backup را محدود کن.

## logging و خطای قابل انتشار

JSON console فعال است و request-start عمومی که URL/query را چاپ می‌کند در سطح Information فعال نیست.
فقط دستهٔ SecurityEvents و اطلاعات ثابت lifecycle اجازهٔ Information دارند؛ EF و middleware خطای عمومی جزئیات exception را چاپ نمی‌کنند.
این policy مخصوص lab برای جلوگیری از نشت در تست است؛ diagnostics خصوصیِ عملیاتی با redaction و دسترسی محدود، طراحی جدا می‌خواهد.
پاسخ خطا 500 عمومی است و stack trace یا SQL ندارد؛ خطای binding همچنان 400 است.
سقف body در Kestrel شانزده KiB است؛ payload بزرگِ آزمون login باید 413 بگیرد.
JSON logger به ساختار کمک می‌کند، اما اگر دادهٔ حساس را به آن بدهی، خود JSON شدن آن را امن نمی‌کند.

## اجرای نمونه

[آزمایشگاه PostgreSQL](../../../handbook/postgresql-lab.md) و SDK 10 و Python 3 لازم‌اند.
یک database اختصاصیِ قابل دور ریختن استفاده کن. schema این نسخه با نمونهٔ قبلی مشترک نیست.
فرمان init فقط در CLI مورد اعتماد و حالت Development کار می‌کند و ایمیل واقعی ارسال نمی‌کند.

```powershell
$schema = 'auth69_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-private-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/68-secrets-audit/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/68-secrets-audit/SupportDesk.csproj -- --urls http://127.0.0.1:5079
```

TEAMDESK_KEYS باید absolute و خصوصی باشد؛ اعتبارسنجی absolute بودن، ACL یا encryption واقعی را ثابت نمی‌کند.
Include Error Detail و Log Parameters باید false باشند؛ فعال کردن آن‌ها باعث شکست startup می‌شود.
schema باید auth69_ و ۳۲ رقم hex کوچک باشد؛ public یا schema نسخهٔ دیگر پذیرفته نمی‌شود.
startup عادی جدول نمی‌سازد و init روی جدول موجود شکست می‌خورد، نه اینکه آن را جایگزین کند.
از رمز موقت برای تمام شش حساب همان آزمایش استفاده می‌شود؛ این روش ساخت حساب محصول نیست.
owner و peer و staff و roleonly و claimonly در alpha هستند و other در beta است.
staff نقش SupportAgent و permissionهای tickets.manage و audit.read دارد. roleonly permission ندارد و claimonly فقط tickets.manage دارد.
جدول کامل حساب‌ها و ورود bearer را در [درس ۶۵](../065-authorization/README.md) ببین؛ token را در URL یا log قرار نده.
از POST /auth/login، accessToken بگیر و در header با شکل Authorization: Bearer <accessToken> بفرست.
Cookie و CSRF و protected bearer و محدودیت‌های refresh و logout درس ۶۴ حفظ شده‌اند؛ token این نمونه JWT/OAuth استاندارد نیست.


## سناریوی مشاهده

با owner یک تیکت بساز و rename کن. با staff، audit را بخوان و فقط دو نوع TicketCreated و TicketRenamed را ببین.
با peer همان route audit را بزن؛ باید 403 بگیری. other یک تیکت جدا بسازد؛ audit سازمان alpha نباید آن را نشان دهد.
در تست خودکار، جدول audit در schema تصادفی موقتاً تغییر نام می‌دهد، mutation شکست می‌خورد، جدول بازگردانده می‌شود و عنوان و تعداد تیکت دوباره سنجیده می‌شوند.
این fault injection را روی دیتابیس محصول اجرا نکن.

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


تست‌ها canary تصادفی در password، title، email، query و bearer نامعتبر می‌گذارند و app log و audit response و rowهای ذخیره‌شده را اسکن می‌کنند.
همچنین وجود رویدادهای ثابت و نبود رویداد موفق برای write شکست‌خورده را می‌سنجند.
این اسکن فقط مسیرهای اجراشده و app log این host را پوشش می‌دهد؛ نشت در proxy، driver tracing، DB logs یا ابزار مانیتورینگ آینده را ثابت نمی‌کند.

## تمرین مستقل

برای تغییر نقش، جدول «چه کسی، چه تغییری، روی چه منبع، در چه زمان» طراحی کن و داده‌های ممنوع در audit را هم بنویس.
برای نگهداری ۹۰روزه یک policy مکتوب و تست حذف/آرشیو بساز؛ حذف بدون policy یا نگهداری دائمیِ پیش‌فرض نداشته باش.
یک adapter تنظیمات برای IConfiguration بساز و نشان بده missing و malformed، بدون چاپ secret شکست می‌خورند.

### سرنخ

رویداد موفق را بعد از commit بنویس. برای اثبات rollback، خطا را پس از update و پیش از commit ایجاد کن.

## معیار عبور

- startup خطرناک بدون چاپ مقدار حساس رد شود.
- mutation بدون audit ماندگار موفق اعلام نشود.
- audit سازمان دیگر و permission نادرست در دسترس نباشد.
- canaryها در log و audit مسیرهای آزمون‌شده پیدا نشوند.
- تفاوت env، vault، operational log و audit و محدودیت tamper protection را توضیح بدهی.

## منابع رسمی

بررسی در 2026-10-18:

- [App secrets و محدودیت User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)

[قبلی](../068-rate-limits/README.md) · [فهرست بخش](../README.md) · [بعدی](../070-security-review/README.md)
