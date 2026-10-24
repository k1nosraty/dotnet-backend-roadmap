# درس ۰۰۹ — exception و مدیریت منابع

[قبلی](../008-nullability/README.md) · [خانه](../../../README.md) · [بعدی](../010-linq/README.md)

## خروجی درس

خطای ورودی، نقض قانون و خرابی فایل را از هم جدا می‌کنی.
از exception درست استفاده می‌کنی و فایل باز را با using آزاد می‌کنی.
می‌توانی توضیح بدهی چرا GC جای Dispose نیست.

## پیش‌نیاز

Ticket، guard سازنده و nullable در درس‌های ۰۰۷ و ۰۰۸.
در این درس برای اولین بار یک فایل لاگ کوچک می‌نویسیم؛ ذخیرهٔ مدل در JSON درس ۰۱۱ است.

## ۱. دسته‌بندی مسئله

| وضعیت | ابزار اولیه |
| --- | --- |
| متن abc به جای عدد | TryParse و پیام ورودی |
| شناسهٔ ناموجود | نتیجهٔ جست‌وجو یا null |
| Close از وضعیت Open | قانون مدل و InvalidOperationException |
| عنوان نامعتبر در فراخوانی سازنده | ArgumentException |
| فایل بدون مجوز | UnauthorizedAccessException |
| مسیر یا I/O خراب | IOException یا subtype مرتبط |

همهٔ شاخه‌های منو را با throw پیاده نمی‌کنیم.
خطای قابل انتظار کاربر را می‌توان پیش از صدا زدن مدل بررسی کرد.
مدل نیز باید در برابر caller نامعتبر از قانون خود محافظت کند.

## ۲. try/catch

```csharp
try
{
    _ = new Ticket(1, "ab");
}
catch (ArgumentException)
{
    Console.WriteLine("Invalid title rejected.");
}
```

وقتی ساخت exception می‌دهد، مسیر عادی try از همان نقطه متوقف می‌شود.
catch سازگار با نوع خطا اجرا می‌شود.
_ در اینجا discard است؛ عمداً نتیجهٔ ساخت موفق احتمالی را نگه نمی‌داریم.
پس از catch، جریان می‌تواند ادامه پیدا کند.

## ۳. قانون وضعیت

```csharp
Ticket ticket = new(1, "Network issue");
try
{
    ticket.Close();
}
catch (InvalidOperationException)
{
    Console.WriteLine($"Invalid transition rejected; status: {ticket.Status}");
}
```

Status باید Open باقی بماند، چون guard پیش از تغییر اجرا شده است.
پیام قابل فهم در UI نوشته می‌شود.
در یک API، تبدیل این خطا به status code در مرز وب انجام می‌شود؛ domain نباید HTTP response بسازد.

## ۴. catch دقیق

catch (Exception) همهٔ exceptionهای معمول سازگار را می‌گیرد.
اگر همه را به «عنوان نامعتبر» تبدیل کنی، خرابی storage و bug کد پنهان می‌شود.
فقط خطاهایی را بگیر که می‌توانی رفتار مشخص برایشان تعریف کنی.
در مرز نهایی برنامه می‌توان ثبت خطای غیرمنتظره و خروج کنترل‌شده داشت؛ ادامه دادن کورکورانه درست نیست.

catchهای خاص باید قبل از catch عمومی قرار بگیرند.
بعضی subclassها زیر IOException هستند؛ ترتیب گرفتن آن‌ها مهم است.

## ۵. حفظ stack trace

```csharp
// داخل یک catch که نیاز به ثبت و انتشار دوباره دارد:
// throw;
```

throw; همان exception جاری را دوباره منتشر می‌کند و stack trace را حفظ می‌کند.
throw ex; می‌تواند مسیر trace را از نقطهٔ جدید تغییر بدهد.
در صورت تبدیل خطا به نوع معنی‌دار تازه، exception اولیه را inner exception نگه دار.
در درس JSON این تبدیل را می‌بینی.

## ۶. finally

finally برای cleanup بعد از خروج معمول یا exception استفاده می‌شود.
return در try نیز در حالت عادی مانع finally نیست.
اما خاموشی process، قطع برق یا FailFast را تضمین‌شده ندان.
برای منابع IDisposable معمولاً using خواناتر است.

## ۷. GC و resource

GC حافظهٔ managed دسترس‌ناپذیر را جمع می‌کند.
زمان اجرای آن قرارداد آزاد شدن فوری فایل یا اتصال نیست.
FileStream و StreamWriter به resource بیرونی و buffer مرتبط‌اند.
وقتی مالک resource هستی، باید عمر آن را روشن کنی.
Dispose برای آزادسازی مشخص resource است؛ الزاماً خود شیء managed را فوراً از حافظه حذف نمی‌کند.

## ۸. using block

```csharp
using (StreamWriter writer = new(path, append: true))
{
    writer.WriteLine("Viewed ticket #1");
}
```

با پایان block، Dispose فراخوانی می‌شود؛ مسیر exception هم در خروج معمول این scope پوشش داده می‌شود.
writer بیرون block در دسترس نیست.
append:true به فایل موجود اضافه می‌کند؛ false می‌تواند آن را بازنویسی کند.
برای نوشتن لاگ نمونه از append استفاده کردیم.

## ۹. using declaration

```csharp
using StreamReader reader = new(path);
Console.WriteLine(reader.ReadLine());
```

منبع تا پایان scope مالک باز می‌ماند و سپس dispose می‌شود.
این با using System.IO که namespace وارد می‌کند فرق دارد.
اگر می‌خواهی فایل پیش از مرحلهٔ بعد بسته شود، block کوچک بساز.
در نمونه، writer در block بسته می‌شود و بعد reader فایل را باز می‌کند.

## ۱۰. ownership

اگر متدی Stream ساخته باشد، معمولاً همان کد مالک بستن آن است.
اگر Stream را از بیرون دریافت کرده باشد، باید قرارداد روشن کند چه کسی آن را می‌بندد.
بستن ناگهانی resource قرض‌گرفته می‌تواند caller را خراب کند.
بعضی wrapperها گزینهٔ leaveOpen دارند؛ آن را هنگام نیاز و طبق مستندات استفاده می‌کنیم.
همهٔ کلاس‌ها IDisposable نیستند؛ Ticket را بی‌دلیل disposable نکن.

## ۱۱. شکست Dispose

نوشتن buffer نهایی هم می‌تواند خطای I/O بدهد.
پس try باید scope using را هم در بر بگیرد.
catch فقط اطراف WriteLine و بیرون گذاشتن پایان using ممکن است یک خطای مهم را نگیرد.
cleanup خودش می‌تواند خطا بدهد؛ در ذخیرهٔ فایل موقت مراقب پنهان کردن خطای اصلی خواهیم بود.

## ۱۲. اجرای نمونه

[Program](../../../samples/08-ticket-exceptions/Program.cs)

```powershell
dotnet run --project samples/08-ticket-exceptions/SupportDesk.csproj -- exercises/activity.log
```

پوشهٔ exercises باید وجود داشته باشد.
در مسیر تازه، انتظار:

```text
Invalid title rejected.
Invalid transition rejected; status: Open
Log: Viewed ticket #1
```

با اجرای دوباره، یک خط تازه به فایل اضافه می‌شود.
reader این نمونه فقط اولین خط را نمایش می‌دهد.
این فایل لاگ آموزشی شامل دادهٔ حساس نیست؛ در محصول واقعی باید دربارهٔ محتوا و retention تصمیم بگیری.

## ۱۳. مسیر خراب

یک مسیر داخل پوشه‌ای که وجود ندارد بده.
متن پیام خطای نمونه این است:

```text
Log could not be opened or written.
```

کد خروج باید ۱ باشد.
با دستور زیر در PowerShell کد خروج آخرین برنامهٔ native را ببین:

```powershell
$LASTEXITCODE
```

مسیر خراب نباید به «عنوان نامعتبر» تبدیل شود.
برای آزمایش permission، مجوز فایل مهم شخصی را عوض نکن؛ از مسیر آزمایشی استفاده کن.

## تمرین مستقل

به منوی Ticket گرفتن خطای تغییر وضعیت اضافه کن.
فقط تغییر مجاز را در فایل activity.log ثبت کن.
اگر لاگ شکست خورد، پیام جدا بده و توضیح بده آیا تغییر مدل را ادامه می‌دهی یا کل عملیات را شکست‌خورده اعلام می‌کنی.
این سیاست تصمیم محصول است و در README باید روشن باشد.
در نمونهٔ مرجع تغییر وضعیت و ذخیرهٔ واقعی هنوز transaction واحد نیستند.

## تمرین تکمیلی

فایلی با سه خط بساز و با StreamReader همه را بخوان.
resource را در block محدود نگه دار و پس از block فایل را دوباره برای نوشتن باز کن.
بدون catch خالی، خطای مسیر ناموجود را گزارش کن.

## سرنخ

اول قانون مدل را بدون فایل بررسی کن.
سپس تعامل فایل را اضافه کن و خطای آن را جدا مدیریت کن.
از try/catch برای جایگزین کردن if ساده استفاده نکن.

## معیار عبور

- [ ] خطای ورودی و I/O را یک پیام نمی‌کنم.
- [ ] خطای transition وضعیت را تغییر نمی‌دهد.
- [ ] معنی throw; و inner exception را می‌دانم.
- [ ] resource owned را در using می‌بندم.
- [ ] GC و Dispose را یکی نمی‌دانم.
- [ ] سیاست شکست لاگ در تمرین روشن است.

## منابع رسمی

- [Exception handling](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/exception-handling)
- [Best practices for exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Using statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)
- [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-10.0)

## قدم بعد

گزارش و مرتب‌سازی Ticketها را با LINQ می‌نویسیم و زمان اجرای query را بررسی می‌کنیم.
