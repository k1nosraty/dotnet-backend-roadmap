# درس ۰۱۶ — تزریق وابستگی و طول عمر سرویس‌ها

[قبلی](../015-interfaces/README.md) · [خانه](../../../README.md)

## خروجی درس

service و reader را در container استاندارد .NET ثبت می‌کنی.
Transient، Scoped و Singleton را با reference equality بررسی می‌کنی.
scope را می‌بندی و آزاد شدن resource را مشاهده می‌کنی.
وابستگی scoped در singleton را با validation رد می‌کنی.

## پیش‌نیاز

interface، constructor injection دستی، IDisposable و composition root.
هنوز ASP.NET Core request واقعی نداریم؛ scopeها را در کنسول خودمان ایجاد می‌کنیم.

## ۱. DI پیش از container

در درس قبل constructor به ITicketReader وابسته بود و Program آن را می‌ساخت.
همان هم Dependency Injection است.
container ساخت گراف dependency و lifetime را هماهنگ می‌کند؛ تعریف DI را به یک package محدود نکن.
برای برنامهٔ کوچک ساخت دستی گاهی کافی است.

## ۲. کتابخانهٔ این درس

Microsoft.Extensions.DependencyInjection implementation رسمی container استاندارد .NET است.
نمونه از نسخهٔ ثابت 10.0.12 استفاده می‌کند که در NuGet رسمی بررسی شده است.
این pin برای بازتولید نمونه است؛ ادعای آخرین نسخه نیست.
پکیج MIT است؛ صفحهٔ رسمی package و metadata آن مرجع بررسی مجوز و dependencyها هستند.
Abstractions وابستگی پکیج است و restore آن را طبق metadata دریافت می‌کند.

اگر پروژهٔ تازهٔ تمرین داری، از ریشهٔ دوره:

```powershell
dotnet add exercises/MyDiApp/MyDiApp.csproj package Microsoft.Extensions.DependencyInjection --version 10.0.12
```

MyDiApp باید از قبل با dotnet new console ساخته شده باشد.
در نمونهٔ آماده PackageReference وجود دارد؛ دوباره نصب لازم نیست.
restore اولیه دسترسی NuGet می‌خواهد و اجرای بعد از cache استفاده می‌کند.

## ۳. ثبت dependencyها

```csharp
ServiceCollection services = new();
services.AddSingleton<ITicketReader>(_ => new InMemoryTicketReader(seed));
services.AddTransient<TicketSummaryService>();
```

نوع service ITicketReader و implementation واقعی InMemoryTicketReader است.
factory در این مثال دادهٔ اولیه را فراهم می‌کند.
_ یعنی پارامتر provider این factory لازم نشده است.
registration به معنی اجرا شدن فوری factory نیست؛ singleton معمولاً هنگام نیاز ساخته می‌شود.

این reader فقط برای demo خواندن است.
بازگرداندن Ticketهای mutable برای یک singleton مشترک در سرویس واقعی نیاز به بررسی concurrency دارد.
container هیچ کلاسی را خودکار thread-safe نمی‌کند.

## ۴. ساخت container

```csharp
using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true
});
```

این کار در composition root نمونهٔ کنسولی انجام می‌شود.
در ASP.NET Core معمولاً framework provider را می‌سازد؛ ساخت provider دوم در registrationهای برنامه مناسب نیست.
using عمر root provider را محدود می‌کند.
گراف dependency این نمونه constructorهای روشن و بدون انتخاب مبهم دارد.

## ۵. resolve در مرز ساخت

```csharp
TicketSummaryService service = provider.GetRequiredService<TicketSummaryService>();
Console.WriteLine(service.GetSummary(1));
```

container reader مورد نیاز constructor را فراهم می‌کند.
اگر registration لازم نباشد یا گراف نامعتبر باشد، resolve شکست می‌خورد.
GetRequiredService قرار نیست null برای dependency ضروری بدهد.
داخل TicketSummaryService به IServiceProvider وابسته نمی‌شویم تا خودش همه‌چیز را جست‌وجو کند.
این کار dependencyها را پنهان می‌کند و الگوی service locator ایجاد می‌کند.

## ۶. طول عمرها

| lifetime | نمونهٔ تازه کی ساخته می‌شود؟ | کاربرد آموزشی |
| --- | --- | --- |
| Transient | هر resolve از registration | سرویس سبک بدون state مشترک |
| Scoped | یک بار برای هر scope | state مربوط به یک عملیات |
| Singleton | یک بار برای یک root provider | dependency مشترک با عمر برنامه |

singleton به معنی یکی در تمام سرورها یا تمام processها نیست.
ساخت دو root provider می‌تواند دو singleton مستقل ایجاد کند.
Scoped هم به معنی یک instance برای هر کاربر نیست.
در وب معمولاً scope درخواست داریم، اما قرارداد عمومی container «scope» است.

## ۷. مشاهدهٔ Transient

```csharp
services.AddTransient<TransientProbe>();
```

اگر در یک scope دو بار TransientProbe را resolve کنیم، referenceها متفاوت‌اند.
هرکدام ممکن است dependencyهای scoped مشترک در همان scope داشته باشند.
Transient بودن caller به معنی تازه بودن همهٔ dependencyهایش نیست.
در این نمونه probe فقط برای دیدن هویت instance است و business service واقعی نیست.

## ۸. مشاهدهٔ Scoped

```csharp
using IServiceScope scope = provider.CreateScope();
ScopedProbe first = scope.ServiceProvider.GetRequiredService<ScopedProbe>();
ScopedProbe second = scope.ServiceProvider.GetRequiredService<ScopedProbe>();
```

first و second در همین scope یک instance هستند.
در scope تازه instance تازه ساخته می‌شود.
scope را مالک عملیات ایجاد و dispose می‌کند.
resolution scoped از root با ValidateScopes در این نمونه رد می‌شود.

## ۹. Dispose در پایان scope

ScopedProbe یک property به نام IsDisposed دارد.
Dispose آن را true می‌کند تا در نمونه قابل مشاهده باشد.
داخل scope مقدار false است و بعد از بسته شدن scope true می‌شود.
service واقعی می‌تواند connection یا resource دیگری داشته باشد.
نباید service ساخته‌شده توسط container را وسط scope خودسرانه dispose کنی و انتظار داشته باشی callerهای دیگر سالم بمانند.

## ۱۰. مالکیت registration

وقتی container instance را با constructor یا factory خودش می‌سازد، معمولاً disposal مطابق lifetime را مدیریت می‌کند.
اگر instance از قبل ساخته‌شده را با AddSingleton(instance) بدهی، مالکیت disposal آن با ثبت نوع یا factory یکسان فرض نمی‌شود؛ مستندات را بررسی و مالک را روشن کن.
این نمونه از factory برای reader و ثبت نوع برای probeها استفاده می‌کند.
serviceها برای dependency قرض‌گرفته IDisposable پیاده نمی‌کنند مگر مالکیت و نیاز روشن باشد.

## ۱۱. captive dependency

```csharp
public sealed class BadSingleton
{
    public ScopedProbe Scoped { get; }
    public BadSingleton(ScopedProbe scoped) => Scoped = scoped;
}
```

singleton عمر بلند دارد، اما scoped dependency باید متعلق به scope عملیات باشد.
نگه داشتن مستقیم آن در singleton می‌تواند آن را از عمر مورد انتظار جدا کند.
نمونه عمداً AddSingleton<BadSingleton> و AddScoped<ScopedProbe> را در container جدا ثبت می‌کند.
با ValidateScopes و ValidateOnBuild ساخت آن رد می‌شود.

## ۱۲. معنی validationها

ValidateScopes استفادهٔ scoped از root و وابستگی scoped در singleton را بررسی می‌کند.
ValidateOnBuild بررسی گراف‌های قابل بررسی را هنگام ساخت فعال می‌کند.
این‌ها صحت همهٔ factoryها، business ruleها و پیکربندی خارجی را ثابت نمی‌کنند.
بدنهٔ factory دلخواه ممکن است فقط هنگام resolve اجرا شود.
open genericها و بعضی مسیرهای runtime نیز نیاز به بررسی جدا دارند.
پس validation روشن را با اثبات کل برنامه یکی نگیر.

## ۱۳. singleton و worker

اگر سرویس بلندعمر برای هر job به dependency scoped نیاز دارد، می‌تواند scope مستقل عملیات بسازد و در همان scope استفاده و dispose کند.
این کار با گرفتن scoped در constructor singleton فرق دارد.
نگه داشتن reference پس از خروج scope همچنان اشتباه است.
در فصل worker و background service این الگو را با job واقعی بررسی می‌کنیم.
اینجا scopeها صرفاً برای آموزش lifetime هستند.

## ۱۴. انتخاب lifetime با مسئولیت

service بدون state ممکن است transient باشد.
DbContext در الگوی متداول ASP.NET Core scoped است؛ thread-safe فرض نمی‌شود.
singleton باید دربارهٔ state مشترک و هم‌زمانی تصمیم درست داشته باشد.
ثبت تمام dependencyها به صورت singleton برای کاهش ساخت object راه‌حل عمومی نیست.
ثبت تمام dependencyها transient هم ownership و disposal را خودکار ساده نمی‌کند.

## ۱۵. چیدمان فایل‌ها

| مسیر | مسئولیت |
| --- | --- |
| Application/ITicketReader.cs | قرارداد caller |
| Application/TicketSummaryService.cs | use case |
| Infrastructure/InMemoryTicketReader.cs | implementation demo |
| Lifetimes/Probes.cs | ابزار مشاهدهٔ lifetime |
| Program.cs | registrations، scopes و نمایش |
| SupportDesk.csproj | dependency نسخه‌دار NuGet |

فایل Lifetimes برای آموزش است و الزام فولدر مشابه در محصول نیست.
مدل Ticket و service به container package وابسته نشده‌اند.
وابستگی به package در composition root استفاده شده است.

## ۱۶. اجرا

[Program](../../../samples/15-ticket-di/Program.cs)

```powershell
dotnet run --project samples/15-ticket-di/SupportDesk.csproj
```

خروجی مورد انتظار:

```text
#1: Network issue (Open)
Transient same: False
Scoped same inside: True
Disposed inside: False
Disposed after scope: True
Scoped same across: False
Singleton same across: True
Root scoped resolution rejected.
Captive scoped dependency rejected at build.
```

ReferenceEquals هویت instance را بررسی می‌کند، نه برابر بودن مقدار propertyها.
نمونهٔ invalid عمداً در container جدا است تا برنامهٔ اصلی سالم ساخته شود.

## ۱۷. هزینه و انتخاب کتابخانه

container ساخت dependencyها و lifetime را هماهنگ می‌کند، اما registrationها باید نگهداری شوند.
خطاهای ثبت ممکن است runtime باشند و validation مهم است.
برای خارج کردن container، می‌توانیم constructorها را حفظ و اجزا را مانند درس ۰۱۵ دستی بسازیم.
این قابلیت نشانهٔ مرز سالم است؛ service نباید برای اجرای business logic به provider دسترسی لازم داشته باشد.

## تمرین مستقل

TicketSummaryService را scoped ثبت کن و در دو scope resolve کن.
در هر scope دو resolve انجام بده؛ هویت درون scope یکسان و میان scopeها متفاوت باشد.
reader مناسب demo را بدون تغییر service جایگزین کن.
نام registrationها و دلیل lifetime هرکدام را در README بنویس.

## تمرین تکمیلی

به یک service آموزشی دو dependency با lifetime متفاوت بده.
با reference equality نشان بده transient caller ممکن است scoped dependency مشترک داشته باشد.
یک registration ضروری را حذف کن و تفاوت validation یا resolve failure را توضیح بده.
ساخت provider دوم را برای پنهان کردن مشکل registration استفاده نکن.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| دو resolve transient | instanceهای متفاوت |
| دو resolve scoped در یک scope | یک instance |
| scoped در دو scope | دو instance |
| singleton در دو scope یک provider | یک instance |
| پایان scope | IDisposable scoped آزاد شود |
| singleton با scoped مستقیم | validation شکست بخورد |
| scoped از root | در نمونه رد شود |

## سرنخ

قبل از انتخاب lifetime، state، ownership و مرز عملیات را بنویس.
اگر service مجبور است provider بگیرد، بررسی کن dependencyها را می‌توان در constructor صریح کرد یا نه.
error validation را با خاموش کردن ValidateScopes حل نکن.

## معیار عبور

- [ ] DI را با وجود container یکی نمی‌دانم.
- [ ] scope و کاربر را یکی نمی‌گیرم.
- [ ] عمر و disposal instanceها را مشاهده کرده‌ام.
- [ ] factory و instance registration را از نظر ownership بررسی می‌کنم.
- [ ] captive dependency را می‌شناسم.
- [ ] business service از IServiceProvider استفاده نمی‌کند.
- [ ] محدودیت validation را می‌دانم.

## منابع رسمی و نسخه

- [Dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview)
- [Service lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes)
- [DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)
- [Package 10.0.12](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.12)

نسخه و منابع برای این نمونه در ۲۰۲۶/۱۰/۱۵ بررسی شده‌اند.

## قدم بعد

درس ۰۱۷ به SOLID و بازآرایی یک قابلیت اختصاص دارد و هنوز نوشته نشده است.
پیش از آن، lifetimeها و ownership تمرین خودت را توضیح بده.
