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
| ILogger | لاگ ساختاریافته و زمینهٔ خطا | ۰۷۷ |
| IHttpClientFactory | مدیریت client و handler برای HTTP | ۰۷۵ |
| BackgroundService | کار پس‌زمینه و توقف کنترل‌شده | ۰۷۶ |
| RateLimiter و health checks | محدودسازی و وضعیت سرویس | ۰۶۸، ۰۸۲ |

## ابزارهای بیرونی برای بررسی در پروژه

| ابزار | هدف آموزشی | درس | گزینهٔ مقایسه |
| --- | --- | --- | --- |
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
