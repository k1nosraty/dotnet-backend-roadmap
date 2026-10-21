# کتابخانه‌ها و تکنیک‌های .NET

این فهرست برنامهٔ بررسی ابزارهاست، نه دستور نصب همهٔ آن‌ها.
نسخهٔ دقیق هر dependency در زمان نوشتن درس و با مستندات رسمی انتخاب می‌شود.
هیچ سازگاری نسخه‌ای برای کتابخانه‌های آینده در این نسخه ادعا نشده است.

## ابتدا امکانات استاندارد

| امکان | مسئله‌ای که بررسی می‌کنیم | درس |
| --- | --- | --- |
| List / Dictionary | دادهٔ ترتیبی و lookup | ۰۰۶ |
| IDisposable / using | آزاد کردن resource | ۰۰۹ |
| LINQ | query روی collection و هزینهٔ enumeration | ۰۱۰ |
| System.Text.Json | ذخیره و قرارداد JSON | ۰۱۱، ۰۲۷ |
| Task / CancellationToken | انتظار asynchronous و لغو | ۰۱۲ |
| DI و options | اتصال اجزا و تنظیمات قابل بررسی | ۰۱۶، ۰۳۰، ۰۳۱ |
| DataAnnotations و ProblemDetails | ورودی معتبر و قرارداد failure | ۰۲۸ |
| ILogger | لاگ ساختاریافته و زمینهٔ خطا | ۰۲۹، ۰۷۷ |
| IHttpClientFactory | مدیریت client و handler برای HTTP | ۰۷۵ |
| BackgroundService | کار پس‌زمینه و توقف کنترل‌شده | ۰۷۶ |
| RateLimiter و health checks | محدودسازی و وضعیت سرویس | ۰۶۸، ۰۸۲ |

## ابزارهای بیرونی برای بررسی در پروژه

| ابزار | هدف آموزشی | درس | گزینهٔ مقایسه |
| --- | --- | --- | --- |
| Microsoft.AspNetCore.OpenApi | تولید سند قرارداد API | ۰۳۳ | سند دستی |
| Npgsql driver 10.0.3 | SQL پارامتری و transaction مستقیم PostgreSQL | ۰۳۶، ۰۳۹ | ORM در مرحلهٔ بعد |
| EF Core + Npgsql provider | persistence رابطه‌ای با PostgreSQL | ۰۴۱ تا ۰۴۶ | SQL مستقیم |
| Dapper | query با کنترل مستقیم SQL | ۰۴۷ | projection در EF Core |
| FluentValidation | قواعد اعتبارسنجی خوانا و قابل تست | ۰۵۸ | validation دستی |
| xUnit | تست خودکار | ۰۷۱ | تست دستی برای فهم اولیه |
| Testcontainers | دیتابیس موقت برای integration test | ۰۷۴ | دیتابیس تست مدیریت‌شده |
| Serilog | sink و enrichment لاگ | ۰۷۷ | providerهای ILogger |
| OpenTelemetry | trace و metric و ارتباط درخواست‌ها | ۰۸۲ | لاگ به تنهایی |
| Redis client | distributed cache | ۰۸۳ | cache در حافظه |
| Hangfire یا Quartz.NET | بررسی نیاز به job پایدار یا زمان‌بندی | ۰۸۴ | BackgroundService |

mapping دستی ابتدا آموزش داده می‌شود؛ ابزار mapping فقط در صورت نیاز پروژه بررسی خواهد شد.
CQRS یک تفکیک طراحی است؛ به خودی خود الزام به MediatR ایجاد نمی‌کند.
retry تنها برای عملیات مناسب و با توجه به idempotency اعمال می‌شود.

## کارت تصمیم هر dependency

برای هر ابزار در درس مربوط این موارد ثبت می‌شوند:

1. مسئلهٔ واقعی و نسخهٔ پیاده‌سازی بدون ابزار.
2. لینک رسمی، نسخه، license و سازگاری با target framework.
3. نصب و پیکربندی حداقلی.
4. یک use case و یک failure case.
5. هزینهٔ نگهداری، رفتار امنیتی و محدودیت.
6. راه حذف یا جایگزینی ابزار.

در درس‌های ابتدایی هیچ NuGet package بیرونی لازم نیست.
اولین package معرفی‌شده در [درس ۰۱۶](../lessons/02-design/016-dependency-injection/README.md)، Microsoft.Extensions.DependencyInjection با نسخهٔ ثابت 10.0.12 است.
نصب، lifetime، ownership، محدودیت validation و روش حذف container با حفظ constructorها در همان درس آمده است.

## ابزارهای اجراشده در بخش وب

[درس ۰۲۸](../lessons/03-web/028-validation/README.md) از DataAnnotations و ProblemDetails استاندارد استفاده می‌کند.
[درس ۰۳۰](../lessons/03-web/030-configuration/README.md) Options و ValidateOnStart را با configuration واقعی اجرا می‌کند.
[درس ۰۳۳](../lessons/03-web/033-openapi/README.md) Microsoft.AspNetCore.OpenApi نسخهٔ ثابت 10.0.13 را نصب و سند تولیدشده را با HTTP مقایسه می‌کند.
نسخهٔ OpenAPI در ۲۰۲۶/۱۰/۱۵ از [NuGet رسمی](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.13) بررسی شده است؛ سازگاری و audit وابستگی‌های transitive در CI بررسی می‌شود.
UI مستندات یا ابزار mapping خارجی در این مرحله نصب نشده‌اند؛ نیاز و هزینهٔ آن‌ها پیش از افزودن ارزیابی می‌شود.


## ابزارهای اجراشده در ابتدای data

Npgsql 10.0.3 در [درس ۳۶](../lessons/04-data/036-sql-basics/README.md) با CRUD، parameter، reader، token و ownership معرفی شده است.
[درس ۳۹](../lessons/04-data/039-transactions/README.md) از connection و transaction همان driver برای isolation واقعی استفاده می‌کند.
Npgsql با Npgsql.EntityFrameworkCore.PostgreSQL یک package نیست؛ provider EF Core در درس ۴۱ اضافه می‌شود.
نسخهٔ مستقیم driver و منابع رسمی در زمان تهیهٔ این درس‌ها بررسی شده‌اند؛ ارتقا نیاز به بررسی سازگاری و اجرای مجدد integration دارد.

## ابزارهای دیتابیس در درس‌های ۴۱ تا ۴۸

- EF Core 10.0.13: mapping، tracker و واحد کار؛ [درس ۴۱](../lessons/04-data/041-ef-core/README.md).
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3: provider PostgreSQL، جدا از driver Npgsql.
- Microsoft.EntityFrameworkCore.Design و ابزار محلی dotnet-ef 10.0.13: scaffold و migration؛ [درس ۴۲](../lessons/04-data/042-migrations/README.md).
- Dapper 2.1.66: query هدفمند با SQL و mapping به DTO؛ [درس ۴۷](../lessons/04-data/047-dapper/README.md). این نسخهٔ مرجع pin شده است، نه ادعای آخرین نسخه.

نسخه‌های Microsoft و provider شمارهٔ patch مستقل دارند. انتخاب ابزار با مسئله و SQL واقعی انجام می‌شود؛ افزودن همهٔ کتابخانه‌ها هدف دوره نیست.

## validation و معماری

[درس ۵۸](../lessons/05-architecture/058-validation-library/README.md) FluentValidation 12.2.0 را در Application و بدون package auto-validation معرفی می‌کند. در 2026-10-17 [NuGet رسمی](https://www.nuget.org/packages/FluentValidation/12.2.0)، target .NET 8 یا بالاتر، سازگاری محاسبه‌شده با net10.0 و license Apache-2.0 بررسی شدند. نسخه pin شده است؛ build و audit زمان اجرا در CI انجام می‌شوند. ruleهای نمونه pure هستند و با ValidateAsync صریح اجرا می‌شوند. Domain وابسته به validator نیست؛ روش حذف و جایگزینی با guard دستی در متن درس آمده است.

برای تفکیک command/query mediator اجباری نداریم. تست referenceها با XML پروژه و reflection assembly انجام می‌شود؛ ابزار تست معماری خارجی در این مرحله اضافه نشده است.

## Identity و authentication

[درس‌های ۶۲ تا ۶۴](../lessons/06-security/README.md) Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.13 و EF Core 10.0.13 را با provider PostgreSQL 10.0.3 استفاده می‌کنند. hash، token provider، cookie و bearer handler از framework هستند؛ کتابخانهٔ JWT یا رمزنگاری دست‌ساز اضافه نشده است. schemaهای نمونه آزمایشی و تازه‌اند؛ روش مهاجرت محصول همان بخش data است. Data Protection نمونهٔ کنسولی ephemeral و نمونهٔ HTTP دارای key ring خصوصی و پایدار است.

## authorization و کنترل درخواست

[درس‌های ۶۵ تا ۶۸](../lessons/06-security/README.md) از IAuthorizationService، requirement/handler و AddRateLimiter خود ASP.NET Core استفاده می‌کنند؛ package خارجی برای role، CORS یا rate limit اضافه نشده است. AddRoles، UserManager و RoleManager روی همان store PostgreSQL ثبت می‌شوند. query جست‌وجو و ExecuteUpdate از EF Core موجود استفاده می‌کنند. شمارندهٔ limiter محلی و مستقل برای هر process است؛ قابلیت توزیع‌شده ادعا نمی‌شود.

## audit و بازبینی امنیت

[درس ۶۹](../lessons/06-security/069-secrets/README.md) از JSON console و ILogger استاندارد و transaction همان EF Core استفاده می‌کند؛ package جدیدی اضافه نشده است. جدول audit با write محلی اتمیک است؛ console، سرویس خارجی و ذخیرهٔ غیرقابل تغییر ادعا نمی‌شوند. [درس ۷۰](../lessons/06-security/070-security-review/README.md) metadata واقعی RouteEndpoint و policy و rate limit را بدون بستهٔ تست جدید بررسی می‌کند. xUnit و integration tooling موضوع بخش بعد هستند.

## ابزار تست در درس‌های ۷۱ تا ۷۴

[بخش هفتم](../lessons/07-testing/README.md) xUnit v2 با نسخهٔ مرجع 2.9.3، adapter xunit.runner.visualstudio 3.1.5 و Microsoft.NET.Test.Sdk 18.0.1 دارد. نسخهٔ adapter با framework یکی نیست و شماره‌ها ادعای جدیدترین نسخه نیستند. packageها فقط در test projectها ثبت شده‌اند.
Microsoft.AspNetCore.Mvc.Testing 10.0.13 برای WAF و TestServer و Testcontainers.PostgreSql 4.15.0 برای lifecycle PostgreSQL 18.6 معرفی می‌شوند. Npgsql 10.0.3 در adapter واقعی sample قرار دارد. source packageهای رسمی و APIها در 2026-10-18 بررسی شدند و build و اجرای CI سازگاری عملی را می‌سنجند. mocking library یا coverage collector صرفاً برای افزایش تعداد package اضافه نشده است.

## HTTP و logging در درس‌های ۷۵ تا ۷۸

Microsoft.Extensions.Http.Resilience نسخهٔ مرجع 10.11.0 برای pipeline استاندارد HTTP در درس ۷۵ ثبت شده است؛ نسخهٔ Extensions مستقل از patch runtime است. Serilog.Extensions.Hosting 10.0.0 در درس ۷۷، ILogger را به Serilog متصل می‌کند. JsonFormatter و sink کوچک روی TextWriter برای مشاهدهٔ stdout به‌کار می‌روند؛ package مقصد شبکه‌ای یا فایل اضافه نشده است.
نسخه‌ها و API از Microsoft Learn، NuGet و مخزن رسمی Serilog در 2026-10-18 بررسی شده‌اند. این‌ها نسخهٔ pinشدهٔ نمونه هستند؛ ادعای آخرین نسخه یا آمادگی محصول نیستند. درس‌های ۷۶ و ۷۸ از Channel و BackgroundService و ActivitySource موجود در framework استفاده می‌کنند. همهٔ نمونه‌های جدید FrameworkReference به Microsoft.AspNetCore.App دارند و SDK/runtime 10 لازم است.

## ابزار محیط اجرا در درس‌های ۷۹ تا ۸۲

نمونه‌های Docker از SDK 10.0.402 و ASP.NET runtime 10.0.13 و نمونهٔ reverse proxy از Caddy official image نسخهٔ مرجع 2.11.7-alpine استفاده می‌کنند. tag نسخه با digest یکسان نیست؛ برای محیط واقعی باید ارتقا و pin کردن digest با سیاست patch بررسی شود.
OpenTelemetry.Extensions.Hosting و OpenTelemetry.Exporter.Console هر دو 1.19.0 pin شده‌اند؛ console فقط برای مشاهدهٔ آموزشی است و collector یا backend خارجی ندارد. اطلاعات نسخه‌ها و API از منابع رسمی Microsoft، Caddy، NuGet و OpenTelemetry در 2026-10-21 بررسی شده است؛ ادعای آخرین نسخه نیست. HealthCheck و ForwardedHeaders از framework هستند و mocking یا auto-instrumentation HTTP جدید اضافه نشده است.
