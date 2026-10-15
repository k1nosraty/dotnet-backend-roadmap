# درس ۰۲۴ — host، pipeline و تنظیمات اجرا

[قبلی](../023-http/README.md) · [خانه](../../../README.md) · [بعدی](../025-routing/README.md)

## خروجی درس

یک host ASP.NET Core را از build تا اجرا توضیح می‌دهی.
configuration و environment را تنظیم می‌کنی و نتیجه را از HTTP می‌بینی.
middleware ساده‌ای قبل از endpoint اجرا می‌کنی و lifetime سرویس demo را مشاهده می‌کنی.

## پیش‌نیاز

درس ۰۲۳ و DI و lifetime در درس ۰۱۶.
SDK 10 شامل ابزار ساخت است و نمونه از Web SDK استفاده می‌کند.
برای این درس package تازه‌ای نصب نمی‌کنیم.

## مسئله

یک endpoint تنها یک متد نیست؛ host باید پورت را باز کند، dependencyها را بسازد و requestها را از pipeline عبور دهد.
می‌خواهیم بدانیم تنظیمات کجا خوانده می‌شوند و کدام کد هنگام startup یا برای هر request اجرا می‌شود.

## ۱. Web SDK

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Web SDK پیش‌فرض‌ها و referenceهای لازم برای برنامهٔ ASP.NET Core را فراهم می‌کند.
نمونهٔ واقعی TreatWarningsAsErrors هم فعال دارد.
این با اضافه کردن دستی هر DLL وب به project کنسولی فرق دارد.
server وب پیش‌فرض این مسیر معمولاً Kestrel است.
نقش reverse proxy و TLS در فصل استقرار بررسی می‌شود.

## ۲. ساخت builder

```csharp
var builder = WebApplication.CreateBuilder(args);
```

builder نقطهٔ ثبت serviceها و استفاده از configuration و environment است.
CreateBuilder پیش‌فرض‌های host و تنظیمات متداول را آماده می‌کند.
args می‌تواند configuration خط فرمان مانند --urls را وارد کند.
این مرحله برای هر request دوباره اجرا نمی‌شود.

## ۳. registration پیش از Build

```csharp
builder.Services.AddSingleton<StartupStamp>();
var app = builder.Build();
```

StartupStamp یک Guid برای هویت instance دارد.
singleton در یک provider به callerهای بعدی همان نمونه را می‌دهد.
با گرفتن /info دو بار، instanceId باید در همان اجرای process یکسان باشد.
با restart معمولاً instanceId تازه می‌شود.
این Guid برای authentication یا user identity نیست؛ فقط مشاهدهٔ عمر نمونه است.

## ۴. Build و Run

Build گراف host و برنامه را آماده می‌کند.
بعد middleware و endpointها روی app تعریف می‌شوند.
Run host را اجرا و تا shutdown منتظر می‌ماند.
ثبت سرویس‌های مورد نیاز را پس از Build انجام نده؛ برنامه باید با registrations مشخص ساخته شود.
در ASP.NET Core provider دوم برای گرفتن زودهنگام serviceها نساز.

## ۵. middleware ساده

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Course"] = "supportdesk";
    await next(context);
});
```

context نمای درخواست و پاسخ جاری است.
next ادامهٔ pipeline را صدا می‌زند.
Header قبل از شروع نوشتن response تنظیم می‌شود.
اگر next را صدا نزنی و پاسخ خودت را کامل کنی، مسیر کوتاه می‌شود؛ این می‌تواند عمدی باشد.
حذف تصادفی next ممکن است endpointها را از دسترس خارج کند.

## ۶. ترتیب pipeline

middleware می‌تواند قبل و بعد از ادامهٔ مسیر کد اجرا کند.
کد قبل از await next در جهت ورود و کد بعد از آن در جهت برگشت اجرا می‌شود.
اما header یا status را بعد از شروع response نمی‌توان آزادانه تغییر داد.
نمونه برای جلوگیری از این ابهام header را قبل از next می‌گذارد.
ترتیب مدیریت خطا، routing، authentication و authorization در فصل مربوط با مثال بررسی خواهد شد.

## ۷. endpointها

| مسیر | خروجی |
| --- | --- |
| /health | status برابر ok |
| /info | environment و instanceId |
| /greeting | مقدار configuration |

/health این درس فقط زنده بودن مسیر demo را نشان می‌دهد.
این endpoint اتصال دیتابیس یا readiness کامل محصول را بررسی نمی‌کند.
اطلاعات /info برای آموزش محلی است؛ افشای جزئیات environment در محصول باید آگاهانه باشد.

## ۸. configuration فایل

```json
{
  "Course": { "Greeting": "Welcome to SupportDesk" }
}
```

فایل نمونه appsettings.json علاوه بر این بخش، سطح logging را دارد.
کلید Course:Greeting مسیر بخش تو در تو را معرفی می‌کند.
JSON باید معتبر باشد؛ syntax خراب می‌تواند startup را متوقف کند.
secret واقعی را در فایل commitشدهٔ دوره نگذار؛ مدیریت secrets در درس پیکربندی می‌آید.

## ۹. مصرف configuration

```csharp
app.MapGet("/greeting", (IConfiguration configuration) => Results.Ok(new
{
    message = configuration["Course:Greeting"] ?? "Hello"
}));
```

IConfiguration از DI دریافت می‌شود.
اگر کلید نباشد fallback این نمونه Hello است.
برای تنظیم ضروری، fallback خاموش همیشه انتخاب مناسبی نیست؛ options و validation درس جدا دارند.
اینجا از یک greeting غیرحیاتی برای مشاهدهٔ precedence استفاده می‌کنیم.

## ۱۰. environment variable

در PowerShell:

```powershell
$env:Course__Greeting = "Hello from environment"
dotnet run --project samples/23-web-host/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5240
```

دو underscore شکل سازگار کلید تو در تو در environment variable است.
در تنظیمات پیش‌فرض این مسیر، مقدار environment مقدار فایل appsettings را override می‌کند.
پس GET /greeting باید متن environment را بدهد.
این متغیر متعلق به session ترمینال است؛ برای تست بعدی آن را پاک کن.

```powershell
Remove-Item Env:Course__Greeting
```

این دستور را وقتی دیگر به مقدار آن نیاز نداری اجرا کن.

## ۱۱. override خط فرمان

```powershell
dotnet run --project samples/23-web-host/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5240 --Course:Greeting="Hello from command line"
```

با پیش‌فرض‌های CreateBuilder، configuration برنامهٔ خط فرمان نسبت به environment و فایل اولویت بالاتری دارد.
اگر providerها را سفارشی کنی، precedence را باید دوباره تحلیل کنی.
این مثال با یک greeting کوتاه است؛ command line محل مناسب secretهایی که در history می‌مانند نیست.

## ۱۲. EnvironmentName

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project samples/23-web-host/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5240
```

مسیر /info باید Development را نشان دهد.
Development، Staging و Production نام‌های رایج‌اند، اما خود برنامه می‌تواند نام دیگری هم داشته باشد.
نام environment تنظیمات رفتار را هدایت می‌کند؛ به معنی user role نیست.
این نمونه مستقیماً DOTNET_ENVIRONMENT را استفاده می‌کند و launch profile را کنار می‌گذارد تا آزمایش مبهم نشود.

## ۱۳. launchSettings

launchSettings.json برای تجربهٔ اجرای محلی IDE و dotnet run کاربرد دارد.
نباید آن را configuration قابل اتکای production فرض کنی.
در نمونهٔ فعلی چنین فایل اضافه‌ای نداریم.
--no-launch-profile از دخالت profile محلی جلوگیری می‌کند.
برای اجرای DLL منتشرشده، تنظیمات runtime را صریح بده.

## ۱۴. پورت و آدرس

```powershell
dotnet run --project samples/23-web-host/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5240
```

--urls این نمونه آدرس listener را تعیین می‌کند.
اگر پورت اشغال باشد startup ممکن است شکست بخورد؛ با خواندن خطای bind علت را پیدا کن.
127.0.0.1 فقط loopback IPv4 همان دستگاه است.
استفاده از 0.0.0.0 دامنهٔ دسترسی متفاوتی دارد و برای این تمرین لازم نیست.
اگر endpointهای Kestrel را از جای دیگری صریح پیکربندی کنی، رفتار URLها می‌تواند متفاوت شود.

## ۱۵. مشاهدهٔ response header

```powershell
curl.exe -i http://127.0.0.1:5240/health
curl.exe -i http://127.0.0.1:5240/info
```

در response، X-Course: supportdesk باید دیده شود.
دو بار /info را بخوان و instanceId را مقایسه کن.
HTTP header nameها case-insensitive هستند؛ تست را به بزرگی حروف نام header وابسته نکن.

## ۱۶. shutdown

Ctrl+C در ترمینال host درخواست shutdown می‌دهد.
host مدیریت بستن listener و disposal سرویس‌های متعلق به خود را انجام می‌دهد.
کار پس‌زمینه یا resource سفارشی باید قرارداد توقف خودش را رعایت کند.
Run منتظر درخواست بعدی است و مصرف CPU بالا نباید لازمهٔ «باز بودن برنامه» باشد.
از حلقهٔ بی‌پایان دستی به جای host استفاده نمی‌کنیم.

## تمرین مستقل

یک مقدار Course:TeamName به configuration اضافه کن.
endpoint /team آن را از IConfiguration بخواند.
فایل، environment و command line را جدا آزمایش کن و precedence مشاهده‌شده را بنویس.
مقادیر تست و متغیرهای session را پس از آزمایش مدیریت کن.

## تمرین تکمیلی

middleware دوم با یک header متفاوت اضافه کن.
هر دو header باید در یک endpoint عادی دیده شوند.
سپس یک middleware کوتاه‌کننده فقط برای یک مسیر آزمایشی بساز و اثر صدا نزدن next را مشاهده کن.
این مسیر نباید همهٔ درخواست‌ها را تصادفی مسدود کند.

## معیار عبور

- [ ] startup را از handler هر request جدا می‌دانم.
- [ ] Services پیش از Build ثبت می‌شوند.
- [ ] next و ترتیب pipeline را توضیح می‌دهم.
- [ ] header را پیش از شروع پاسخ تنظیم می‌کنم.
- [ ] configuration را با environment name یکی نمی‌گیرم.
- [ ] precedence فایل، environment و خط فرمان را مشاهده کردم.
- [ ] دامنهٔ loopback و خطای پورت را می‌شناسم.

## منابع رسمی

- [WebApplication and builder](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0)
- [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)
- [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)

## قدم بعد

ورودی route، query، header و JSON body را از هم جدا دریافت و بررسی می‌کنیم.
