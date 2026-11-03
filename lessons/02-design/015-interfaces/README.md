# درس ۰۱۵ — interface و قرارداد قابل جایگزینی

[قبلی](../014-composition/README.md) · [خانه](../../../README.md) · [بعدی](../016-dependency-injection/README.md)

## خروجی درس

یک service می‌سازی که برای گزارش درخواست به قرارداد خواندن وابسته است.
دو implementation را بدون تغییر service جابه‌جا می‌کنی.
ناموجود بودن داده را از failure زیرساخت جدا نگه می‌داری.

## پیش‌نیاز

آشنایی با composition، constructor injection دستی، nullable و exception.
هنوز DI container یا دیتابیس جدید نمی‌سازیم.

## مسئله

کلاس TicketSummaryService باید عنوان و وضعیت را با شناسه بخواند.
اگر خودش فایل یا dictionary را بسازد، تغییر منبع داده و بررسی رفتار service سخت می‌شود.
یک مرز کوچک می‌خواهیم: «یک Ticket با شناسه پیدا کن».
این مرز را از نیاز caller استخراج می‌کنیم، نه از فهرست همهٔ متدهای یک کلاس.

## ۱. تعریف interface

```csharp
public interface ITicketReader
{
    Ticket? FindById(int id);
}
```

یک `interface` مشخص می‌کند کد مصرف‌کننده چه عملیاتی می‌تواند درخواست کند و چه نوع نتیجه‌ای بگیرد. در این نمونه، عملیات پیداکردن درخواست با شناسه است؛ روش خواندن را هر implementation تعیین می‌کند. implementation همان کلاسی است که بدنهٔ این عملیات را می‌نویسد و لازم نیست از کلاس پایهٔ مشترکی ارث ببرد.

پیشوند `I` در نام interface قرارداد رایج .NET است. بااین‌حال، نام و signature کافی نیست: معنای رفتار یا `semantics` را هم باید مشخص کنیم، مثلاً اینکه در نبود درخواست `null` برگردد و در خرابی خواندن exception بدهیم.

## ۲. قرارداد دقیق

| موضوع | قرارداد فعلی |
| --- | --- |
| ورودی Id | مثبت؛ غیرمثبت ArgumentOutOfRangeException |
| نتیجهٔ موجود | Ticket با همان Id |
| نتیجهٔ ناموجود | null |
| خطای خواندن | exception؛ به null تبدیل نشود |
| روش نگهداری | caller نباید به آن وابسته باشد |
| هویت reference | تضمین نمی‌شود که هر بار همان instance برگردد |

این قرارداد `synchronous` است: فراخواننده تا برگشت متد، نتیجه یا خطا را دریافت می‌کند. اگر خواندن به انتظار طولانی I/O نیاز داشته باشد، می‌توان قرارداد جداگانه‌ای با خروجی `Task` و token لغو نوشت. نباید پشت همین امضای فعلی، کار را شروع و رها کنیم؛ در آن صورت فراخواننده راهی برای دنبال‌کردن پایانش ندارد. نسخهٔ `FindByIdAsync` را در مراحل بعد طراحی می‌کنیم.

## ۳. implementation در حافظه

```csharp
public sealed class InMemoryTicketReader : ITicketReader
{
    private readonly Dictionary<int, Ticket> tickets;
    public InMemoryTicketReader(IEnumerable<Ticket> tickets)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        this.tickets = tickets.ToDictionary(t => t.Id);
    }
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return tickets.TryGetValue(id, out Ticket? ticket) ? ticket : null;
    }
}
```

دادهٔ اولیه به dictionary تبدیل می‌شود.
شناسهٔ تکراری هنگام ToDictionary خطا می‌دهد؛ silently یکی را جای دیگری نمی‌گذاریم.
این تبدیل ساختار collection را کپی می‌کند، اما خود Ticketها همچنان referenceهای مشترک‌اند.
مدل Ticket از تغییر نامعتبر محافظت می‌کند؛ reader snapshot عمیق یا concurrent store نیست.

## ۴. implementation جایگزین

کلاس `FixedTicketReader` برای شناسهٔ ۱۰ یک درخواست نمونه می‌دهد و برای بقیه `null`. رفتار آن `deterministic` است؛ با ورودی یکسان و شرایط همین نمونه، نتیجهٔ قراردادی مشخصی می‌دهد. این پاسخ ثابت برای آزمایش جایگزینی است و داده را در فایل یا دیتابیس نگه نمی‌دارد.
هر فراخوانی موفق ممکن است instance تازه بسازد؛ قرارداد فعلی reference equality را وعده نداده است.
هر دو reader باید ورودی غیرمثبت را رد کنند.
اگر implementationها در این مورد رفتارهای ناسازگار داشته باشند، interface یکسان مشکل را حل نمی‌کند.

## ۵. وابستگی service

```csharp
public sealed class TicketSummaryService
{
    private readonly ITicketReader reader;
    public TicketSummaryService(ITicketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }
    public string GetSummary(int id)
    {
        var ticket = reader.FindById(id);
        return ticket is null ? "Ticket not found." : $"#{ticket.Id}: {ticket.Title} ({ticket.Status})";
    }
}
```

کد service قرارداد را می‌بیند، نه dictionary یا مسیر فایل را.
constructor dependency را صریح اعلام می‌کند.
service لازم نیست بداند یک نمونهٔ واقعی reader چگونه ساخته شده است.
این نوع جداسازی زمینهٔ تست و dependency inversion را فراهم می‌کند.

## ۶. اتصال دستی

```csharp
ITicketReader reader = new InMemoryTicketReader(seed);
TicketSummaryService service = new(reader);
```

کلاس قابل نمونه‌سازی، مثل `InMemoryTicketReader`، یک نوع `concrete` است. این نوع را در `Program` می‌سازیم و به service می‌دهیم. محل اتصال اجزای برنامه `composition root` نام دارد. اگر reader عوض شود، انتخاب را در همین محل تغییر می‌دهیم؛ متد گزارش همچنان فقط interface را می‌شناسد.

## ۷. جایگزینی بدون تغییر caller

```csharp
TicketSummaryService memoryService = new(new InMemoryTicketReader(seed));
TicketSummaryService demoService = new(new FixedTicketReader());
```

کد GetSummary در هر دو یکی است.
فقط دادهٔ قابل خواندن تغییر می‌کند.
این همان polymorphism از طریق interface است.
interface الزام به کلاس پایهٔ مشترک ندارد.

## ۸. failure را ناموجود نکن

کلاس FailingTicketReader در نمونه عمداً IOException می‌دهد.
service آن را به Ticket not found تبدیل نمی‌کند.
Program در مرز بیرونی خطا را گزارش و کد خروج ۱ را تنظیم می‌کند.
اگر service همهٔ Exceptionها را بگیرد و null بدهد، خرابی زیرساخت به نبود داده تبدیل شده است.
این می‌تواند در محصول واقعی باعث تصمیم اشتباه caller شود.

## ۹. تست‌پذیری چه معنی دارد؟

می‌توان reader کوچک قابل کنترل به service داد و خروجی را بررسی کرد.
برای هر تست گزارش، لازم نیست فایل واقعی یا دیتابیس اجرا شود.
اما تست service با fake اثبات نمی‌کند reader واقعی درست است.
implementation واقعی بررسی مستقل و در آینده integration test لازم دارد.
صرف تعداد mockهای زیاد معیار کیفیت طراحی نیست.

## ۱۰. interface برای همهٔ کلاس‌ها؟

کلاس Ticket entity نیازی ندارد فقط برای داشتن I در نام به ITicket تبدیل شود.
helper NormalizeText هم لزوماً interface عمومی لازم ندارد.
در اینجا مرز داده واقعاً implementationهای متفاوت و caller مستقل دارد.
اگر فقط یک کلاس کوچک بدون مرز تغییر داریم، interface ممکن است هزینهٔ اضافه ایجاد کند.
انتخاب باید با مثال نیاز پروژه توجیه شود.

## ۱۱. قرارداد کوچک

این reader فقط FindById دارد.
متدهای Delete، Save، SendEmail و RenderHtml را برای تکمیل ظاهری interface اضافه نمی‌کنیم.
consumer گزارش فقط خواندن می‌خواهد.
این ایده در درس SOLID با interface segregation بررسی می‌شود.
همچنین طراحی یک generic repository برای همهٔ entityها از این نمونه نتیجه نمی‌شود.

## ۱۲. چه چیزی interface تضمین نمی‌کند؟

| interface نشان می‌دهد | به تنهایی تضمین نمی‌کند |
| --- | --- |
| امضا و نوع خروجی | صحت lookup |
| قابلیت جایگزینی نحوی | سازگاری semantic |
| مرز caller | thread safety |
| احتمال null | تشخیص همهٔ failureها |
| متد مشترک | سرعت یا persistence |

قرارداد متن، implementation و سناریوهای بررسی باید با هم سازگار باشند.
کامپایلر نمی‌تواند از روی نام FindById ثابت کند Id نتیجه درست است.

## ۱۳. چیدمان فایل‌ها

| مسیر | مسئولیت |
| --- | --- |
| Application/ITicketReader.cs | port مورد نیاز caller |
| Application/TicketSummaryService.cs | use case گزارش |
| Infrastructure/InMemoryTicketReader.cs | lookup واقعی در حافظه |
| Infrastructure/FixedTicketReader.cs | implementationهای آموزشی ثابت و خراب |
| Domain/Ticket.cs | مدل و invariantها |
| Program.cs | اتصال و پیام خطا |

اینجا همه در یک csproj هستند؛ پوشه‌ها assembly boundary نیستند.
جداسازی به چند project در فصل معماری انجام می‌شود.
اسم Infrastructure به معنی امن یا پایدار شدن خودکار کد نیست.

## ۱۴. اجرا

[Program](../../../samples/14-ticket-interfaces/Program.cs)

```powershell
dotnet run --project samples/14-ticket-interfaces/SupportDesk.csproj
```

خروجی اصلی:

```text
#1: Network issue (Open)
Ticket not found.
#10: Known demo ticket (Open)
Ticket not found.
Memory retained: #1: Network issue (Open)
```

برای مسیر شکست:

```powershell
dotnet run --project samples/14-ticket-interfaces/SupportDesk.csproj -- --failure
```

باید پیام Reader failed بگیری و کد خروج ۱ باشد.
این مسیر نباید پیام Ticket not found بدهد.

## تمرین مستقل

یک reader بر اساس List<Ticket> بساز که همان قرارداد را پیاده کند.
GetSummary را تغییر نده و فقط composition root را عوض کن.
Id مثبت موجود، Id ناموجود و Id غیرمثبت را روی هر دو implementation بررسی کن.
شناسهٔ تکراری در seed را طبق قرارداد مستند رد کن.

## تمرین تکمیلی

قرارداد async پیشنهادی را روی کاغذ طراحی کن:

```csharp
Task<Ticket?> FindByIdAsync(int id, CancellationToken cancellationToken);
```

رفتار null، invalid Id، cancellation و failure را تعریف کن.
توضیح بده چرا افزودن پسوند `Async` کافی نیست: اگر متد عملیات را شروع کند و Task آن را به فراخواننده ندهد یا پایانش را دنبال نکند، فراخواننده نمی‌تواند موفقیت و خطای خواندن را تشخیص بدهد.
برای reader حافظه‌ای فعلی async فقط برای شباهت به دیتابیس ضروری نیست.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| Id موجود | Ticket با همان Id |
| Id ۹۹ ناموجود | null و پیام ناموجود |
| Id صفر یا منفی | خطای قرارداد |
| reader خراب | failure جدا از null |
| جابه‌جایی implementation | service بدون تغییر |

## سرنخ

برای تعریف contract از نیاز مصرف‌کننده شروع کن.
متد caller را با جزئیات dictionary یا FilePath آلوده نکن.
برای هر implementation همان جدول رفتار را اجرا کن.

## معیار عبور

- [ ] امضای interface و semantics آن را جدا می‌دانم.
- [ ] service نوع concrete را انتخاب نمی‌کند.
- [ ] دو implementation رفتار قراردادی سازگار دارند.
- [ ] failure به null تبدیل نمی‌شود.
- [ ] کپی dictionary را کپی عمیق Ticket نمی‌دانم.
- [ ] مرز پوشه و مرز assembly را یکی نمی‌گیرم.

## منابع رسمی

- [Interfaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces)
- [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Dependency injection overview](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview)

## قدم بعد

همین اجزا را در DI container ثبت می‌کنیم و عمر instanceها را مشاهده می‌کنیم.
