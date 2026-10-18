# درس ۰۷۶ — کار پس‌زمینه از شروع تا توقف

می‌خواهیم دو job را به‌ترتیب پردازش کنیم، برای هر job وابستگی تازه بسازیم و هنگام توقف برنامه منتظر task خودمان بمانیم.
ابتدا فقط اجرای موفق را می‌بینیم؛ سپس لغو و صف پر را بررسی می‌کنیم.

## ۱. اجرا و خروجی

از ریشهٔ مخزن، با SDK 10:

```powershell
dotnet run --project samples/75-background-work/SupportDesk.csproj
dotnet test tests/076-background-work/SupportDesk.Tests.csproj
```

خروجی برنامه:

```text
completed=2; disposed=2
```

این شمارنده می‌گوید دو job اجرا شده‌اند و دو processor متعلق به scopeها dispose شده‌اند.
کاری به ایمیل یا دیتابیس واقعی ارسال نمی‌شود؛ DemoProcessor فقط نتیجه را در حافظه ثبت می‌کند.

## ۲. مسیر یک job را دنبال کن

| فایل/کلاس | مسئولیت |
| --- | --- |
| `Program.cs` | ساخت Host، شروع، افزودن دو job، بستن ورودی، انتظار پایان، توقف |
| `JobQueue` در `Worker.cs` | صف حافظه‌ای با ظرفیت دو و یک خواننده |
| `JobWorker` | خواندن job و ساخت scope برای همان job |
| `DemoProcessor` | اجرای کار آزمایشی و آزادسازی resource |
| `JobJournal` | شاهد حافظه‌ای برای تست و خروجی برنامه |

`BackgroundService` یک task بلندمدت دارد؛ Host شروع و توقف آن را مدیریت می‌کند.
در .NET 10 کل ExecuteAsync روی background thread اجرا می‌شود؛ به ترتیب اجرای قسمت قبل از اولین await تکیه نکن.
worker ما بلافاصله می‌تواند منتظر صف بماند؛ نبود job به معنی مشغول کردن CPU نیست.

## ۳. چرا scope برای هر job؟

worker معمولاً singleton است، اما DbContext و خیلی از سرویس‌های پردازش scoped هستند.
اگر processor را مستقیم در constructor worker بگیری، عمر scoped را به singleton گره می‌زنی.
در فایل Worker.cs این قسمت را پیدا کن:

```csharp
await using var scope = scopes.CreateAsyncScope();
await scope.ServiceProvider.GetRequiredService<IJobProcessor>()
    .ProcessAsync(job, stoppingToken);
```

اول scope ساخته می‌شود، سپس processor از همان scope resolve می‌شود و بعد از پایان job، حتی در exception، scope آزاد می‌شود.
`await using` برای resource دارای `IAsyncDisposable` هم منتظر آزادسازی می‌ماند.
تست اول دو شناسهٔ scope متفاوت و dispose شدن هر دو را بررسی می‌کند؛ فقط تعداد resolveها را نمی‌شمارد.

## ۴. صف پر چه می‌شود؟

Channel ظرفیت دو دارد. `FullMode.Wait` یعنی اگر دو job منتظر باشند، enqueue بعدی باید منتظر جای خالی بماند.
این انتظار، backpressure است: تولیدکننده سریع‌تر از مصرف‌کننده نمی‌تواند بی‌نهایت حافظه مصرف کند.

تست `Full_queue_wait_can_be_cancelled_without_adding_a_job` بدون شروع worker صف را پر می‌کند، تلاش سوم را لغو می‌کند، سپس بررسی می‌کند فقط jobهای ۱ و ۲ در صف مانده‌اند.
این آزمون به Sleep یا سرعت سیستم وابسته نیست.
اگر ورودی complete شده باشد، enqueue تازه دیگر پذیرفته نمی‌شود؛ فراخواننده باید این پایان را در قرارداد خود در نظر بگیرد.

## ۵. تمام شدن کار با خاموش شدن برنامه فرق دارد

| مسیر | چه اتفاقی می‌افتد؟ |
| --- | --- |
| `queue.Complete()` و انتظار ExecuteTask | ورودی بسته می‌شود؛ jobهای موجود خوانده می‌شوند و loop تمام می‌شود |
| `host.StopAsync()` | token توقف لغو می‌شود؛ job جاری باید همکاری کند و باقی صف ممکن است اجرا نشود |
| kill یا crash | ممکن است حتی فرصت آزادسازی هم نباشد؛ حافظه از بین می‌رود |

Program از مسیر اول استفاده می‌کند تا هر دو job تمام شوند، سپس Host را stop می‌کند.
این **تضمین drain در همهٔ shutdownها نیست**. تست توقف، processor را عمداً وسط await نگه می‌دارد؛ پس از StopAsync باید cancellation و dispose هر دو دیده شوند.
Timeout خود Host نیز محدود است. اگر processor token را نادیده بگیرد، shutdown قابل اتکا نیست.

هر job را await می‌کنیم. `Task.Run` بدون نگه داشتن task یا async void به این حلقه اضافه نکن؛ Host باید بتواند پایان کار را دنبال کند.

## ۶. خطای یک job

در این lab، InvalidOperationException قرارداد رد شدن قابل انتظار job است و با event ثابت ثبت می‌شود؛ job بعدی ادامه پیدا می‌کند.
لغو ناشی از stoppingToken دوباره پرتاب می‌شود و loop بیرونی آن را پایان طبیعی worker می‌داند.
خطای پیش‌بینی‌نشده مثل bug برنامه را با `catch (Exception) { }` مخفی نکرده‌ایم؛ رفتار Host برای خرابی background service اهمیت دارد.
در محصول، exception اختصاصی برای خطای قابل انتظار بهتر از InvalidOperationException عمومی است.

صف حافظه‌ای، retry پایدار، dead letter و تحویل حداقل یک‌بار ندارد. اگر هر job نباید گم شود، ذخیرهٔ پایدار و قرارداد تحویل لازم است؛ در بخش عملیات به آن برمی‌گردیم.

## تمرین

1. در DemoProcessor یک await قابل لغو اضافه کن و اجرای موفق را دوباره ببین.
2. تست توقف را بخوان و توضیح بده چرا قبل از StopAsync منتظر signal شروع processor می‌مانیم.
3. برای Complete شدن صف، تست enqueue بعد از پایان بنویس و انتظار ChannelClosedException داشته باش.
4. بنویس در crash کدام داده از دست می‌رود و چرا dispose شدن، جای persistence را نمی‌گیرد.

معیار عبور: عمر worker و processor را رسم یا توضیح بدهی و تفاوت Complete و StopAsync را با تست نشان بدهی.

## منابع رسمی

- [Scoped services in BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service)
- [Hosted services و shutdown](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0)
- [تغییر ExecuteAsync در .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task)
- [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)

[قبلی](../075-http-resilience/README.md) · [بعدی](../077-logging/README.md) · [فهرست بخش](../README.md)
