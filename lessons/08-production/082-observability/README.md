# درس ۰۸۲ — برنامه زنده است، اما آمادهٔ کار هم هست؟

ممکن است process بالا باشد ولی هنوز آمادهٔ پذیرفتن job نباشد.
در این درس اول این دو وضعیت را از هم جدا می‌کنیم؛ بعد یک عملیات را با trace و counter می‌بینیم.

## ۱. اجرای کوچک و قابل مشاهده

از ریشهٔ مخزن با SDK 10:

```powershell
dotnet run --project samples/81-observability/SupportDesk.csproj --urls http://127.0.0.1:5082
```

در ترمینال دوم:

```powershell
curl.exe http://127.0.0.1:5082/health/live
curl.exe http://127.0.0.1:5082/health/ready
curl.exe -X POST http://127.0.0.1:5082/work/accepted
curl.exe -X POST http://127.0.0.1:5082/work/rejected
```

دو health در حالت عادی 200 با متن Healthy می‌دهند. accepted پاسخ 200 با outcome=accepted و rejected پاسخ 409 با outcome=rejected دارد.
در ترمینال برنامه، console exporter اطلاعات span به نام worker.process را نشان می‌دهد؛ شناسه‌ها در هر اجرا فرق می‌کنند.
metric به‌صورت دوره‌ای و هنگام flush در پایان Host دیده می‌شود؛ مثل event بلافاصله بعد از هر درخواست چاپ نمی‌شود.
با Ctrl+C برنامه را تمیز متوقف کن و خروجی counter با نام worker.jobs را ببین.

## ۲. فایل‌ها را دنبال کن

| فایل/کلاس | مسئولیت |
| --- | --- |
| `Program.cs` | routeها، health endpointها و ثبت telemetry |
| `LabTelemetry` در `LabTelemetry.cs` | ساخت Activity و افزودن counter |
| `StartupState` | وضعیت آماده بودنِ محلی با خواندن/نوشتن thread-safe |
| `StartupLifecycle` | اعلام آمادگی بعد از initialization محلی و لغو آن در توقف |
| `StartupCheck` | تبدیل وضعیت به HealthCheckResult |
| `TelemetryRegistration` | اتصال ActivitySource و Meter به SDK واقعی OpenTelemetry |

این نمونه کاری را به صف یا دیتابیس ارسال نمی‌کند؛ accepted/rejected دو نتیجهٔ مصنوعی برای دیدن ابزارها هستند.

## ۳. liveness و readiness را یکی نکن

| endpoint | سؤال | اگر false شد چه تصمیمی ممکن است لازم باشد؟ |
| --- | --- | --- |
| /health/live | آیا برنامه پاسخ می‌دهد؟ | بررسی process یا restart با سیاست مناسب |
| /health/ready | آیا initialization محلی تمام شده و این نمونه کار می‌پذیرد؟ | خارج کردن از ترافیک تا رفع مشکل |

Predicate در liveness هیچ dependency check را اجرا نمی‌کند. readiness فقط check دارای tag برابر ready را می‌گیرد.
اگر StartupState آماده نباشد، readiness کد 503 می‌دهد، liveness همچنان 200 است و /work هم 503 می‌دهد.
تست این حالت state را از DI تغییر می‌دهد؛ endpoint عمومی برای دست‌کاری state اضافه نکرده‌ایم.

آمادگی این lab فقط initialization محلی را می‌سنجد؛ سلامت PostgreSQL یا سرویس همکار را ادعا نمی‌کند.
در محصول check dependency باید کوتاه، قابل لغو و با هزینهٔ مشخص باشد. اختلال دیتابیس را بی‌دلیل وارد liveness نکن؛ restart انبوه می‌تواند مشکل را بدتر کند.
پاسخ health عمومی را کم‌جزئیات نگه دار؛ stack trace، اتصال و secret در آن نریز.

## ۴. چه چیزی از درس ۷۸ اضافه شده؟

در درس ۷۸ ActivityListener محلی داشتیم. این بار SDK واقعی OpenTelemetry با عمر مدیریت‌شده توسط Host به source گوش می‌دهد و exporter دارد.

| جزء | نقش |
| --- | --- |
| ActivitySource | ایجاد span در کد برنامه |
| TracerProvider | انتخاب source، sampling و پردازش span |
| Meter و Counter | شمارش jobها با نام و unit مشخص |
| MeterProvider | جمع‌آوری و تجمیع metric |
| exporter | خروجی دادن دادهٔ جمع‌آوری‌شده |
| Resource | هویت ثابت سرویس، worker-lab و نسخهٔ 1.0.0 |

AddSource و AddMeter باید دقیقاً نام SupportDesk.WorkerLab را بگیرند؛ صرف ساختن ActivitySource بدون provider متصل، خروجی را تضمین نمی‌کند.
این lab برای نتیجهٔ قابل مشاهده AlwaysOnSampler دارد. در محصول باید sampling و حجم و هزینه را طراحی کنی؛ درخواست‌های واقعی را صرفاً با همین تنظیم آموزشی جمع‌آوری نکن.

## ۵. یک tag کم‌خطر، دو مقدار محدود

span فقط job.outcome دارد و metric فقط outcome دارد؛ مقدار هر دو accepted یا rejected است.
ورودی دیگری مانند /work/alice رد می‌شود و metric تازه تولید نمی‌کند.
اگر JobId یا email را label metric کنی، برای تعداد زیادی مقدار یکتا، سری‌های فراوان و هزینهٔ بالا می‌سازی؛ به این مشکل cardinality می‌گوییم.

نام و نتیجهٔ عملیات کافی است؛ body، query string، header و اطلاعات شخصی را tag نمی‌کنیم.
اینجا auto-instrumentation درخواست HTTP نصب نشده است؛ صرفاً span و meter خودمان را وصل کرده‌ایم. logging پیش‌فرض درخواست ASP.NET در سطح Warning فیلتر شده تا برای دیدن این lab نیازی به ثبت URLهای ورودی نباشد.
در ابزار واقعی، log و trace و metric هر سه سیاست داده و دسترسی لازم دارند.

## ۶. تست، خروجی را از SDK می‌گیرد

```powershell
dotnet test tests/082-observability/SupportDesk.Tests.csproj
python scripts/production_smoke.py
```

تست‌ها exporter حافظه‌ای کوچک به همان SDK ثبت می‌کنند؛ صرفاً متد Record را mock نمی‌کنند.
پس از درخواست واقعی WAF، ForceFlush صریح اجرا می‌شود؛ سپس نام span، outcome، وضعیت Error برای rejected و counter با مقدار یک بررسی می‌شود.
تست با email مصنوعی در query و body محرمانه نشان می‌دهد این اطلاعات وارد tagهای span نمی‌شوند؛ این ضمانتِ همهٔ agentها و ابزارهای آینده نیست.
یک تست جدا نشان می‌دهد مقدار دلخواه label تازه نمی‌سازد و readiness خراب، liveness را خراب نمی‌کند.

## ۷. چه چیزی هنوز به محیط واقعی نرفته است؟

Console exporter برای یادگیری و عیب‌یابی محلی است؛ خروجی‌اش قرارداد پایدار یا ذخیره‌سازی قابل اتکا نیست.
OTLP exporter، collector و backend مانند سامانهٔ trace در این درس راه‌اندازی نشده‌اند. برای مقصد واقعی باید endpoint، TLS، credential، retry/buffering و سیاست نگه‌داری داده مشخص شوند.
از زیاد شدن dependencyها شروع نکن؛ اول بگو کدام سؤال عملیاتی باید پاسخ بگیرد.

## تمرین

1. با تست نشان بده در حالت not-ready هیچ span کار جدیدی صادر نمی‌شود.
2. یک histogram برای مدت عملیات اضافه کن، با unit درست و بدون JobId در tag؛ جمع‌آوری آن را از SDK بررسی کن.
3. برای worker درس ۷۶ بنویس readiness چه زمانی باید false شود و در shutdown چه jobهایی ممکن است باقی بمانند.
4. سه سؤال بنویس: یکی که log جواب می‌دهد، یکی trace و یکی metric.

معیار عبور: تفاوت زنده/آماده را با status code نشان بدهی و یک span و counter را از SDK واقعی دریافت کنی، بدون tag نامحدود یا payload حساس.

## منابع رسمی

- [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [Hosting package مرجع 1.19.0](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.19.0)
- [Console exporter مرجع 1.19.0](https://www.nuget.org/packages/OpenTelemetry.Exporter.Console/1.19.0)

[قبلی](../081-ci/README.md) · [فهرست بخش](../README.md) · [نقشهٔ دوره](../../../ROADMAP.md)
[درس بعد: cache و Redis](../083-caching/README.md) آماده است.
