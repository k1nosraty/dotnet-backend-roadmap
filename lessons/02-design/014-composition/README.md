# درس ۰۱۴ — composition و inheritance

[قبلی](../013-encapsulation/README.md) · [خانه](../../../README.md) · [بعدی](../015-interfaces/README.md)

## خروجی درس

یک سیاست بررسی درخواست را به service تزریق دستی می‌کنی.
یک مثال محدود و معتبر از ارث‌بری برای گزارش می‌سازی.
برای استفاده از «داشتن یک وابستگی» یا «بودن یک زیرنوع»، دلیل مشخص می‌آوری.

## پیش‌نیاز

کلاس و سازنده، encapsulation و قرارداد mutation در درس ۰۱۳.
interface خودمان را در درس بعد طراحی می‌کنیم؛ نمونهٔ این درس dependency concrete دارد.

## واژگان

| اصطلاح | معنی |
| --- | --- |
| composition | ساخت قابلیت با همکاری اجزای دیگر |
| inheritance | تعریف یک نوع مشتق از نوع پایه |
| polymorphism | استفاده از چند implementation از طریق قرارداد مشترک |
| virtual / override | رفتار قابل جایگزینی و پیاده‌سازی جایگزین آن |
| abstract | تعریف ناقص برای تکمیل در نوع مشتق |

## ۱. نیازمندی جدید

می‌خواهیم درخواست باز با تعداد مشخصی یادداشت، برای بررسی بیشتر علامت بخورد.
در این مثال NoteThreshold عددی بین ۱ و ۵ است.
اگر درخواست Closed است، NeedsReview همیشه false است.
اگر بسته نیست و تعداد یادداشت به threshold رسیده، true است.
این یک heuristic آموزشی است؛ تعداد یادداشت در محصول واقعی الزاماً معیار پیچیدگی نیست.

با threshold برابر ۲ و دو یادداشت، درخواست نیاز به review دارد.
با همان Ticket و threshold برابر ۳، هنوز ندارد.
سیاست نمایش و بررسی را بدون تغییر invariantهای Ticket کنار آن قرار می‌دهیم.

## ۲. انتخاب غلط: ارث‌بری برای استفاده از کد

ممکن است بخواهیم ReviewService را از Ticket مشتق کنیم تا به Notes دسترسی داشته باشیم.
اما service یک درخواست پشتیبانی نیست.
وابستگی به دادهٔ Ticket دلیل کافی برای رابطهٔ inheritance نیست.
Ticket در مدل فعلی sealed است و اجازهٔ چنین مشتقی هم نمی‌دهد.
برداشتن sealed صرفاً برای دسترسی راحت‌تر به state تصمیم خوبی نیست.

## ۳. composition در سیاست

```csharp
public sealed class TicketReviewPolicy
{
    public int NoteThreshold { get; }
    public TicketReviewPolicy(int noteThreshold)
    {
        if (noteThreshold < 1 || noteThreshold > 5)
            throw new ArgumentOutOfRangeException(nameof(noteThreshold));
        NoteThreshold = noteThreshold;
    }
    public bool NeedsReview(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return ticket.Status != TicketStatus.Closed && ticket.Notes.Count >= NoteThreshold;
    }
}
```

این کلاس state درخواست را تغییر نمی‌دهد.
پیکربندی threshold در constructor معتبر می‌شود.
نمونهٔ policy بعد از ساخت threshold جدید نمی‌گیرد.
نیاز نیست Ticket برای هر threshold یک subclass تازه داشته باشد.

## ۴. service یک policy دارد

```csharp
public sealed class TicketReviewService
{
    private readonly TicketReviewPolicy policy;
    public TicketReviewService(TicketReviewPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        this.policy = policy;
    }
    public string GetDecision(Ticket ticket) => policy.NeedsReview(ticket) ? "Review required" : "No review";
}
```

service با policy همکاری می‌کند و آن را در خودش new نمی‌کند.
caller policy مناسب را انتخاب می‌کند.
این تزریق دستی وابستگی است؛ برای DI container هنوز package لازم نداریم.
private readonly رابطه را روشن می‌کند، نه اینکه همهٔ objectهای dependency به طور عمومی thread-safe باشند.

## ۵. ساخت دو ترکیب

```csharp
TicketReviewService strict = new(new TicketReviewPolicy(2));
TicketReviewService relaxed = new(new TicketReviewPolicy(3));
```

دو instance از یک کلاس service با دو policy داریم.
ساختار inheritance رشد نکرده و فایل Ticket تغییر نکرده است.
برای تغییر الگوریتم policy در آینده interface می‌تواند کمک کند، اما برای دو مقدار config الزام ندارد.
کلاس‌های ConcreteServiceA و ConcreteServiceB که فقط عدد متفاوت دارند ضرورتی ندارند.

## ۶. composition هم هزینه دارد

هر جزء جدید یک مرز و هزینهٔ فهم ایجاد می‌کند.
برای یک شرط یک‌بارمصرف، استخراج کلاس همیشه لازم نیست.
اینجا policy مستقل، قابل پیکربندی و قابل بررسی است و قرار است در چند caller به کار برود.
اگر همیشه یک مقدار ثابت و یک caller داریم، می‌توان طراحی ساده‌تر را هم ارزیابی کرد.
هدف این درس ساخت کلاس برای تمام ifها نیست.

## ۷. یک رابطهٔ inheritance محدود

برای گزارش، یک پایه با Generate مشترک تعریف می‌کنیم:

```csharp
public abstract class TicketReport
{
    public string Generate(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return Format(ticket);
    }
    protected abstract string Format(Ticket ticket);
}
```

Generate قرارداد public خواندن و گزارش را ارائه می‌دهد.
Format بخش متغیر گزارش است و protected است؛ caller عادی آن را مستقیم صدا نمی‌زند.
abstract class مستقیم با new ساخته نمی‌شود.
متد abstract بدنه ندارد و مشتق concrete باید آن را پیاده کند.

## ۸. override

```csharp
public sealed class PlainTextTicketReport : TicketReport
{
    protected override string Format(Ticket ticket) => $"#{ticket.Id}: {ticket.Title} ({ticket.Status})";
}
```

: TicketReport رابطهٔ ارث‌بری را اعلام می‌کند.
override قرارداد Format پایه را تکمیل می‌کند.
در اینجا base state mutable یا setterهای حساس نداریم.
به همین دلیل مثال از ارث‌بری برای مدل Ticket کم‌خطرتر و معنی‌دارتر است.

DetailedTicketReport هم Format دیگری دارد و Notes.Count را نمایش می‌دهد.
هر دو باید بدون تغییر Ticket گزارش بدهند.
اگر یکی در Format درخواست را ببندد، قرارداد گزارش را نقض کرده است، حتی اگر کامپایل شود.

## ۹. polymorphism در caller

```csharp
List<TicketReport> reports = new() { new PlainTextTicketReport(), new DetailedTicketReport() };
foreach (TicketReport report in reports)
{
    Console.WriteLine(report.Generate(ticket));
}
```

caller با نوع پایه کار می‌کند.
پیاده‌سازی مناسب Format در runtime بر اساس نوع واقعی اجرا می‌شود.
اگر caller برای هر نوع خاص یک if بنویسد، ممکن است benefit قرارداد مشترک را از دست بدهیم.
polymorphism فقط از inheritance کلاس نمی‌آید؛ interface نیز آن را فراهم می‌کند.

## ۱۰. virtual و new

برای متد دارای بدنهٔ قابل جایگزینی در پایه می‌توان virtual نوشت.
override همان رفتار virtual را جایگزین می‌کند.
new برای پنهان کردن عضو است و با override یکی نیست.
در این درس به new member hiding نیاز نداریم.
اگر هنگام استفاده از reference نوع پایه و نوع مشتق رفتار متفاوت غیرمنتظره دیدی، declarationها را بررسی کن.

## ۱۱. شرط جایگزینی

نوع مشتق باید در جایی که caller نوع پایه می‌خواهد، قرارداد مورد انتظار را حفظ کند.
مثلاً همهٔ TicketReportها باید برای Ticket معتبر گزارش بسازند و Ticket را تغییر ندهند.
اگر یک مشتق فقط Open را قبول کند اما قرارداد پایه همهٔ وضعیت‌ها را می‌پذیرد، جایگزینی مشکل دارد.
در درس SOLID این موضوع با اصل جایگزینی دقیق‌تر بررسی می‌شود.
صرف وجود رابطهٔ is-a در زبان روزمره، رفتار سازگار را تضمین نمی‌کند.

## ۱۲. جدول تصمیم

| نیاز | انتخاب اولیه | دلیل |
| --- | --- | --- |
| service از policy استفاده می‌کند | composition | service یک policy نیست |
| دو تنظیم threshold | دو instance policy | الگوریتم یکسان، config متفاوت |
| چند formatter با مرحلهٔ مشترک | پایهٔ abstract محدود | قرارداد و orchestration مشترک |
| فقط قابلیت مشترک بدون implementation مشترک | interface در درس بعد | ارث‌بری کد لازم نیست |
| مدل با invariant حساس | حفظ مرز بسته | مشتق می‌تواند مسیر تغییر را پیچیده کند |

## ۱۳. چیدمان فایل‌ها

| مسیر | مسئولیت |
| --- | --- |
| Domain/Ticket.cs | invariantهای درخواست |
| Routing/TicketReviewPolicy.cs | قانون علامت‌گذاری برای review |
| Application/TicketReviewService.cs | هماهنگی تصمیم و شکل نتیجهٔ نمونه |
| Reporting/TicketReport.cs | قرارداد گزارش و دو formatter |
| Program.cs | ساخت و انتخاب اجزا |

در برنامهٔ واقعی، متن نمایشی service ممکن است به DTO یا نتیجهٔ معنایی تبدیل شود.
اینجا string برای کوچک نگه داشتن نمونه استفاده شده است.
این service هنوز controller، repository یا HTTP نیست.

## ۱۴. اجرا

[Program](../../../samples/13-ticket-composition/Program.cs)

```powershell
dotnet run --project samples/13-ticket-composition/SupportDesk.csproj
```

خروجی‌های اصلی:

```text
Threshold 2: Review required
Threshold 3: No review
#1: Network issue (Open)
#1: Network issue; notes=2
Unchanged: Open; notes=2
Closed: No review
```

دو formatter نباید عنوان، تعداد یادداشت یا وضعیت را تغییر دهند.

## تمرین مستقل

یک CompactTicketReport مشتق بساز که فقط Id و Status را گزارش کند.
آن را به همان List پایه اضافه کن؛ caller نباید برای این نوع branch تازه داشته باشد.
سپس policy را در دو caller استفاده کن و دلیل استفاده از composition را توضیح بده.

## تمرین تکمیلی

بخش Format را با یک collaborator formatter بازطراحی کن تا بدون ارث‌بری به همان خروجی برسی.
interface سفارشی را هنوز لازم نیست تعریف کنی؛ می‌توانی دو طراحی روی کاغذ مقایسه کنی و کد آن را در درس بعد کامل کنی.
هزینهٔ تعداد نوع‌ها، امکان reuse و coupling را در README بنویس.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| threshold صفر یا شش | ساخت policy رد شود |
| تعداد کمتر از threshold | No review |
| تعداد برابر threshold | Review required |
| Closed با تعداد کافی | No review |
| گزارش با هر formatter | state Ticket ثابت |

## سرنخ

از relation برای گرفتن access راحت‌تر استفاده نکن.
اول قرارداد caller را بنویس و سپس رابطهٔ نوع‌ها را انتخاب کن.
برای ورودی null مشخص کن خطا در کدام مرز رد می‌شود.

## معیار عبور

- [ ] رابطهٔ has-a و is-a را با مثال کد توضیح می‌دهم.
- [ ] policy را درون service new نمی‌کنم.
- [ ] abstract و override را می‌شناسم.
- [ ] formatter مدل را mutate نمی‌کند.
- [ ] config متفاوت را با subclass بی‌دلیل مدل نمی‌کنم.
- [ ] محدودیت‌های جایگزینی را در README نوشته‌ام.

## منابع رسمی

- [Inheritance](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/inheritance)
- [Polymorphism](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/polymorphism)
- [Abstract and sealed classes](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/abstract-and-sealed-classes-and-class-members)

## قدم بعد

مصرف‌کنندهٔ داده را از implementation خواندن جدا می‌کنیم و قرارداد interface می‌نویسیم.
