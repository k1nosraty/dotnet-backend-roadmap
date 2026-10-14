# درس ۰۱۷ — SOLID با بازآرایی یک قابلیت

[قبلی](../016-dependency-injection/README.md) · [خانه](../../../README.md) · [بعدی](../018-value-objects/README.md)

## خروجی درس

قابلیت ثبت و گزارش درخواست را با مرزهای کوچک بازآرایی می‌کنی.
هر اصل SOLID را با یک مشکل واقعی و یک تصمیم در کد توضیح می‌دهی.
برای «کلاس کمتر» و «جداسازی بیشتر» tradeoff می‌نویسی.

## پیش‌نیاز

درس‌های ۰۱۳ تا ۰۱۶: invariant، composition، قرارداد و DI.
این نمونه container لازم ندارد؛ constructor injection دستی برای نشان دادن وابستگی‌ها کافی است.

## مسئلهٔ قبل از بازآرایی

فرض کن Program یک متد بزرگ دارد که:

1. عنوان را از Console می‌گیرد.
2. عنوان را بررسی می‌کند.
3. شناسه می‌سازد و داده را ذخیره می‌کند.
4. گزارش را به چند قالب نمایش می‌دهد.
5. خطای ذخیره را به پیام کاربر تبدیل می‌کند.

تغییر قالب گزارش، storage و قانون عنوان به همان متد دست می‌زند.
برای تست یک گزارش مجبور می‌شویم ثبت و ورودی کنسول را هم راه بیندازیم.
این مشکل دلیل بازآرایی است؛ تعداد خطوط بزرگ به تنهایی دلیل معماری تازه نیست.

## ۱. پنج اصل به زبان تصمیم

| اصل | سؤال عملی |
| --- | --- |
| SRP | این جزء به چه دلیل مستقل تغییر می‌کند؟ |
| OCP | یک تنوع مشخص را بدون دست‌کاری core چگونه اضافه کنیم؟ |
| LSP | implementation جایگزین قرارداد caller را حفظ می‌کند؟ |
| ISP | caller فقط قابلیت‌هایی را می‌بیند که نیاز دارد؟ |
| DIP | use case به قرارداد وابسته است یا جزئیات storage؟ |

این اصول ابزار قضاوت‌اند؛ رعایت آن‌ها با شمارش interfaceها اندازه‌گیری نمی‌شود.
طراحی کوچک هم می‌تواند خوب باشد.

## ۲. SRP: یک دلیل منسجم برای تغییر

SRP به معنی «هر کلاس فقط یک متد» نیست.
یک کلاس می‌تواند چند عملیات مرتبط با همان مسئولیت داشته باشد.
Ticket عنوان، یادداشت و وضعیت مرتبط خود را مدیریت می‌کند.
InMemoryTicketStore نگهداری و lookup داده را انجام می‌دهد.
formatter دربارهٔ متن خروجی تصمیم می‌گیرد.
اگر formatter خودش فایل دیتابیس باز کند، دو دلیل تغییر نامرتبط مخلوط شده‌اند.

## ۳. جدا کردن callerهای خواندن و نوشتن

```csharp
public interface ITicketWriter { int Add(string title); }
public interface ITicketReader { Ticket? FindById(int id); }
```

registration فقط writer می‌خواهد.
report فقط reader می‌خواهد.
یک InMemoryTicketStore می‌تواند هر دو را پیاده کند؛ لازم نیست دو storage موازی بسازیم.
تفکیک interface به معنی تکثیر داده نیست.

قرارداد Add در این نمونه: عنوان معتبر را ثبت و شناسهٔ مثبت برگرداند.
عنوان نامعتبر باید رد شود و شناسهٔ بعدی مصرف نشود.
قرارداد FindById همان درس ۰۱۵ است: Id مثبت، null برای ناموجود و failure جداگانه.

## ۴. service ثبت

```csharp
public int Register(string title)
{
    ArgumentNullException.ThrowIfNull(title);
    string normalized = title.Trim();
    if (normalized.Length < 3 || normalized.Length > 120)
        throw new ArgumentException("Title must contain 3-120 characters.", nameof(title));
    return writer.Add(normalized);
}
```

service با writer از constructor کار می‌کند و storage را درون خودش new نمی‌کند.
این نسخه یک use case کوچک است؛ wrapper بدون نیاز واقعی همیشه به service جدا احتیاج ندارد.
در این مسیر بعداً قواعد و persistence واقعی رشد می‌کنند.
validation فعلی در application و مدل تکرار دارد؛ در درس بعد مفهوم عنوان معتبر را متمرکز می‌کنیم.
این تکرار را پنهان نمی‌کنیم یا با utility عمومی بی‌معنی حل نمی‌کنیم.

## ۵. OCP: تنوع خروجی

```csharp
public interface ITicketFormatter
{
    string Format(Ticket ticket);
}
```

PlainTextFormatter و CompactFormatter دو قالب دارند.
TicketReportService برای formatter تازه branch اضافه نمی‌کند.
composition root implementation را انتخاب می‌کند.
این یک محور تغییر مشخص است: قالب گزارش.
برای همهٔ نیازمندی‌های خیالی از قبل plugin system نمی‌سازیم.

## ۶. report service

```csharp
public string Report(int id)
{
    var ticket = reader.FindById(id);
    return ticket is null ? "Ticket not found." : formatter.Format(ticket);
}
```

نبود نتیجه به پیام نمونه تبدیل می‌شود.
failure reader همچنان exception می‌ماند.
در نسخهٔ production، نتیجهٔ application ممکن است DTO یا outcome معنایی باشد و presentation متن را بسازد.
برای محدود نگه داشتن محور این درس string استفاده شده است؛ در درس ۰۱۹ خروجی DTO داریم.

## ۷. LSP: syntax کافی نیست

تمام formatterها باید Ticket معتبر را بدون تغییر آن گزارش کنند.
یک formatter که فقط Open را قبول می‌کند و برای Closed خطای بی‌دلیل می‌دهد قرارداد را نقض می‌کند.
یک formatter که بعد از چاپ، Ticket.Close را صدا می‌زند نیز جایگزین سالم نیست.
کامپایلر امضا را بررسی می‌کند، اما قرارداد بدون mutation را به تنهایی اثبات نمی‌کند.
پس قبل و بعد Format، state مدل را در سناریوهای بررسی مقایسه می‌کنیم.

## ۸. LSP برای reader

reader خراب نباید برای تمام شناسه‌ها null برگرداند و به caller بگوید داده‌ای وجود ندارد.
Id نامعتبر هم نباید در یک implementation خطا و در دیگری Ticket جعلی بدهد.
contract مشترک درس ۰۱۵ برای همهٔ implementationها بررسی می‌شود.
جایگزینی درست شامل رفتار، شرایط ورودی و failure است.
یکسان بودن نام متد یا type کافی نیست.

## ۹. ISP: قرارداد بر اساس نیاز caller

اگر interface بزرگی با Add، Delete، SendEmail، RenderHtml و Find بسازیم، report به قابلیت‌هایی وابسته می‌شود که نمی‌خواهد.
در این نمونه سه قرارداد کوچک writer، reader و formatter داریم.
به تعداد هر متد یک interface تازه هم نساخته‌ایم؛ معنی مرز و مصرف‌کننده مهم است.
اگر عملیات بعدی به قابلیت‌های مرتبط نیاز داشت، قرارداد را با همان نیاز تحلیل می‌کنیم.

## ۱۰. DIP: جهت وابستگی

Application قرارداد reader و writer را تعریف می‌کند.
Infrastructure آن قراردادها را پیاده می‌کند.
use case برای اجرای business flow به Dictionary یا FilePath اشاره نمی‌کند.
Program implementation را می‌شناسد تا اجزا را متصل کند.
این با «هیچ فایلی نباید concrete type را ببیند» فرق دارد؛ composition root باید concreteها را انتخاب کند.

## ۱۱. DI و DIP یک چیز نیستند

constructor injection راهی برای رساندن dependency است.
DIP دربارهٔ انتخاب مرز و جهت وابستگی است.
اگر یک service concrete و بسیار وابسته به فایل را فقط inject کنیم، همهٔ couplingها خودکار حل نمی‌شوند.
اگر interfaceی تمام جزئیات SQL را لو بدهد، نام I به تنهایی مرز خوبی ایجاد نمی‌کند.
اصل را با مشکل caller بررسی کن.

## ۱۲. ثبت ناموفق و شناسه

store ابتدا Ticket معتبر می‌سازد، سپس در dictionary اضافه و nextId را زیاد می‌کند.
اگر ساخت Title شکست بخورد، شمارنده افزایش پیدا نمی‌کند.
nextId از نوع long است تا بررسی ظرفیت int پیش از cast انجام شود.
این generator تک‌نخی است؛ concurrent storage یا دیتابیس نیست.

## ۱۳. بازآرایی مرحله‌ای

1. رفتار فعلی را با جدول ورودی و خروجی ثبت کن.
2. formatting را استخراج کن و دوباره خروجی را بررسی کن.
3. reader و writer را تعریف کن و storage را پشت آن‌ها بگذار.
4. serviceهای ثبت و گزارش را به قراردادها وابسته کن.
5. ساخت concreteها را در Program نگه دار.
6. سناریوهای failure و حفظ state را بررسی کن.

هم‌زمان قواعد دامنه را تغییر نده تا علت تفاوت خروجی مشخص باشد.
نباید برای «زیباتر شدن» refactor خطای ثبت را به موفقیت خاموش تبدیل کند.

## ۱۴. چیدمان نمونه

| مسیر | مسئولیت |
| --- | --- |
| Domain/Ticket.cs | invariant و state |
| Application/Contracts.cs | قراردادهای مورد نیاز use case |
| Application/Services.cs | ثبت و گزارش |
| Infrastructure/InMemoryTicketStore.cs | داده و generator نمونه |
| Presentation/Formatters.cs | قالب خروجی |
| Program.cs | اتصال اجزا و سناریو |

چند type کوچک در Contracts.cs برای خواندن نمونه کنار هم‌اند؛ هر interface الزام به فایل جدا ندارد.
وقتی تعداد و اندازه زیاد شد، جدا کردن فایل‌ها را با قابلیت و مسئولیت انجام می‌دهیم.

## ۱۵. اجرا

[Program](../../../samples/16-ticket-solid/Program.cs)

```powershell
dotnet run --project samples/16-ticket-solid/SupportDesk.csproj
```

خروجی:

```text
#1: Network issue (Open)
#1 Open
Invalid registration rejected.
Next successful ID: 2
Ticket not found.
State after reporting: Open
```

ثبت نامعتبر نباید شناسهٔ ۲ را مصرف کند.
دو قالب متفاوت نباید state درخواست را تغییر دهند.

## تمرین مستقل

یک JsonLineFormatter برای یک خط JSON بساز.
بدون تغییر TicketReportService آن را به برنامه متصل کن.
با System.Text.Json escape متن را انجام بده؛ رشتهٔ JSON را دستی با concatenation نساز.
برای عنوان فارسی و کوتیشن سناریوی بررسی بنویس.

## تمرین بازآرایی

نسخهٔ تمرین قبلی خودت را با این مرزها مقایسه کن.
اگر کلاس جدید فقط یک عبور بدون سیاست و بدون نیاز آیندهٔ واقعی است، دلیل وجودش را بررسی کن.
سه تصمیم را در README بنویس: چیزی که جدا کردی، چیزی که فعلاً یکجا نگه داشتی، و دلیل هر دو.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| عنوان نامعتبر | ثبت و مصرف شناسه انجام نشود |
| قالب تازه | report service تغییر نکند |
| formatterهای مختلف | state ثابت |
| Id ناموجود | پیام ناموجود |
| reader خراب | failure به پیام ناموجود تبدیل نشود |

## سرنخ

اصل را از روی حرف اول حفظ نکن؛ قبل و بعد یک تغییر واقعی را مقایسه کن.
OCP به معنی «هیچ کدی هرگز تغییر نکند» نیست.
SRP به معنی «کلاس تک‌متدی» نیست.
DIP به معنی «interface برای هر کلاس» نیست.

## معیار عبور

- [ ] برای هر اصل یک مثال از کد خودم دارم.
- [ ] قالب تازه بدون branch در service اضافه می‌شود.
- [ ] reader و writer مطابق نیاز caller جدا هستند.
- [ ] رفتار شکست ثبت ثابت است.
- [ ] دلیل تعداد کلاس‌ها را توضیح می‌دهم.
- [ ] هزینهٔ abstraction و تکرار validation را می‌شناسم.

## منابع رسمی

- [Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Common application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

## قدم بعد

عنوان معتبر را به value object تبدیل می‌کنیم و معنی equality را بررسی می‌کنیم.
