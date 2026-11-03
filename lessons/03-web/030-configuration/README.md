# درس ۰۳۰ — configuration و options و secrets

[قبلی](../029-middleware/README.md) · [خانه](../../../README.md) · [بعدی](../031-di-scopes/README.md)

## مسئله و خروجی

فرض کن اندازهٔ پیش‌فرض صفحه باید مثبت و از سقف صفحه کوچک‌تر باشد. به‌جای خواندن رشته و تبدیل آن در هر action، تنظیمات را در یک کلاس با propertyهای مشخص یا `typed options` می‌گیریم. هنگام startup، مقدارها و رابطه‌شان را بررسی می‌کنیم تا تنظیم اشتباه پیش از دریافت درخواست کاربر آشکار شود. محل نگه‌داری secretها را هم از فایل تنظیمات عمومی جدا می‌کنیم.
نمونه برای مشاهدهٔ مقدار تنظیمات یک endpoint موقت settings دارد.
در درس pagination همین options رفتار واقعی فهرست را کنترل می‌کند.

## ۱. پیکربندی با منطق فرق دارد

پورت، اندازهٔ پیش‌فرض صفحه و مقدار endpoint سرویس بیرونی می‌توانند configuration باشند.
قاعدهٔ «Ticket بسته بدون note مجاز نیست» قانون domain است، نه یک setting اتفاقی محیط.
برای هر مقدار قابل تغییر مشخص کن چه کسی، چه زمانی و با چه اثری آن را تغییر می‌دهد.
قرار دادن یک قانون مهم در JSON بدون سیاست تغییر، انعطاف مفید تضمین نمی‌کند.

## ۲. نوع تنظیمات

```csharp
public sealed class TicketOptions
{
    public const string SectionName = "Tickets";
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 50;
}
```

این کلاس در Configuration است و DTO ورودی کاربر نیست.
بخشی که تنظیمات را می‌خواند، یعنی `configuration binder`، با setterها مقدار کلیدها را در این کلاس قرار می‌دهد. مقدار پیش‌فرض برای زمانی است که کلید ارسال نشده باشد؛ اگر صریحاً صفر تنظیم شود، همان صفر باید بررسی و در صورت نامعتبر بودن رد شود. این کلاس تنظیمات، entity داده‌های درخواست نیست.
نام section را متمرکز نگه می‌داریم تا typo در چند فایل تکرار نشود.

## ۳. فایل تنظیمات

```json
{
  "Tickets": { "DefaultPageSize": 20, "MaxPageSize": 50 }
}
```

فایل appsettings.json نمونه شامل بخش Logging نیز هست.
فایل JSON باید syntax درست داشته باشد.
در مسیر اجرای مستقیم DLL، content root و محل فایل مهم‌اند.
تست publish برنامه را از پوشهٔ خروجی اجرا می‌کند تا خواندن فایل به source وابسته نماند.

## ۴. bind و validate

```csharp
builder.Services.AddOptions<TicketOptions>()
    .Bind(builder.Configuration.GetSection(TicketOptions.SectionName))
    .Validate(o => o.DefaultPageSize >= 1 && o.DefaultPageSize <= o.MaxPageSize,
        "DefaultPageSize must be between 1 and MaxPageSize.")
    .Validate(o => o.MaxPageSize >= 1 && o.MaxPageSize <= 100,
        "MaxPageSize must be between 1 and 100.")
    .ValidateOnStart();
```

متد `Bind` کلیدهای بخش تنظیمات را به propertyهای این کلاس وصل می‌کند. سپس `Validate` علاوه بر مقدار هر property، رابطهٔ آن‌ها را بررسی می‌کند؛ مثلاً مقدار پیش‌فرض نباید از سقف بزرگ‌تر باشد. متد `ValidateOnStart` این بررسی را هنگام شروع host اجرا می‌کند، به‌جای اینکه اولین درخواست کاربر خطای تنظیمات را آشکار کند.
برای مقدار خطای syntax یا تبدیل مثل abc به int ممکن است binding زودتر شکست بخورد.
این failureها خطای تنظیمات operator هستند و نباید با request ۴۰۰ client اشتباه شوند.

## ۵. fail fast

در این نمونه با تنظیم `DefaultPageSize=0`، شروع host با `OptionsValidationException` شکست می‌خورد. به توقف زودهنگام پس از کشف مشکل، `fail fast` می‌گویند. به این ترتیب برنامه با تنظیمی که از همان ابتدا نامعتبر است ساعت‌ها باز نمی‌ماند تا خطا در مسیر یک کاربر دیده شود.
در CI هم این رفتار با process جدا و timeout بررسی می‌شود.
تست باید پایان ناموفق process را تأیید کند، نه فقط وجود یک رشته در log موفق.

## ۶. مصرف با IOptions

```csharp
public sealed class TicketsController(TicketService service,
    IOptions<TicketOptions> options) : ControllerBase
```

در این snapshot مسیر GET /api/tickets/settings مقدار Value را به صورت دادهٔ غیرحساس نشان می‌دهد.
IOptions<T> یک view از options کش‌شدهٔ پیش‌فرض می‌دهد.
تغییر فایل را معادل refresh خودکار این interface فرض نکن.
controller هم نباید مقدار options مشترک را هنگام request mutate کند.
این endpoint آموزشی در درس pagination حذف می‌شود و تنظیمات مستقیماً مصرف می‌شوند.

## ۷. انتخاب interface

| interface | lifetime معمول | کاربرد |
| --- | --- | --- |
| IOptions<T> | singleton wrapper | تنظیمات ساده بدون refresh مورد انتظار |
| IOptionsSnapshot<T> | scoped | خواندن snapshot در scope درخواست |
| IOptionsMonitor<T> | singleton | دسترسی به مقدار جاری و اعلان تغییر providerهای پشتیبان |

وجود Monitor به تنهایی reload هر منبع را تضمین نمی‌کند؛ provider باید تغییر را پشتیبانی کند.
environment variable را در process دیگری عوض کردن معمولاً process در حال اجرا را refresh نمی‌کند.
Snapshot را در singleton تزریق نکن.
هزینه و زمان اعمال تغییر را پیش از انتخاب interface مشخص کن.

## ۸. environment override

```powershell
$env:Tickets__DefaultPageSize = "3"
$env:Tickets__MaxPageSize = "8"
```

برنامهٔ server را با همین session اجرا کن و settings را بخوان.
انتظار defaultPageSize=3 و maxPageSize=8 داریم.
برای آزمایش failure برنامه را متوقف و DefaultPageSize را ۹ بگذار؛ رابطهٔ دو عدد نقض می‌شود.
پس از آزمایش process را متوقف و متغیرهای تمرین را پاک کن.

```powershell
Remove-Item Env:Tickets__DefaultPageSize
Remove-Item Env:Tickets__MaxPageSize
```

## ۹. precedence

هر `configuration provider` از یک منبع، مثل فایل یا environment variable، مقدار می‌خواند. در تنظیمات پیش‌فرض `CreateBuilder`، فایل appsettings، فایل مخصوص environment، user secrets در Development، environment variables و command line به‌ترتیب اضافه می‌شوند. برای یک کلید مشترک، منبعی که دیرتر اضافه شده مقدار قبلی را override می‌کند؛ پس command line نسبت به مقدار فایل اولویت بالاتری دارد.
درس ۰۲۴ این رفتار را با greeting آزمایش کرد.
اگر providerها را جابه‌جا کنی، باید precedence پروژهٔ واقعی را دوباره مستند کنی.
IOptions خودش منبع secret یا لایهٔ رمزنگاری نیست.

## ۱۰. User Secrets برای توسعه

نمونه UserSecretsId دارد و از ریشهٔ دوره می‌توان یک مقدار ساختگی ثبت کرد:

```powershell
dotnet user-secrets set "Demo:ApiKey" "local-demo-only" --project samples/29-options/SupportDesk.csproj
dotnet user-secrets remove "Demo:ApiKey" --project samples/29-options/SupportDesk.csproj
```

این کلید در نمونه مصرف نمی‌شود و صرفاً برای تمرین محل نگهداری خارج از repository است.
مقدار واقعی حساب خودت را برای این تمرین وارد نکن.
User Secrets محل توسعهٔ محلی است و storage آن رمزنگاری امن برای production محسوب نمی‌شود.
فایل secrets را commit یا در log و endpoint settings نمایش نده.

## ۱۱. مرز production secrets

در استقرار واقعی روش دریافت credential به محیط اجرا و سیاست دسترسی وابسته است.
متغیر محیطی هم ممکن است در ابزارهای مدیریتی دیده شود و ذاتاً secret manager کامل نیست.
تعویض کلید، سطح دسترسی و audit نیازمند طراحی‌اند.
در این پروژه هنوز credential واقعی یا اتصال دیتابیس نداریم.
بخش امنیت و استقرار این مسیر را کامل می‌کند.

## بررسی عملی

با defaultها settings باید ۲۰ و ۵۰ نشان دهد.
با environment باید ۳ و ۸ دیده شود.
با default صفر، max بالاتر از ۱۰۰ یا default بزرگ‌تر از max شروع host باید شکست بخورد.
هیچ‌یک از این خطاها نباید با GET موفق به client پنهان شوند.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/29-options/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5300
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5300/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5300/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5300/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/29-options/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

یک option برای حداکثر طول فیلتر طراحی کن و رابطهٔ آن با قرارداد API را توضیح بده.
مقدار ضروری ناموجود، مقدار خراب و مقدار معتبر را جدا تست کن.
تصمیم بگیر این مقدار در startup ثابت است یا reload لازم دارد و دلیل بنویس.

## معیار عبور

- [ ] binding و validation options را تفکیک می‌کنم.
- [ ] تنظیمات ضروری خراب مانع startup می‌شوند.
- [ ] IOptions و Snapshot و Monitor کاربرد یکسان ندارند.
- [ ] secrets را در appsettings عمومی یا response نمی‌گذارم.
- [ ] می‌دانم endpoint settings این درس ابزار آموزشی موقت است.

## منابع رسمی

- [Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0)
- [Configuration providers](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)
- [Development secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
