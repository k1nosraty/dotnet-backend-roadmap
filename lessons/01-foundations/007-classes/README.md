# درس ۰۰۷ — کلاس، سازنده و property

[قبلی](../006-collections/README.md) · [خانه](../../../README.md) · [بعدی](../008-nullability/README.md)

## خروجی درس

درخواست پشتیبانی را از یک string به یک Ticket با شناسه، عنوان و وضعیت تبدیل می‌کنی.
فایل مدل را از Program جدا می‌کنی و تغییر وضعیت را از طریق متد انجام می‌دهی.
فرق کپی یک reference و ساخت شیء مستقل را مشاهده می‌کنی.

## پیش‌نیاز و واژگان

درس ۰۰۶، collection و متد.

| اصطلاح | معنی |
| --- | --- |
| class | تعریف ساختار و رفتار یک نوع مرجع |
| instance | شیء مشخص ساخته‌شده از کلاس |
| constructor | سازنده‌ای که هنگام new اجرا می‌شود |
| property | عضو قابل خواندن یا نوشتن با accessor |
| enum | مجموعهٔ مقدارهای نام‌دار برای یک نوع |
| namespace | نام منطقی برای گروه‌بندی نوع‌ها |

## مسئله

عنوان تنها کافی نیست؛ وضعیت درخواست نیز باید معلوم باشد.
اگر سه dictionary جدا برای عنوان و وضعیت و اولویت بسازیم، ممکن است یکی از آن‌ها رکورد را داشته باشد و دیگری نداشته باشد.
داده و رفتار مرتبط یک درخواست را در یک مدل جمع می‌کنیم.
در این نسخه اولویت هنوز اضافه نشده و تمرین توست.

## ۱. اولین کلاس ساده

```csharp
public class TicketDraft
{
    public string Title { get; set; } = string.Empty;
}
```

این مثال فقط syntax property را نشان می‌دهد و مدل نهایی ما نیست.
get خواندن و set نوشتن را مجاز می‌کند.
public یعنی از کد مصرف‌کننده قابل دسترسی است.
مقدار اولیهٔ رشتهٔ خالی هشدار مقداردهی را رفع می‌کند، اما قانون عنوان معتبر را تضمین نمی‌کند.
نباید رفع هشدار کامپایلر را با درست شدن مدل یکی بدانیم.

## ۲. وضعیت نام‌دار

```csharp
public enum TicketStatus { Open, InProgress, Closed }
```

enum خواندن کد را از مقدارهای ۰ و ۱ و ۲ واضح‌تر می‌کند.
بدون مقداردهی صریح، این عضوها در همین ترتیب مقدار عددی می‌گیرند.
تبدیل عدد به enum می‌تواند مقداری بدون نام بسازد؛ در ورودی خارجی باید اعتبار بررسی شود.
enum به تنهایی جلوی تغییر وضعیت غیرمجاز را نمی‌گیرد.

## ۳. شناسه و عنوان

```csharp
public int Id { get; }
public string Title { get; private set; }
public TicketStatus Status { get; private set; } = TicketStatus.Open;
```

Id بعد از ساخت از بیرون قابل تغییر نیست.
Title و Status از بیرون قابل خواندن‌اند، اما setter آن‌ها درون کلاس است.
این انتخاب برای کنترل مسیر تغییر است.
private set همهٔ طراحی domain را حل نمی‌کند؛ رفتار متدها هم باید درست باشد.

## ۴. سازنده

```csharp
public Ticket(int id, string title)
{
    if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
    Id = id;
    Title = NormalizeTitle(title);
}
```

سازنده نام کلاس را دارد و نوع بازگشتی نمی‌نویسیم.
new Ticket(1, "Network issue") سازنده را فراخوانی می‌کند.
nameof(id) نام پارامتر را بدون نوشتن رشتهٔ دستی می‌دهد.
throw یعنی ساخت با ورودی نامعتبر موفق نمی‌شود؛ جزئیات exception در درس ۰۰۹ می‌آید.
این guard کوچک را اینجا می‌بینی تا شیء نامعتبر ساخته نشود.

عنوان در یک نقطه Trim و بررسی می‌شود:

```csharp
private static string NormalizeTitle(string title)
{
    ArgumentNullException.ThrowIfNull(title);
    string normalized = title.Trim();
    if (normalized.Length < 3 || normalized.Length > 120)
        throw new ArgumentException("Title must contain 3-120 characters.", nameof(title));
    return normalized;
}
```

private یعنی این helper جزئی از API عمومی کلاس نیست.
static یعنی برای این محاسبه به instance و state شیء نیاز نداریم.
طول همان واحد UTF-16 درس‌های قبل است.

## ۵. متد تغییر عنوان

```csharp
public void Rename(string title) => Title = NormalizeTitle(title);
```

=> در اینجا expression-bodied method است؛ بدنهٔ کوتاه را می‌نویسد.
با setter عمومی ممکن بود کسی مستقیماً عنوان خالی بنویسد.
Rename هم در ساخت و هم در تغییر همان قانون را حفظ می‌کند.
اگر NormalizeTitle خطا بدهد، انتساب انجام نمی‌شود و عنوان قبلی باقی می‌ماند.

## ۶. رفتار وضعیت

```csharp
public void Start()
{
    if (Status != TicketStatus.Open)
        throw new InvalidOperationException("Only open tickets can be started.");
    Status = TicketStatus.InProgress;
}
```

Close هم فقط از InProgress به Closed می‌رود.
در نسخهٔ فعلی Open به Closed مستقیم مجاز نیست.
اگر بعداً نیازمندی تغییر کرد، این قانون باید آگاهانه اصلاح و بررسی شود.
کلاس از Console و فایل چیزی نمی‌داند.
رابط کنسول مسئول پیام است و مدل مسئول قانون وضعیت.

## ۷. reference چیست؟

```csharp
Ticket first = new(1, "Network issue");
Ticket alias = first;
alias.Rename("Router issue");
Console.WriteLine(first.Title);
```

خروجی Router issue است.
انتساب alias = first یک شیء Ticket تازه نساخته است.
هر دو متغیر به همان شیء اشاره می‌کنند.
این رفتار را با کپی int از درس قبل مقایسه کن.
اگر List<Ticket> را با ToList کپی کنیم نیز خود Ticketها عمیق کپی نمی‌شوند؛ در درس LINQ می‌بینیم.

## ۸. شیء مستقل

```csharp
Ticket second = new(2, "Printer issue");
```

این new یک instance دیگر می‌سازد.
تغییر first نباید عنوان یا وضعیت second را عوض کند.
ReferenceEquals برای دیدن یکسان بودن referenceها در نمونه استفاده شده است.
معنی برابری منطقی موجودیت‌ها را در بخش طراحی بررسی خواهیم کرد.
sealed در مدل مرجع اجازهٔ ارث‌بری از Ticket را نمی‌دهد؛ فعلاً برای Ticket نیاز به inheritance نداریم.

## ۹. چیدمان فایل واقعی

| فایل | مسئولیت |
| --- | --- |
| Program.cs | سناریو و نمایش نتیجه |
| Domain/Ticket.cs | نوع Ticket، وضعیت و قواعد تغییر |
| SupportDesk.csproj | ساخت پروژه |

در فایل مدل namespace برابر SupportDesk.Domain است.
Program با using SupportDesk.Domain نوع را می‌بیند.
پوشهٔ Domain به تنهایی namespace را ایجاد نمی‌کند؛ اعلان namespace در کد مهم است.
پروژهٔ SDK معمولاً فایل‌های cs داخل پوشهٔ پروژه را خودکار برای ساخت شامل می‌کند.
برای هر کلاس الزام به یک پروژهٔ csproj جدا نداریم.

## ۱۰. اجرا

[کد مدل](../../../samples/06-ticket-model/Domain/Ticket.cs) · [Program](../../../samples/06-ticket-model/Program.cs)

```powershell
dotnet run --project samples/06-ticket-model/SupportDesk.csproj
```

خروجی:

```text
First: #1 Router issue Closed
Alias is first: True
Second: #2 Printer issue Open
```

## تمرین مستقل

مدل را به منوی درس ۰۰۶ منتقل کن و Dictionary<int, Ticket> بساز.
عملیات ثبت باید شیء بسازد، جست‌وجو عنوان و وضعیت را نمایش بدهد و تغییر عنوان از Rename انجام شود.
Start و Close را به منو اضافه کن.
برای رفتار مجاز ابتدا وضعیت را بررسی کن؛ مدیریت exception را در درس ۰۰۹ کامل می‌کنیم.

## تمرین تکمیلی

enum اولویت Low، Normal و High بساز.
اولویت اولیه Normal باشد و تغییر آن از یک متد انجام شود.
برای نگهداری اولویت، dictionary جدا نساز.
تبدیل ورودی کاربر به enum باید با مقدارهای مجاز محدود شود.

## سناریوهای پذیرش

| عمل | انتظار |
| --- | --- |
| ساخت با عنوان دارای فاصله | عنوان Trim شده |
| تغییر عنوان شیء اول | شیء دوم ثابت |
| Start سپس Close | Closed |
| Close مستقیم از Open | رد تغییر؛ Open باقی بماند |
| عنوان دوکاراکتری در Rename | عنوان قبلی حفظ شود |

## سرنخ

اول مدل را بدون منو اجرا کن و رفتار reference را مشاهده کن.
بعد مرحله‌به‌مرحله نوع مقدار dictionary را عوض کن.
Title را برای ساده‌تر شدن منو دوباره public set نکن؛ مسیر تغییر را درست صدا بزن.

## معیار عبور

- [ ] فرق class و instance را توضیح می‌دهم.
- [ ] getter و private setter را می‌شناسم.
- [ ] alias شیء جدید نیست.
- [ ] مدل به Console و storage وابسته نیست.
- [ ] داده و قواعد یک Ticket در یک محل‌اند.
- [ ] مسیر فایل و namespace را مستقل توضیح می‌دهم.

## منابع رسمی

- [Classes](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes)
- [Properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties)
- [Constructors](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors)
- [Enums](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum)

## قدم بعد

در جست‌وجوی شناسهٔ ناموجود، null چه قراردادی دارد؟ این سؤال درس ۰۰۸ است.
