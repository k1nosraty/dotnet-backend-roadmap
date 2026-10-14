# درس ۰۲۱ — بازآرایی و کنترل پیچیدگی

[قبلی](../020-errors/README.md) · [خانه](../../../README.md) · [بعدی](../022-cli-delivery/README.md)

## خروجی درس

command handling را به parse، dispatch و render تقسیم می‌کنی.
با جدول رفتار نشان می‌دهی بازآرایی contract را حفظ کرده است.
برای helper و abstraction تازه معیار تصمیم می‌نویسی.

## پیش‌نیاز

Outcome، constructor injection، record و مرز presentation/application.
نمونهٔ این درس یک CLI حافظه‌ای کوچک است؛ persistence نهایی در درس ۰۲۲ یکپارچه می‌شود.

## مسئله

در کدی با چند فرمان، parsing Id، گرفتن ورودی، اجرای use case و چاپ پیام ممکن است در یک switch طولانی مخلوط شوند.
افزودن یک فرمان ساده همهٔ مسیرها را تحت تأثیر قرار می‌دهد.
می‌خواهیم اجزای قابل بررسی داشته باشیم، بدون اینکه framework فرمان پیچیده بسازیم.

## ۱. refactoring چیست؟

ساختار داخلی را تغییر می‌دهیم و رفتار قابل مشاهدهٔ مورد توافق را حفظ می‌کنیم.
اگر هم‌زمان exit code، schema یا قاعدهٔ status را عوض کنیم، بخشی از کار تغییر رفتار است و باید جدا اعلام شود.
«کد بهتر شد» جای جدول قبل و بعد را نمی‌گیرد.

## ۲. قرارداد قبل از تغییر

| فرمان | خروجی یا failure | کد خروج |
| --- | --- | --- |
| add با عنوان معتبر | summary تازه | ۰ |
| add با عنوان کوتاه | InvalidInput | ۲ |
| find با Id موجود | summary | ۰ |
| find با Id ناموجود | NotFound | ۳ |
| find با abc یا صفر | usage | ۲ |
| فرمان ناشناخته | usage | ۲ |

در نمونه، seed اولیه یک Ticket با شناسهٔ ۱ است.
هر اجرای process تازه است؛ ثبت add در اجرای بعدی باقی نمی‌ماند.
این محدودیت حافظه‌ای را با رگرسیون persistence اشتباه نگیر.

## ۳. مرحلهٔ parse

```csharp
public abstract record TicketCommand;
public sealed record AddTicket(string RawTitle) : TicketCommand;
public sealed record FindTicket(int Id) : TicketCommand;
```

recordهای فرمان مقدارهای parse شده را نگه می‌دارند.
AddTicket هنوز RawTitle دارد و قانون عنوان در application/value object بررسی می‌شود.
FindTicket در parser فقط با Id مثبت ساخته می‌شود.
caller مستقیم همچنان می‌تواند FindTicket نامعتبر بسازد؛ application باید guard خودش را حفظ کند.
record declaration خودکار domain validation نیست.

## ۴. parser خالص

```csharp
public static Outcome<TicketCommand> Parse(string[] args)
{
    ArgumentNullException.ThrowIfNull(args);
    if (args.Length == 2 && args[0] == "add")
        return Outcome<TicketCommand>.Success(new AddTicket(args[1]));
    if (args.Length == 2 && args[0] == "find" &&
        int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0)
        return Outcome<TicketCommand>.Success(new FindTicket(id));
    return Outcome<TicketCommand>.Failure(ErrorCode.InvalidInput, "Usage: add <title> | find <positive-id>");
}
```

parser Console و فایل را نمی‌خواند و service را اجرا نمی‌کند.
می‌توان با آرایهٔ string رفتار آن را مستقل بررسی کرد.
برای این نمونه syntax دقیق و case-sensitive است.
پشتیبانی از flag پیچیده یا quote parser مستقل نداریم؛ shell آرگومان quote شده را به string واحد تبدیل می‌کند.

## ۵. مرحلهٔ dispatch

```csharp
Outcome<TicketSummary> result = parsed.Value switch
{
    AddTicket add => service.Register(add.RawTitle),
    FindTicket find => service.Find(find.Id),
    _ => throw new InvalidOperationException("Unhandled command type.")
};
```

pattern روی نوع command انتخاب می‌کند.
نتیجهٔ همهٔ branchهای عادی Outcome<TicketSummary> است.
نوع فرمان جدید باید handler صریح داشته باشد.
branch پیش‌فرض برای bug پشتیبانی‌نشدن command است، نه عنوان نامعتبر کاربر.
در این اندازه، switch روشن از ثبت خودکار handler با reflection ساده‌تر است.

## ۶. مرحلهٔ render

در success، renderer summary را چاپ و صفر برمی‌گرداند.
در failure، code و message را چاپ و code CLI تعیین می‌کند.
renderer برای تصمیم‌گیری دوباره reader را صدا نمی‌زند.
اگر render خودش side effect business داشته باشد، جدا شدن ظاهری متدها کافی نیست.

## ۷. parser کجا قرار گرفت؟

CommandParser در پوشه و namespace Presentation است.
TicketCommandها در این نمونه همراه parser قرار دارند، چون syntax CLI را نمایندگی می‌کنند.
use caseهای application از آن‌ها خبر ندارند.
اگر command معنای application مستقل از CLI پیدا کرد، محل قرارداد را با همان نیاز بازبینی می‌کنیم.
صرف واژهٔ Command الزام به CQRS یا mediator ایجاد نمی‌کند.

## ۸. ترتیب بازآرایی پیشنهادی

1. سناریوهای جدول قبل را اجرا و ثبت کن.
2. تبدیل متن Id را به parser منتقل کن.
3. یک command parse شده را به service متصل کن.
4. render success و failure را در یک نقطه نگه دار.
5. branch دوم را منتقل کن.
6. همهٔ سناریوها را دوباره اجرا کن.

اگر در مرحله‌ای failure داری، همان مرحله را اصلاح کن.
بازنویسی هم‌زمان همهٔ فایل‌ها تشخیص علت تغییر رفتار را سخت می‌کند.

## ۹. extraction همیشه بهبود نیست

متدی که فقط برای کوتاه شدن خطوط ساخته شده، ممکن است فهم جریان را دشوار کند.
نام helper باید مسئولیت و قرارداد روشن داشته باشد.
اگر caller باید مدام میان ده helper تک‌خطی جابه‌جا شود، هزینهٔ خواندن را بسنج.
اگر helper چند use case واقعاً مشابه را با شرط‌های بسیار زیاد پوشش می‌دهد، احتمال abstraction نادرست وجود دارد.

## ۱۰. duplication یا abstraction؟

دو شرط یکسان نحوی ممکن است دو سیاست متفاوت باشند.
مثلاً حد طول عنوان و حد طول یادداشت هر دو عدد دارند اما یک مفهوم نیستند.
TicketTitle ارزش مشترک را متمرکز می‌کند.
ساخت GenericValidator برای هر primitive فقط برای حذف چند خط می‌تواند semantics را پنهان کند.
قانون مشترک و business meaning را قبل از حذف تکرار شناسایی کن.

## ۱۱. guard clause

وقتی parser شکست خورده، زود برمی‌گردیم.
branch success در چند if تو در تو دفن نمی‌شود.
این می‌تواند جریان را روشن کند، اما early return خودکار تمام متدها را بهتر نمی‌کند.
تضمین cleanup منابع همچنان با using یا finally لازم است.

## ۱۲. state و helper مشترک

در CLI نهایی، تغییرهای rename، note و status مراحل Load، Find، Apply و Save مشترک دارند.
یک helper محدود ChangeAsync می‌تواند این مراحل را هماهنگ کند.
اما فقط وقتی Outcome success باشد Save انجام می‌شود.
helper برای همهٔ عملیات، از report تا delete، با flagهای فراوان ساخته نمی‌شود.
delete و add قراردادهای متفاوتی دربارهٔ state و generator دارند.

## ۱۳. قرارداد helper را بنویس

اگر helper یک callback برای mutation می‌گیرد:

- callback قبل از mutation guardهایش را اجرا کند.
- failure عادی state را تغییر ندهد.
- helper در success ذخیره کند.
- I/O failure را موفقیت اعلام نکند.
- state فقط متعلق به همان عملیات باشد، نه cache مشترک بدون rollback.

در درس ۰۲۲ state هر فرمان از فایل بارگذاری می‌شود و بعد از failure ادامهٔ silent روی cache نداریم.
این الگو جای transaction concurrent را نمی‌گیرد.

## ۱۴. چیدمان نمونه

| فایل | مسئولیت |
| --- | --- |
| Presentation/CommandParser.cs | syntax به command |
| Presentation/ConsoleCommands.cs | dispatch و render کوچک |
| Application/TicketService.cs | نتیجهٔ ثبت و lookup |
| Application/Outcome.cs | قرارداد نتیجه |
| Infrastructure/InMemoryTicketStore.cs | state نمونه |
| Program.cs | seed و اتصال |

این نمونه عمداً همهٔ مرحله‌ها را به framework جدا تبدیل نکرده است.
در رشد بعدی، اندازه و مسئولیت هر فایل ملاک جداسازی بیشتر است.

## ۱۵. اجرا

[Program](../../../samples/20-ticket-refactoring/Program.cs)

```powershell
dotnet run --project samples/20-ticket-refactoring/SupportDesk.csproj -- find 1
dotnet run --project samples/20-ticket-refactoring/SupportDesk.csproj -- add "Printer issue"
dotnet run --project samples/20-ticket-refactoring/SupportDesk.csproj -- find 99
```

اولی #1: Network issue می‌دهد.
دومی #2: Printer issue می‌دهد، چون seed هر اجرا شناسهٔ ۱ دارد.
سومی NotFound و کد خروج ۳ می‌دهد.

## ۱۶. بررسی مستقل parser

به Parse آرایه‌های زیر را بده:

```csharp
new[] { "find", "1" }
new[] { "find", "abc" }
new[] { "find", "0" }
new[] { "add", "Printer issue" }
Array.Empty<string>()
```

parser نباید برای هیچ‌کدام فایل ایجاد کند یا Ticket ثبت کند.
این بررسی کوچک با اجرای end-to-end جایگزین نمی‌شود؛ هر دو سطح فایده دارند.

## تمرین مستقل

فرمان rename را به نسخهٔ تمرین اضافه کن.
ابتدا syntax را parse کن، سپس use case مستقل را صدا بزن و در پایان render کن.
شرط عنوان و Closed را در parser پنهان نکن؛ آن‌ها مربوط به application/domain هستند.
سناریوی Id ناموجود و input نامعتبر را به جدول رفتار اضافه کن.

## تمرین تکمیلی

یک متد طولانی از پروژهٔ خودت انتخاب کن و فقط یک محور را refactor کن.
قبل و بعد، خروجی و exit code را با ورودی‌های همان جدول مقایسه کن.
تغییر رفتار احتمالی را جدا ثبت کن؛ آن را تحت عنوان refactor پنهان نکن.

## معیار عبور

- [ ] parser بدون I/O بررسی می‌شود.
- [ ] dispatch به نوع command وابسته است، نه parse دوبارهٔ متن.
- [ ] renderer use case را دوباره اجرا نمی‌کند.
- [ ] جدول قبل و بعد یکسان است.
- [ ] helper مشترک قرارداد محدود دارد.
- [ ] تغییر رفتار و تغییر ساختار را جدا ثبت می‌کنم.

## منابع رسمی

- [Pattern matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching)
- [Switch expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression)
- [Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

## قدم بعد

نسخهٔ یکپارچهٔ CLI را تحویل می‌دهیم و publish و سناریوهای دادهٔ واقعی را بررسی می‌کنیم.
