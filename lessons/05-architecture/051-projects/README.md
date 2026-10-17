# درس 051 — جداسازی assembly و جهت وابستگی

## خروجی قابل مشاهده

همان use case بستن تیکت را با چهار پروژه می‌سازی.
Domain بدون اجرای host و بدون وابستگی به adapter قابل build است.
دسترسی مستقیم host به کلاس internal ذخیره‌سازی در یک آزمایش منفی توسط compiler رد می‌شود.

پیش‌نیاز: [درس 050](../050-folders/README.md)، interface و reference و internal.
پوشهٔ این درس یک برنامهٔ deployشدنی دارد؛ چند پروژه به معنی چند سرویس یا microservice نیست.

## واژگان دقیق

| اصطلاح | معنی در این نمونه |
| --- | --- |
| folder | مسیر سازمان‌دهی فایل‌ها |
| namespace | فضای نام typeها، مستقل از محل فایل |
| project / csproj | واحد تنظیم build و referenceها |
| assembly | خروجی کامپایل، معمولاً یک DLL |
| solution | گروه‌بندی پروژه‌ها برای ابزار و IDE |

برای اجرای نمونه داشتن sln الزامی نیست؛ build پروژهٔ host referenceها را هم build می‌کند.
برای کار در IDE می‌توانی یک solution شخصی بسازی و چهار csproj را اضافه کنی.
این فایل فقط گروه‌بندی است؛ مرز dependency در ProjectReferenceها قرار دارد.

## ساختار واقعی

مسیرها نسبت به samples/50-projects هستند:

| پروژه | فایل‌های اصلی | وابستگی مستقیم مجاز |
| --- | --- | --- |
| src/Domain/SupportDesk.Domain.csproj | Ticket، TicketTitle | هیچ پروژه‌ای |
| src/Application/SupportDesk.Application.csproj | CloseTicket، قرارداد store، نتیجه | Domain |
| src/Infrastructure/SupportDesk.Infrastructure.csproj | adapter و factory | Application و Domain |
| SupportDesk.csproj | Program و Verification | سه پروژهٔ بالا |

جهت فلش در این جدول یعنی dependency زمان کامپایل.
در اجرا، CloseTicket متد SaveAsync پیاده‌سازی حافظه‌ای را صدا می‌زند؛ این جهتِ جریان اجرا، مالکیت interface را عوض نمی‌کند.
Application، نوع InMemoryTicketStore را نمی‌شناسد؛ فقط ITicketStore را می‌شناسد.
Program در composition root می‌تواند factory زیرساخت را فراخوانی کند.
این استثنا دلیل مناسبی برای وابستگی Domain به Infrastructure نیست.

## از پروژهٔ کوچک شروع کن

ابتدا فقط Domain را جدا کن.
فایل‌های Ticket و TicketTitle را منتقل و namespace را حفظ کن.
بعد Application و در پایان Infrastructure را جدا کن.
این ترتیب باعث می‌شود خطای reference به تغییر مشخصی مربوط باشد.

نمونهٔ Application project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Domain/SupportDesk.Domain.csproj" />
  </ItemGroup>
</Project>
```

`ProjectReference` هم ترتیب build و هم مرجع لازم برای typeها را فراهم می‌کند.
مسیر Include نسبت به همان csproj است، نه پوشه‌ای که terminal در آن باز است.
چرخهٔ Domain → Application → Domain مجاز نیست و build را خراب می‌کند.
برای شکستن چرخه قرارداد را در سمت مالک نیاز قرار بده؛ reference دوطرفه راه‌حل نیست.

## تلهٔ compile دوبارهٔ سورس

در این نمونه host در ریشه و پروژه‌های کتابخانه زیر src هستند.
SDK عادی فایل‌های cs زیرپوشه‌ها را هم جمع می‌کند.
پس host نباید آن‌ها را دوباره داخل assembly خودش کامپایل کند:

```xml
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
```

فایل‌های host صریحاً افزوده می‌شوند:

```xml
<Compile Include="Program.cs" />
<Compile Include="Verification.cs" />
```

این تنظیم در پروژهٔ host است؛ کتابخانه‌ها default compile خودشان را دارند.
در چیدمانی که همهٔ projectها sibling باشند معمولاً به این استثنا نیاز نداری.
هدف را بفهم؛ این دو خط را بدون بررسی ساختار به هر پروژه‌ای اضافه نکن.

## internal چه چیزی را محدود می‌کند؟

InMemoryTicketStore اکنون internal است و در assembly مستقل Infrastructure زندگی می‌کند.
host نمی‌تواند آن را مستقیم new کند.
StoreFactory عمومی، فقط قرارداد ITicketStore را برمی‌گرداند.
بنابراین جزئیات dictionary از سطح API عمومی adapter بیرون نمی‌آید.
internal مرز امنیت داده در برابر مهاجم نیست؛ محدودیت دسترسی کد در زمان کامپایل است.
استفاده از reflection یا InternalsVisibleTo تصمیم‌های دیگری‌اند و در این مثال لازم نیستند.

## اجرا

```powershell
dotnet build samples/50-projects/src/Domain/SupportDesk.Domain.csproj
dotnet build samples/50-projects/SupportDesk.csproj
dotnet run --project samples/50-projects/SupportDesk.csproj -- --check
dotnet run --project samples/50-projects/SupportDesk.csproj
python scripts/architecture_smoke.py
```

سه کتابخانه و host ساخته می‌شوند؛ فقط host قابل run است.
خروجی عادی `Success: 1 Closed notes=1` و پایان بررسی `PASS: architecture behavior` است.
اسکریپت علاوه بر رفتار، referenceها و مخفی بودن adapter را هم بررسی می‌کند.
این اسکریپت برای نمونهٔ آموزشی .NET SDK 10 می‌خواهد و دیتابیس لازم ندارد.

## آزمایش منفیِ وابستگی

اسکریپت یک کپی موقت از پروژه می‌سازد.
در Domain کدی با اشاره به CloseTicket اضافه می‌کند؛ چون reference ندارد باید compile شکست بخورد.
همچنین در host تلاش می‌کند InMemoryTicketStore داخلی را نام ببرد و خطای CS0122 را بررسی می‌کند.
نسخهٔ سورس دوره برای این آزمایش تغییر نمی‌کند.
موفقیت تست منفی یعنی build همان تخلفِ عمدی شکست خورده، نه اینکه build سالم شکست داشته باشد.

ProjectReference در SDKهای جدید به‌طور معمول transitive است.
پس چند پروژه لزوماً همهٔ دسترسی‌های ناخواسته را مسدود نمی‌کند.
مثلاً host از طریق Application هم ممکن است انواع عمومی Domain را ببیند.
اسکریپت graph دقیقِ referenceهای مستقیم نمونه را بررسی می‌کند؛ این بررسی جای تحلیل تمام وابستگی‌های type-level را نمی‌گیرد.
در درس ۵۹ تست معماری و ADR را کامل‌تر بررسی خواهیم کرد.

## تمرین مستقل

قابلیت RenameTicket تمرین قبلی را به همین مرزها منتقل کن.
Use case فقط به Domain و قرارداد موردنیازش وابسته بماند.
هیچ نام System.Console، ASP.NET، EF یا InMemoryTicketStore در Domain و Application اضافه نکن.
اگر به dependency تازه‌ای نیاز داری، قبل از reference دادن دلیل آن را بنویس.

### سرنخ‌ها

1. خطای namespace را با افزودن همهٔ referenceها پنهان نکن.
2. اول بفهم type متعلق به کدام مسئولیت است.
3. اگر قرارداد store در Infrastructure است، مالکیت آن را بازنگری کن.
4. هنگام استفاده از DLLها تمام خروجی publish را منتقل کن؛ فقط SupportDesk.dll کافی نیست.

## بررسی خطاها و معیار عبور

- Domain به‌تنهایی build شود و package یا project reference خارجی نداشته باشد.
- خطای internal عمدی با CS0122 بازتولید شود.
- تمام سناریوهای رفتار درس ۴۹ همچنان موفق باشند.
- بتوانی فرق جهت اجرای متد و جهت reference را توضیح بدهی.
- بتوانی برنامهٔ چندپروژه‌ای را به یک پوشه publish و از آن اجرا کنی.
- توضیح بدهی چرا این نمونه microservice یا تضمین امنیت نیست.

## منابع رسمی

تطبیق در 2026-10-17؛ مبنا net10.0 و بدون package خارجی است:

- [تنظیمات SDK و compile items](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)
- [مرز internal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal)
- [جهت وابستگی در معماری برنامه](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

[قبلی](../050-folders/README.md) · [بعدی: Application Service](../052-application-services/README.md)
