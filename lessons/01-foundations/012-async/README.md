# درس ۰۱۲ — Task، async/await و لغو عملیات

[قبلی](../011-files-json/README.md) · [خانه](../../../README.md) · [ارزیابی بخش اول](../README.md)

## خروجی درس

یک نسخهٔ کنسولی با مدل Ticket، گزارش LINQ و ذخیرهٔ JSON اجرا می‌کنی.
خواندن و نوشتن فایل را با await انجام می‌دهی و token لغو را تا storage می‌فرستی.
فرق انتظار I/O و کار CPU را توضیح می‌دهی.

## پیش‌نیاز

همهٔ درس‌های ۰۰۱ تا ۰۱۱، به‌ویژه exception و persistence.
نمونه بدون کتابخانهٔ async خارجی ساخته می‌شود.
این درس پایان Foundation است؛ پروژهٔ CLI و معماری آن در بخش دوم بیشتر رشد می‌کند.

## ۱. مسئلهٔ انتظار

خواندن فایل ممکن است منتظر دیسک بماند.
در یک سرویس با درخواست‌های زیاد، نگه داشتن thread هنگام انتظار I/O هزینه دارد.
async به طراحی مسیر انتظار کمک می‌کند؛ هدف آن لزوماً سریع‌تر کردن یک فایل کوچک نیست.
در یک کنسول تک‌کاربره سود ظاهری ممکن است کم باشد، اما قرارداد درست را یاد می‌گیریم.

## ۲. Task

Task نمایندهٔ تکمیل احتمالی یک عملیات است.
Task<T> پس از تکمیل موفق یک نتیجهٔ T می‌دهد.
Task الزاماً thread تازه نیست.
عملیات می‌تواند موفق، faulted یا cancelled تمام شود.
برای خواندن فایل و برگرداندن Ticketها از Task<List<Ticket>> استفاده می‌کنیم.

## ۳. async و await

```csharp
public async Task<List<Ticket>> LoadAsync(CancellationToken cancellationToken)
{
    string json = await File.ReadAllTextAsync(path, cancellationToken);
    return SnapshotMapper.Import(JsonSerializer.Deserialize<StoreSnapshot>(json, Options));
}
```

این قطعه ساده‌شده است؛ handling فایل غایب و بررسی لغو در فایل کامل وجود دارد.
async اجازهٔ await در بدنه را می‌دهد.
await نتیجه را وقتی عملیات تکمیل شد در دسترس قرار می‌دهد.
اگر عملیات از قبل کامل باشد، لازم نیست suspend رخ بدهد.
کد پیش از اولین await به طور عادی اجرا می‌شود.

## ۴. زنجیرهٔ async

```csharp
List<Ticket> tickets = await store.LoadAsync(token);
await store.SaveAsync(tickets, token);
```

caller باید تکمیل عملیات را منتظر بماند.
بدون await، ممکن است برنامه قبل از پایان ذخیره خارج شود یا خطا را درست مشاهده نکند.
در top-level program می‌توان await نوشت؛ کامپایلر entry point مناسب ایجاد می‌کند.
Async suffix یک قرارداد نام‌گذاری برای متدهای asynchronous است.

## ۵. Result و Wait

خواندن Task.Result یا صدا زدن Wait thread caller را block می‌کند.
در بعضی contextها می‌تواند deadlock ایجاد کند و در سرور فشار thread را بالا ببرد.
به جای تبدیل async به sync در وسط مسیر، زنجیره را با await نگه دار.
ConfigureAwait انتخابی با زمینهٔ کاربرد است؛ در این Foundation بدون دلیل به همهٔ متدها اضافه نمی‌کنیم.

## ۶. async void

برای متد application معمولاً Task برگردان تا caller completion و exception را مشاهده کند.
async void معمولاً برای event handler لازم است و قرارداد عادی service نیست.
در این نمونه handler مربوط به Ctrl+C async نیست؛ فقط درخواست لغو می‌دهد.
ذخیره در handler اجرا نمی‌شود.

## ۷. I/O و CPU

Serialize این نمونه synchronous است و CPU مصرف می‌کند.
WriteAllTextAsync برای انتظار نوشتن استفاده می‌شود.
async کردن نام Save باعث نمی‌شود محاسبهٔ JSON خودکار روی thread دیگر برود.
Task.Run برای کار CPU انتخابی است، اما wrapper پیش‌فرض دور هر I/O یا متد service نیست.
برای JSON بزرگ باید streaming، اندازه و هزینهٔ پردازش جدا بررسی شوند.

## ۸. token و source

```csharp
using CancellationTokenSource shutdown = new();
CancellationToken token = shutdown.Token;
shutdown.Cancel();
```

source مالک درخواست لغو است؛ token به عملیات داده می‌شود.
Cancel درخواستی cooperative است، نه کشتن اجباری thread.
عملیات باید token را بپذیرد و بررسی کند.
Dispose روی source جای Cancel نیست.

## ۹. انتشار لغو

```csharp
cancellationToken.ThrowIfCancellationRequested();
await File.WriteAllTextAsync(temporary, json, cancellationToken);
cancellationToken.ThrowIfCancellationRequested();
File.Move(temporary, path, overwrite: true);
```

در این ذخیره قبل از شروع و پیش از جایگزینی مقصد لغو بررسی می‌شود.
WriteAllTextAsync هم token را می‌گیرد.
اگر نوشتن نیمه‌کاره شود، فایل موقت در cleanup پاک می‌شود و مقصد هنوز جایگزین نشده است.
Move در این نمونه synchronous است و token ندارد.
لغو بعد از آخرین check ممکن است بعد از commit داده رخ بدهد؛ لغو تضمین rollback نیست.
این مرز باید در قرارداد روشن باشد.

## ۱۰. OperationCanceledException

```csharp
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    Console.WriteLine("Cancelled.");
}
```

when یک exception filter است.
فقط لغو مرتبط با درخواست shutdown را در این مرز پیام لغو می‌دهیم.
خطای permission یا JSON را لغو معرفی نمی‌کنیم.
در سرویس واقعی token timeout و درخواست کاربر ممکن است policyهای متفاوت داشته باشند.

## ۱۱. Ctrl+C و محدودیت کنسول

handler نمونه e.Cancel را true می‌کند و source.Cancel را صدا می‌زند.
در finally handler از Console.CancelKeyPress جدا می‌شود.
Console.ReadLine در این نمونه synchronous است و token لغو ندارد.
اگر هنگام prompt لغو کنی، ممکن است لازم باشد Enter بزنی تا ReadLine برگردد و check بعدی لغو را ببیند.
پس «همهٔ برنامه فوراً cancellable است» ادعای این نمونه نیست.
پیچیدن ReadLine در Task.Run با token، خود خواندن شروع‌شده را جادویی لغو نمی‌کند.

## ۱۲. اجرای نمونهٔ یکپارچه

[Program](../../../samples/11-ticket-async/Program.cs) · [Store](../../../samples/11-ticket-async/Storage/JsonTicketStore.cs)

```powershell
New-Item -ItemType Directory -Force exercises | Out-Null
dotnet run --project samples/11-ticket-async/SupportDesk.csproj -- exercises/foundation.json
```

منو شامل افزودن، فهرست، شروع، بستن، گزارش و خروج است.
در شروع داده Load می‌شود و پس از هر تغییر موفق Save می‌شود.
در خروج عادی هیچ تغییر ذخیره‌نشده‌ای در صف نداریم.
storage failure باعث خروج با کد ۱ می‌شود؛ برنامه با state حافظهٔ ذخیره‌نشده ادامه نمی‌دهد.

## ۱۳. مسیر عملی مرحله‌به‌مرحله

1. گزینهٔ ۱ و عنوان Network issue را وارد کن؛ شناسهٔ ۱ بگیر.
2. گزینهٔ ۴ با شناسهٔ ۱ را بزن؛ تغییر مستقیم باید رد شود.
3. گزینهٔ ۳ با شناسهٔ ۱ را بزن؛ InProgress ذخیره شود.
4. گزینهٔ ۴ با همان شناسه را بزن؛ Closed ذخیره شود.
5. گزینهٔ ۵ باید Open: 0، InProgress: 0 و Closed: 1 بدهد.
6. خارج شو و برنامه را دوباره با همان path اجرا کن.
7. گزینهٔ ۲ باید همان Ticket با وضعیت Closed را نشان بدهد.

اگر فایل از قبل داده دارد، شناسهٔ تازه ممکن است ۱ نباشد؛ در مسیر آزمایشی تازه شروع کن.

## ۱۴. آزمایش لغو تکرارپذیر

```powershell
dotnet run --project samples/11-ticket-async/SupportDesk.csproj -- exercises/cancel-check.json --cancel-demo
```

این mode پیش از Save درخواست لغو می‌دهد.
انتظار Cancelled است و فایل جدید نباید ساخته شود.
اگر فایل مقصد از قبل بود، بایت‌هایش باید ثابت بمانند.
این تست pre-cancel است و اندازه‌گیری timing لغو وسط I/O نیست.
تست میانی قطعی با وابستگی قابل کنترل در فصل تست پوشش داده می‌شود.

## ۱۵. هم‌زمانی با WhenAll

```csharp
Task<string> first = File.ReadAllTextAsync(firstPath, token);
Task<string> second = File.ReadAllTextAsync(secondPath, token);
string[] results = await Task.WhenAll(first, second);
```

این مثال مستقل است؛ firstPath و secondPath باید قبل از آن تعریف شوند.
برای دو خواندن مستقل می‌توان هر دو را شروع و تکمیلشان را منتظر شد.
WhenAll به تنهایی محدودیت concurrency ایجاد نمی‌کند.
دو Save به همان فایل را با WhenAll اجرا نکن؛ sample store قرارداد تک‌نویسنده دارد.
هم‌زمانی read/write روی فایل مشترک نیازمند طراحی جدا است.

## ۱۶. خطاهای async

exception عملیات awaited در caller قابل catch است.
اگر Task را رها کنی و await نکنی، مسیر گزارش خطا و completion را از دست می‌دهی.
fire-and-forget برای ذخیرهٔ حیاتی این پروژه مناسب نیست.
await موفقِ Save بعد از جایگزینی فایل علامت موفقیت این قرارداد است، نه تضمین تحمل قطع برق.

## ۱۷. نقش فایل‌ها

| فایل | نقش |
| --- | --- |
| Program | تعامل، هماهنگی و مرز پیام خطا |
| Domain/Ticket | قوانین عنوان و وضعیت |
| Storage/Snapshot | قرارداد فایل و mapping |
| Storage/JsonTicketStore | I/O و انتشار cancellation |

این هنوز معماری نهایی حرفه‌ای نیست.
در بخش دوم use case، interface، dependency injection و تست‌پذیری مرزها را بررسی می‌کنیم.
فعلاً هیچ فولدر service یا controller خالی صرفاً برای شکل ظاهری ایجاد نشده است.

## تمرین مستقل

منوی خودت را به LoadAsync و SaveAsync منتقل کن.
token را به همهٔ عملیات I/O مربوط بده.
پس از failure ذخیره، رفتار ادامه یا توقف را صریح تعریف کن.
عملیات rename و find را از تمرین‌های قبل نگه دار.
عنوان نامعتبر یا وضعیت نامعتبر نباید save تازه انجام بدهد.

## تمرین تکمیلی

از CancellationTokenSource با CancelAfter برای یک Task.Delay طولانی استفاده کن.
لغو را با catch مناسب مشاهده کن و با تأخیر کامل مقایسه کن.
Delay شبیه‌سازی انتظار است، نه benchmark واقعی دیسک.
برای WhenAll دو فایل مستقل آزمایشی بخوان و ترتیب results را بررسی کن.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| ثبت و restart | همان داده و وضعیت |
| Close از Open | رد تغییر، فایل ثابت |
| JSON خراب | خروج ۱؛ فایل بازنویسی نشود |
| pre-cancel Save | Cancelled؛ مقصد ثابت |
| ID ناموجود | پیام ناموجود؛ منو ادامه دارد |
| EOF در prompt | خروج بدون حلقهٔ سریع |

## معیار عبور

- [ ] Task را thread فرض نمی‌کنم.
- [ ] Save حیاتی را await کرده‌ام.
- [ ] .Result و .Wait در زنجیرهٔ application ندارم.
- [ ] cancellation تا فایل منتقل می‌شود.
- [ ] مرز commit و محدودیت لغو را توضیح می‌دهم.
- [ ] می‌دانم ReadLine فعلی فوراً cancellable نیست.
- [ ] ارزیابی پایان Foundation را مستقل انجام می‌دهم.

## منابع رسمی

- [Async programming](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)
- [Task cancellation](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation)
- [Asynchronous file access](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/using-async-for-file-access)

## پایان بخش اول

۱۲ درس Foundation کامل شده است.
بخش دوم از encapsulation و invariant شروع می‌شود؛ درس ۰۱۳ هنوز نوشته نشده است.
پیش از ورود، تمرین پایان بخش و توضیح مرز کلاس‌ها را انجام بده.
