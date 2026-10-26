# درس ۰۱۸ — record و value object

[قبلی](../017-solid/README.md) · [خانه](../../../README.md) · [بعدی](../019-separation/README.md)

## خروجی درس

یک TicketTitle معتبر و immutable می‌سازی.
برابری ارزش و برابری reference را جدا می‌کنی.
با with یک DTO را کپی می‌کنی و محدودیت shallow copy را مشاهده می‌کنی.
می‌توانی توضیح بدهی چرا هر record خودکار value object سالم نیست.

## پیش‌نیاز

کلاس، reference، factory، nullable، HashSet و encapsulation.
Factory در این درس یک متد ساخت معتبر است؛ framework یا container جدید نیست.

## مسئله

عنوان در چند مرز به صورت string بررسی می‌شود.
string می‌تواند عنوان، مسیر فایل، ایمیل یا هر متن دیگری باشد.
متدی که string title می‌گیرد از نوع ورودی نمی‌فهمد validation انجام شده است یا نه.
می‌خواهیم مفهوم «عنوان معتبر» را در یک نوع مشخص بیان کنیم.

## ۱. entity و value object

| مثال | هویت یا ارزش؟ |
| --- | --- |
| Ticket با شناسهٔ ۱ | entity با هویت |
| عنوان Network issue | value با متن تعریف‌شده |
| TicketSummary برای انتقال داده | DTO؛ لزوماً domain object نیست |

دو Ticket با عنوان مشابه درخواست یکسان نیستند.
دو عنوان با ارزش یکسان می‌توانند از نظر این قرارداد برابر باشند.
معیار equality یک تصمیم دامنه است، نه انتخاب صرفاً نحوی class یا record.

entity را با «کدام مورد است؟» می‌شناسیم: Ticket شمارهٔ ۱ حتی پس از Rename همان درخواست است. value object را با «چه مقداری دارد؟» مقایسه می‌کنیم: دو TicketTitle با متن یکسانِ پس از Trim، در قرارداد این درس یک ارزش را بیان می‌کنند، حتی اگر جدا ساخته شده باشند. DTO هم فقط شکل انتقال داده است و به‌تنهایی تضمین قواعد کسب‌وکار نمی‌دهد.

record ابزار زبان برای امکاناتی مانند equality و copy است. value object نقش طراحی است: مقدار معتبر، معیار برابری روشن و تغییرناپذیریِ لازم دارد. پس نوشتن record به‌تنهایی این نقش را کامل نمی‌کند.

## ۲. یک record ساده برای DTO

```csharp
public sealed record TicketSummary(int Id, string Title);
```

این declaration یک record class می‌سازد.
کامپایلر equality مبتنی بر اعضا، نمایش ToString و امکانات copy را فراهم می‌کند.
propertyهای positional record class معمولاً init دارند.
این DTO خودش طول عنوان یا مثبت بودن Id را enforce نمی‌کند.
نام record به معنی معتبر شدن همهٔ داده‌های ورودی نیست.

## ۳. == و ReferenceEquals

```csharp
TicketTitle first = TicketTitle.Create("  Network issue  ");
TicketTitle second = TicketTitle.Create("Network issue");
```

first و second دو instance جدا هستند.
پس ReferenceEquals باید false باشد.
بعد از normalization ارزش هر دو برابر است و == برای این record true می‌شود.
record equality با reference equality کلاس معمولی یکسان نیست.

برای first و second دو سؤال متفاوت داریم:

| سؤال | پاسخ نمونه | علت |
| --- | --- | --- |
| آیا متن معتبر نهایی یکی است؟ | True | فاصله‌های دو سر ورودی اول حذف شده‌اند |
| آیا هر دو نام به همان شیء اشاره می‌کنند؟ | False | Create دو بار، دو instance جدا ساخته است |

normalization یعنی تبدیل ورودی به شکل قراردادی؛ اینجا فقط Trim است. validation یعنی بررسی مجازبودن مقدار پس از آن. Create ابتدا شکل قراردادی را می‌سازد، سپس طول را می‌سنجد و فقط در صورت موفقیت شیء معتبر می‌دهد.
نوع runtime نیز در equality record class نقش دارد؛ sealed مدل را در این مثال محدود می‌کند.

## ۴. قرارداد TicketTitle

- مقدار ضروری است و null پذیرفته نمی‌شود.
- ابتدا و انتهای متن Trim می‌شود.
- طول بین ۳ و ۱۲۰ واحد UTF-16 است.
- مقایسهٔ ارزش متنی case-sensitive و طبق برابری string است.
- شکل‌های Unicode ظاهراً مشابه را خودکار normalize نمی‌کنیم.

مثلاً Network issue و network issue برابر نیستند.
دو متن با شکل Unicode متفاوت ممکن است ظاهراً یکسان دیده شوند، اما قرارداد فعلی آن‌ها را یکی نمی‌کند.
اگر محصول normalization لازم دارد، باید سیاست صریح و بررسی سازگاری دادهٔ قبلی داشته باشد.

## ۵. ساخت فقط از مسیر معتبر

```csharp
public sealed record TicketTitle
{
    public string Value { get; }
    private TicketTitle(string value) => Value = value;

    public static TicketTitle Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string normalized = value.Trim();
        if (normalized.Length < 3 || normalized.Length > 120)
            throw new ArgumentException("Title must contain 3-120 characters.", nameof(value));
        return new TicketTitle(normalized);
    }
    public override string ToString() => Value;
}
```

constructor خصوصی است و caller عادی باید Create را استفاده کند.
Value فقط getter دارد؛ setter یا init عمومی برای دور زدن guard وجود ندارد.
ToString یک نمایش متنی فراهم می‌کند؛ برای serialization file همچنان قرارداد جدا می‌سازیم.
به شکل پیام ToString برای API پایدار تکیه نمی‌کنیم.

## ۶. چرا init عمومی برای این value ندادیم؟

اگر Value public init داشته باشد، caller ممکن است با with مقدار نامعتبر تازه بسازد.
with برای تغییر init property از مسیر constructor validation اولیه عبور نمی‌کند.
پس factory به تنهایی همراه init آزاد کافی نیست.
در TicketTitle فعلی with {} می‌تواند copy هم‌ارزش بسازد، اما with { Value = ... } مجاز نیست.
تغییر عنوان یعنی ساخت TicketTitle جدید معتبر و سپس انجام operation مدل.

## ۷. copy برای DTO

```csharp
TicketSummary original = new(1, "Network issue");
TicketSummary changed = original with { Title = "Printer issue" };
```

original تغییر نمی‌کند.
changed همان Id و عنوان تازه دارد.
این nondestructive mutation روی record است.
DTO سادهٔ این مثال guard دامنه ندارد؛ نباید changed را بدون تبدیل معتبر وارد Ticket کنی.
یک record برای نقل داده و یک value object برای invariant نقش‌های متفاوت دارند.

## ۸. shallow immutability

```csharp
public sealed record ReportLines(List<string> Lines);
```

property Lines init است، اما List داخل آن mutable است.
caller هنوز می‌تواند Lines.Add یا Clear را صدا بزند.
record declaration لیست را frozen نمی‌کند.
همان نکتهٔ collection درس ۰۱۳ اینجا هم برقرار است.

## ۹. with یک کپی عمیق نیست

```csharp
ReportLines report = new(new List<string> { "First line" });
ReportLines copy = report with { };
copy.Lines.Add("Shared second line");
```

هر دو record به همان List اشاره می‌کنند.
پس report.Lines.Count نیز ۲ می‌شود.
اگر می‌خواهی ساختار لیست مستقل باشد، باید خود لیست را کپی کنی.
اگر عنصرها هم mutable هستند، دربارهٔ کپی یا نمایش امن آن‌ها تصمیم جدا لازم است.

کلمهٔ shallow یعنی کپیِ سطح بیرونی. با with یک ReportLines تازه ساخته‌ایم، ولی مقدار property آن هنوز reference همان List است:

1. report یک List با یک خط دارد.
2. copy شیء بیرونی دیگری است، اما Lines هر دو به یک List می‌رسد.
3. Add از مسیر copy، همان List مشترک را تغییر می‌دهد.
4. به همین دلیل Count از مسیر report هم ۲ است.

این با مثال TicketSummary فرق دارد: تغییر Title در with، property کپی را به string دیگری وصل می‌کند و original همان عنوان قبلی را دارد. برای خودآزمایی، پیش از اجرا بگو «کدام شیء تازه ساخته شد و کدام شیء هنوز مشترک است؟»؛ صرف دیدن واژهٔ copy جواب این سؤال نیست.

## ۱۰. equality collectionها

دو ReportLines با دو List تازه و متن‌های برابر، خودکار برابر نمی‌شوند.
List equality در این استفاده بر اساس همان reference لیست است، نه مقایسهٔ ترتیبی تمام متن‌ها.
اگر چنین مفهوم دامنه‌ای لازم است، باید equality مناسب تعریف یا نوع دادهٔ مناسب انتخاب شود.
record را بدون بررسی عضوهایش به عنوان کلید semantic استفاده نکن.

## ۱۱. HashSet و ارزش ثابت

```csharp
HashSet<TicketTitle> titles = new() { first, second };
```

با دو عنوان برابر Count برابر ۱ است.
HashSet از equality و hash مناسب record استفاده می‌کند.
اگر عضو مؤثر بر hash پس از درج عوض شود، lookup collection می‌تواند رفتار اشتباه بدهد.
TicketTitle با مقدار ثابت در این زمینه مناسب است.
immutable بودن تمام ارزش‌های مؤثر را برای value objectهای پیچیده‌تر بررسی کن.

## ۱۲. record struct و مقدار پیش‌فرض

record class و record struct یک چیز نیستند.
record struct نوع value است و default می‌تواند بدون اجرای constructor سفارشی ساخته شود.
برای نوعی که نمی‌خواهد حالت default نامعتبر داشته باشد، این موضوع مهم است.
در این درس TicketTitle یک record class است؛ default آن null است و caller باید قرارداد null را رعایت کند.
انتخاب struct صرفاً برای «سریع‌تر بودن» بدون اندازه‌گیری و تحلیل semantics انجام نمی‌شود.

## ۱۳. جای value object در پروژه

| مسیر | مسئولیت |
| --- | --- |
| Domain/TicketTitle.cs | normalization، validation و equality عنوان |
| Program.cs | مثال‌ها و مشاهدهٔ copy |

در نمونهٔ این درس مدل Ticket قبلی را بازنویسی نکرده‌ایم؛ تمرکز روی ارزش است.
درس ۰۱۹ یک مدل ثبت کوچک با TicketTitle می‌سازد.
تبدیل snapshot ذخیره‌شده به value object باید از Create عبور کند.
دادهٔ فایل قدیمی را بدون validation به private constructor برنگردان.

## ۱۴. اجرا

[Program](../../../samples/17-ticket-values/Program.cs)

```powershell
dotnet run --project samples/17-ticket-values/SupportDesk.csproj
```

خروجی‌های مهم:

```text
Same value: True; same reference: False
Case sensitive: False
Unique values: 1
Original: Network issue; copy: Printer issue
Invalid value rejected.
Shallow original count: 2
List content equality automatic: False
```

false آخر نشان می‌دهد برابر بودن محتوا درون دو لیست از record ساده نتیجه نمی‌شود.

## تمرین مستقل

یک ResolutionText با طول ۳ تا ۵۰۰ و Trim بساز.
ساخت مستقیم و تغییر property از بیرون مجاز نباشد.
دو مقدار یکسان باید برابر باشند و HashSet آن‌ها یک عضو نگه دارد.
مرزهای طول، null و فضای خالی را بررسی کن.

## تمرین تکمیلی

به مدل تمرین خودت Rename(TicketTitle title) اضافه کن.
setter عمومی برای Value یا Title نساز.
شرح بده UI raw string را کجا به value تبدیل می‌کند و domain کجا invariant وضعیت را بررسی می‌کند.
اگر error handling با exception فعلاً مناسب نیست، طرح TryCreate را بنویس؛ پیاده‌سازی result type در درس ۰۲۰ بررسی می‌شود.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| طول ۳ و ۱۲۰ | قبول |
| طول ۲ و ۱۲۱ | رد |
| null و فقط فاصله | رد |
| دو متن برابر پس از Trim | value برابر، reference جدا |
| تفاوت حروف بزرگ و کوچک | طبق قرارداد نابرابر |
| with روی DTO | original ثابت |
| with شامل List | reference مشترک قابل مشاهده |

## سرنخ

record syntax را با domain semantics یکی نگیر.
value object باید دلیل business داشته باشد، نه فقط wrapper دور همهٔ primitiveها.
برای عنوان، invariant و استفادهٔ تکرارشونده دلیل ساخت این نوع است.

## معیار عبور

- [ ] entity، DTO و value object را جدا می‌دانم.
- [ ] with را clone عمیق نمی‌دانم.
- [ ] factory با init آزاد را validation کامل نمی‌دانم.
- [ ] equality collection را بررسی کرده‌ام.
- [ ] policy normalization و case sensitivity را مستند کردم.
- [ ] value نامعتبر از مسیر public ساخته نمی‌شود.

## منابع رسمی

- [Record types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records)
- [Record reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Domain model and value objects](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model)

## قدم بعد

مدل ارزش، use case و قرارداد فایل را در یک نمونهٔ ذخیره‌شونده با مرزهای روشن کنار هم قرار می‌دهیم.
