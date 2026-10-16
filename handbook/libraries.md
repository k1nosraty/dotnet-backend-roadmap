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
