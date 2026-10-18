# درس ۰۰۹ — وقتی عملیات خطا می‌دهد، چه کنیم؟

[قبلی](../008-nullability/README.md) · [خانه](../../../README.md) · [بعدی](../010-linq/README.md)

## در این درس فقط دو هدف اصلی داریم

۱. وقتی عملیاتی انجام نمی‌شود، بفهمیم برنامه از کجا به کجا می‌رود و چطور پیام مناسب بدهیم.
۲. وقتی فایل باز می‌کنیم، بعد از استفاده آن را درست ببندیم.

برای شروع، لازم نیست همهٔ اسم‌های exception یا جزئیات GC را حفظ کنی.
ابتدا بخش‌های ۱ تا ۶ را انجام بده؛ نکته‌های پیشرفتهٔ آخر درس را می‌توانی در مرور دوم بخوانی.
پیش‌نیاز: شرط if، کلاس Ticket و قواعد سازنده از درس‌های ۷ و ۸.

## ۱. exception یعنی چه؟

فرض کن قانون برنامه این است: «عنوان نباید کمتر از سه حرف باشد.»
اگر بخشی از برنامه عنوان دوحرفی بفرستد، کد مسئول این قانون می‌تواند بگوید: «با این ورودی نمی‌توانم ادامه بدهم.»
در C# یکی از راه‌های اعلام این شکست، ساختن و پرتاب کردن exception با `throw` است.

وقتی exception پرتاب می‌شود، اجرای عادی از همان خط متوقف می‌شود و برنامه دنبال `catch` سازگار می‌گردد.
اگر جایی آن را مدیریت نکنیم، ممکن است اجرای برنامه با خطا پایان یابد.
خود exception مشکل را تعمیر نمی‌کند؛ فقط شکست را اعلام می‌کند.

## ۲. یک مثال کامل؛ خط‌به‌خط دنبال کن

این مثال برای یاد گرفتن مسیر اجراست؛ عمداً داخل آن خطا ایجاد می‌کنیم.
هنوز نیازی به کلاس Ticket یا فایل نداریم.

برای امتحان مثال‌های مستقل، از ریشهٔ repo یک پروژهٔ تمرین بساز:

```powershell
dotnet new console -o exercises/lesson09 --framework net10.0
Set-Location exercises/lesson09
```

اگر پروژه را قبلاً ساخته‌ای، دوباره آن را نساز؛ فقط وارد همان پوشه شو.
کد زیر را **جای تمام محتوای Program.cs همین پروژهٔ تمرین** بگذار و `dotnet run` بزن.
در پروژهٔ console ساخته‌شده با SDK، نام‌های معمول مثل Console و StreamWriter از طریق implicit usings در دسترس‌اند.

```csharp
string title = "ab";

Console.WriteLine("Before");

try
{
    if (title.Length < 3)
    {
        throw new ArgumentException("Title is too short.");
    }

    Console.WriteLine("Accepted");
}
catch (ArgumentException error)
{
    Console.WriteLine("Rejected: " + error.Message);
}

Console.WriteLine("After");
```

خروجی:

```text
Before
Rejected: Title is too short.
After
```


این خروجی را این‌طور بخوان:

| مرحله | اتفاق |
| --- | --- |
| ۱ | Before چاپ می‌شود. |
| ۲ | شرط درست است؛ طول ab برابر ۲ است. |
| ۳ | throw خطا را اعلام می‌کند؛ بقیهٔ try اجرا نمی‌شود. |
| ۴ | catch از نوع ArgumentException اجرا می‌شود و پیام خطا را چاپ می‌کند. |
| ۵ | برنامه بعد از catch ادامه می‌یابد و After چاپ می‌شود. |

چرا Accepted چاپ نشد؟ چون پس از throw، به آن خط نرسیدیم.
`error` نام متغیری است که exception دریافت‌شده را نگه می‌دارد؛ `error.Message` متن پیام آن است.

حالا فقط مقدار title را به `"Network"` عوض کن و دوباره اجرا کن.
این بار باید Before، سپس Accepted و در آخر After چاپ شود. چون throw رخ نداده، catch اجرا نمی‌شود.
قبل از اجرا خروجی را حدس بزن؛ هدف فهمیدن مسیر اجراست، نه حفظ کردن دستورها.

## ۳. try و catch هرکدام چه کار می‌کنند؟

`try` دور عملیاتی قرار می‌گیرد که ممکن است شکست بخورد.
`catch (ArgumentException ...)` فقط خطاهای سازگار با همان نوع را می‌گیرد.
نام ArgumentException معمولاً می‌گوید آرگومانِ داده‌شده به یک متد یا سازنده مناسب نبوده است.

هر نوع خطا معنی خودش را دارد:

| اتفاق | برخورد اولیهٔ مناسب |
| --- | --- |
| کاربر به جای عدد، abc نوشته | TryParse و پیام ورودی؛ مثل درس‌های قبلی |
| تیکت با این شناسه پیدا نشده | بررسی نتیجه یا null |
| سازنده عنوان نامعتبر دریافت کرده | محافظت مدل با ArgumentException |
| تلاش برای بستن تیکت در وضعیت نادرست | InvalidOperationException |
| نوشتن فایل انجام نشده | بررسی خطای مرتبط با فایل |

برای if ساده لازم نیست try/catch بسازی. در رابط کاربری، ورودی قابل انتظار را بررسی می‌کنیم؛ خود مدل هم در برابر فراخوانی نامعتبر محافظت می‌کند.
همهٔ خطاها را با پیام «عنوان نامعتبر است» جواب نده؛ مشکل فایل با مشکل عنوان فرق دارد.

## ۴. حالا به Ticket خودمان برگردیم

این مثال به کلاس Ticket پروژهٔ درس نیاز دارد. برای اجرای آن در همان پروژهٔ تمرین، ابتدا فایل کلاس را کپی کن؛ از داخل exercises/lesson09:

```powershell
Copy-Item ../../samples/08-ticket-exceptions/Domain/Ticket.cs ./Ticket.cs
```

اکنون کد زیر را جای Program.cs بگذار و اجرا کن؛ Ticket.cs را نگه دار.
کلاس مورد استفاده همان [Ticket.cs](../../../samples/08-ticket-exceptions/Domain/Ticket.cs) است.
قانون آن این است: ابتدا Open، سپس Start و InProgress، و در آخر Close.

```csharp
using SupportDesk.Domain;

Ticket ticket = new Ticket(1, "Network issue");

try
{
    ticket.Close();
}
catch (InvalidOperationException)
{
    Console.WriteLine("Start the ticket before closing it.");
}

Console.WriteLine(ticket.Status);
```

خروجی:

```text
Start the ticket before closing it.
Open
```


در این کد، Close قبل از Start خطا می‌دهد.
چون بررسی قانون **پیش از تغییر Status** انجام شده، تیکت همچنان Open است.
catch فقط پیام می‌دهد؛ به‌صورت خودکار تیکت را نمی‌بندد یا قانون مدل را تغییر نمی‌دهد.
برای مسیر موفق، قبل از Close متد Start را صدا می‌زنیم.

در ابتدای نمونهٔ اصلی، `new Ticket(1, "ab")` هم به همین شکل ArgumentException می‌دهد.
اگر `_ = new Ticket(...)` دیدی، `_` یعنی نتیجهٔ ساخته‌شده را نگه نمی‌داریم؛ برای فهم خطا لازم نیست از این شکل کوتاه استفاده کنی.

## ۵. موضوع دوم: فایل را باز کردیم؛ چه کسی آن را می‌بندد؟

فایل باز، فقط یک مقدار ساده مثل int نیست؛ برنامه از سیستم‌عامل اجازهٔ استفاده از یک منبع گرفته است.
بعد از کار باید آن را آزاد کند. برای این کار در C# معمولاً از `using` استفاده می‌کنیم.
این using با `using SupportDesk.Domain;` که namespace را معرفی می‌کند، دو کاربرد متفاوت دارد.

به پروژهٔ تمرین exercises/lesson09 برگرد و Program.cs را کامل با کد زیر جایگزین کن.

```csharp
using (StreamWriter writer = new StreamWriter("activity.log", append: true))
{
    writer.WriteLine("Viewed ticket #1");
}

using (StreamReader reader = new StreamReader("activity.log"))
{
    Console.WriteLine(reader.ReadLine());
}
```

خروجی:

```text
Viewed ticket #1
```


این بار اتفاق‌ها به‌ترتیب هستند:

۱. StreamWriter فایل activity.log را برای نوشتن باز می‌کند.
۲. WriteLine یک خط می‌نویسد.
۳. با رسیدن به پایان بلوک اول، using متد Dispose را صدا می‌زند و کار writer تمام می‌شود.
۴. بعد StreamReader همان فایل را می‌خواند و در پایان بلوک دوم آزاد می‌شود.

`append: true` یعنی خط جدید به انتهای فایل موجود اضافه شود؛ فایل قبلی از ابتدا پاک نشود.
با اجرای دوباره، خط دیگری اضافه می‌شود. ReadLine در این مثال فقط اولین خط را می‌خواند.
فایل در پوشهٔ فعلی اجرای برنامه است؛ با `dotnet run` داخل پوشهٔ تمرین، همان‌جا ساخته می‌شود.

using هنگام خروج معمول از بلوک، حتی به‌خاطر exception، Dispose را انجام می‌دهد.
این رفتار را با تضمین cleanup هنگام قطع برق یا پایان ناگهانی process یکی ندان.
GC مسئول جمع‌آوری حافظهٔ managed است؛ زمان مشخصی برای بستن فوری فایل به ما وعده نمی‌دهد.
فعلاً لازم نیست GC را دقیق بدانی؛ فایلِ متعلق به خودت را با using مدیریت کن.

## ۶. اگر خود فایل باز نشد چه؟

در پوشهٔ تمرین، پوشه‌ای به نام missing-folder نساز و کد زیر را امتحان کن.

```csharp
try
{
    using (StreamWriter writer = new StreamWriter("missing-folder/activity.log"))
    {
        writer.WriteLine("Viewed ticket #1");
    }
}
catch (IOException)
{
    Console.WriteLine("Could not write the log.");
    Environment.ExitCode = 1;
}
```

خروجی:

```text
Could not write the log.
```


پوشهٔ والد وجود ندارد، پس ساخت writer شکست می‌خورد و پیام مربوط به فایل چاپ می‌شود.
نوع خطای این حالت DirectoryNotFoundException است که زیرمجموعهٔ IOException است؛ بنابراین این catch آن را می‌گیرد.
`Environment.ExitCode = 1` هم نتیجهٔ ناموفق را برای اجراکنندهٔ برنامه مشخص می‌کند.
در PowerShell، بلافاصله بعد از `dotnet run`، با `$LASTEXITCODE` عدد ۱ را ببین.

دقت کن try دور **تمام بلوک using** است؛ حتی مرحلهٔ بستن writer ممکن است هنگام نوشتن دادهٔ نهایی خطا بدهد.
خطای نداشتن مجوز، UnauthorizedAccessException، نوع جداگانه‌ای دارد؛ نمونهٔ اصلی آن را جدا مدیریت می‌کند.

## ۷. نمونهٔ اصلی درس را حالا اجرا کن

کد کامل: [Program.cs](../../../samples/08-ticket-exceptions/Program.cs).
اگر داخل پوشهٔ تمرین هستی، ابتدا با `Set-Location ../..` به ریشهٔ repo برگرد.

```powershell
New-Item -ItemType Directory -Force exercises | Out-Null
dotnet run --project samples/08-ticket-exceptions/SupportDesk.csproj -- exercises/activity.log
```

در مسیر تازه خروجی زیر را می‌بینی:

```text
Invalid title rejected.
Invalid transition rejected; status: Open
Log: Viewed ticket #1
```

اکنون هر خط خروجی را به یک بخش وصل کن: خطای عنوان، خطای وضعیت، و کار با فایل.
این نمونه همان سه مثال را کنار هم گذاشته است؛ لازم نیست از ابتدا همهٔ آن را یک‌جا یاد بگیری.

## تمرین‌های کوچک، به همین ترتیب

۱. در مثال بخش ۲، title را یک بار کوتاه و یک بار معتبر بگذار و مسیر چاپ پیام‌ها را توضیح بده.
۲. در نمونهٔ Ticket، ترتیب Start و Close را اصلاح کن و تأیید کن Status به Closed رسیده است.
۳. در مثال فایل، نام را به three-lines.log و append را به false تغییر بده؛ سه بار WriteLine بنویس و با سه بار ReadLine آن‌ها را بخوان. این فایل فقط برای همین تمرین است.
۴. مسیر خراب را امتحان کن و مطمئن شو پیام مربوط به فایل می‌بینی، نه پیام عنوان.

### تمرین پروژه؛ پس از تمرین‌های کوچک

به منوی Ticket گرفتن خطای تغییر وضعیت اضافه کن و فقط تغییر مجاز را در activity.log بنویس.
اگر نوشتن لاگ شکست خورد، پیام جدا بده و در README تصمیم بگیر آیا تغییر مدل را حفظ می‌کنی یا عملیات را ناموفق اعلام می‌کنی.
در این نمونه، تغییر object و نوشتن فایل یک transaction واحد نیستند؛ catch به‌تنهایی آن‌ها را rollback نمی‌کند.

<details>
<summary>مطالب مرور دوم: finally، throw مجدد و مالکیت منبع</summary>

- finally برای cleanup پس از خروج معمول یا exception است؛ return از try هم در حالت عادی مانع آن نیست. برای IDisposable، using معمولاً خواناتر است.
- `using StreamReader reader = new StreamReader(path);` بدون بلوک، reader را در پایان scope جاری آزاد می‌کند. برای بستن زودتر، بلوک کوچک‌تر بنویس.
- فقط خطایی را catch کن که برایش رفتار مشخص داری. `catch (Exception)` و ادامهٔ بی‌قید برنامه می‌تواند bug را پنهان کند. catch خاص را قبل از عمومی بنویس.
- داخل catch، `throw;` خطای جاری را با حفظ stack trace دوباره منتشر می‌کند. `throw error;` ممکن است نقطهٔ trace را تغییر دهد.
- اگر خطا را به نوع تازه تبدیل کردی، علت اولیه را به‌عنوان inner exception نگه دار؛ نمونهٔ آن در درس JSON می‌آید.
- اگر منبع را خودت ساخته‌ای، معمولاً مسئول آزاد کردنش هستی. اگر از caller قرض گرفته‌ای، قرارداد مشخص کند چه کسی آن را می‌بندد؛ بستن خودسرانه می‌تواند caller را خراب کند.
- همهٔ objectها IDisposable نیستند؛ Ticket را برای استفاده از using بی‌دلیل تغییر نده. Dispose هم الزاماً object را فوراً از حافظه پاک نمی‌کند.

این نکته‌ها را پس از اجرای مثال‌های اصلی مرور کن؛ نقطهٔ شروع درس نیستند.

</details>

## برای رفتن به درس بعد، این‌ها کافی است

- [ ] می‌توانم بگویم بعد از throw کدام خط‌ها اجرا نمی‌شوند و کدام catch اجرا می‌شود.
- [ ] خطای عنوان، خطای وضعیت و خطای فایل را با هم اشتباه نمی‌گیرم.
- [ ] می‌دانم using در پایان بلوک منبع را آزاد می‌کند و چرا writer باید قبل از reader بسته شود.
- [ ] مثال‌ها را خودم اجرا کرده‌ام و خروجی را از روی مسیر کد توضیح می‌دهم.

## منابع رسمی برای مراجعه

- [Exception handling](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/exception-handling)
- [Best practices for exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Using statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)
- [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-10.0)

## قدم بعد

در درس ۱۰ ابتدا با عددها انتخاب و مرتب‌سازی را تمرین می‌کنیم، بعد گزارش Ticketها را می‌سازیم.
