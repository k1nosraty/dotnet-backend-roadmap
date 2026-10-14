# درس ۰۱۳ — کپسوله‌سازی و حفظ قواعد مدل

[قبلی](../../01-foundations/012-async/README.md) · [خانه](../../../README.md)

## خروجی درس

مدلی می‌سازی که caller نتواند با setter یا collection عمومی قواعدش را دور بزند.
برای هر تغییر، دادهٔ نامعتبر را پیش از mutation رد می‌کنی.
می‌توانی مرز قواعد خود Ticket و مسئولیت application یا storage را توضیح بدهی.
این اولین درس بخش دوم است؛ بخش اول کامل باید پیش از آن انجام شده باشد.

## پیش‌نیاز و واژگان

درس‌های ۰۰۷ تا ۰۱۲، به‌ویژه property، reference، exception و LINQ.

| اصطلاح | معنی در این درس |
| --- | --- |
| encapsulation | پنهان کردن state و کنترل مسیر تغییر آن |
| invariant | قاعده‌ای که در همهٔ حالت‌های قابل مشاهدهٔ مدل باید برقرار باشد |
| mutation | تغییر state موجود |
| read-only view | نمایی که API آن مسیر نوشتن ندارد؛ الزاماً snapshot نیست |
| defensive copy | کپی برای جلوگیری از تغییر مستقیم ساختار اصلی توسط caller |

## ۱. نیازمندی جدید و صریح

مدل پایهٔ درس ۰۰۷ را با این قوانین تکمیل می‌کنیم:

- عنوان Trim شده بین ۳ و ۱۲۰ واحد UTF-16 است.
- هر یادداشت Trim شده بین ۳ و ۵۰۰ واحد UTF-16 است.
- هر Ticket حداکثر پنج یادداشت دارد.
- یادداشت یکسان پس از Trim، با مقایسهٔ Ordinal، تکراری است.
- Close فقط از InProgress و با حداقل یک یادداشت مجاز است.
- Ticket بسته قابل تغییر عنوان یا افزودن یادداشت نیست.

این قوانین انتخاب آموزشی این نسخه‌اند، نه قانون عمومی همهٔ سیستم‌های پشتیبانی.
در این مدل هر یادداشت را یادداشت حل مسئله فرض می‌کنیم.
در محصول واقعی ممکن است یادداشت داخلی، پاسخ مشتری و resolution نوع‌های متفاوتی داشته باشند.

نمونهٔ مستقل این درس قواعد تازه دارد؛ کد نمونه‌های قدیمی را تغییر نمی‌دهیم تا تفاوت نسخه‌ها قابل مقایسه بماند.

## ۲. چرا private set کافی نیست؟

این کلاس ظاهراً setter وضعیت را محدود کرده، اما راه دور زدن دارد:

```csharp
public class UnsafeTicket
{
    public TicketStatus Status { get; private set; }
    public List<string> Notes { get; } = new();
}
```

caller می‌تواند Notes.Clear یا Notes.Add را صدا بزند.
نداشتن setter فقط جلوی جایگزینی property را می‌گیرد؛ جلوی تغییر شیء پشت آن را نمی‌گیرد.
این همان reference و alias درس ۰۰۷ است.
اگر بستن درخواست به وجود یادداشت وابسته باشد، پاک کردن مستقیم یادداشت می‌تواند invariant را بشکند.

## ۳. مالک state

مدل مرجع یک لیست خصوصی نگه می‌دارد:

```csharp
private readonly List<string> notes = new();
private readonly IReadOnlyList<string> notesView;
public IReadOnlyList<string> Notes => notesView;
```

readonly یعنی reference field بعد از مقداردهی مجاز جایگزین نمی‌شود.
معنی آن immutable شدن محتوای List نیست.
خود Ticket هنوز می‌تواند notes.Add را انجام دهد.
این اختیار برای متدهای مدل است، نه برای caller.

## ۴. چرا صرفاً IReadOnlyList کافی نیست؟

اگر همین List خصوصی را با نوع IReadOnlyList برگردانی، caller ممکن است آن را دوباره به List cast کند.
نوع محدودتر property به تنهایی implementation mutable را تغییر نمی‌دهد.
پس در constructor یک wrapper می‌سازیم:

```csharp
notesView = notes.AsReadOnly();
```

AsReadOnly یک نمای read-only از لیست می‌دهد.
این شیء همان List اصلی نیست و متدهای نوشتن آن از مسیر collection interface نیز قابل استفاده نیستند.
تلاش Add از آن مسیر در نمونه NotSupportedException می‌دهد.
این مثال cast برای بررسی مرز است؛ در application عادی قرار نیست از چنین تلاشی استفاده کنیم.

IReadOnlyList یک interface استاندارد است؛ طراحی interfaceهای خودمان در درس ۰۱۵ می‌آید.
فعلاً معنی قابل خواندن بودن Count و index را لازم داریم.

## ۵. view یا snapshot؟

نمای AsReadOnly دادهٔ زندهٔ لیست پشت خود را می‌بیند.
اگر یک reference به Notes نگه داری و سپس Ticket.AddNote صدا زده شود، Count همان view هم زیاد می‌شود.
این رفتار در قرارداد فعلی عمدی است.
اگر caller به snapshot زمانی نیاز دارد، Notes.ToList یک فهرست مستقل می‌سازد.

```csharp
List<string> copy = ticket.Notes.ToList();
copy.Add("Only in the copy");
```

افزودن به copy لیست اصلی را تغییر نمی‌دهد.
عنصرها در این مدل string و immutable هستند.
اگر عنصرها کلاس mutable بودند، کپی List به تنهایی از تغییر خود عنصرها جلوگیری نمی‌کرد.
کپسوله‌سازی collection را با کپسوله‌سازی عنصرهایش جدا بررسی کن.

## ۶. رفتار معنی‌دار به جای SetEverything

متدهای مدل Rename، AddNote، Start و Close هستند.
هر متد هدف کسب‌وکار مشخص دارد.
متدی مانند Update(string title, int status, List<string> notes) می‌تواند caller را مسئول هماهنگی تمام قواعد کند.
وجود متد عمومی به تنهایی encapsulation مناسب نیست؛ باید مسیرهای قانونی را کنترل کند.
از طرف دیگر، مدل را با متدهای بی‌دلیل پر نمی‌کنیم؛ عملیات باید نیاز واقعی داشته باشد.

## ۷. بررسی قبل از تغییر

```csharp
public void Rename(string title)
{
    EnsureEditable();
    string normalized = NormalizeText(title, 3, 120, nameof(title));
    Title = normalized;
}
```

ابتدا تغییرپذیر بودن وضعیت بررسی می‌شود.
سپس ورودی آماده و validate می‌شود.
فقط بعد از موفقیت guardها، state عوض می‌شود.
اگر عنوان کوتاه باشد، Title قبلی باقی می‌ماند.
این تضمین دربارهٔ این متد و این state است؛ transaction دیتابیس محسوب نمی‌شود.

## ۸. افزودن یادداشت

```csharp
public void AddNote(string text)
{
    EnsureEditable();
    string normalized = NormalizeText(text, 3, 500, nameof(text));
    if (notes.Count >= 5)
        throw new InvalidOperationException("A ticket can contain at most five notes.");
    if (notes.Contains(normalized, StringComparer.Ordinal))
        throw new InvalidOperationException("Duplicate notes are not allowed.");
    notes.Add(normalized);
}
```

Trim قبل از مقایسه انجام می‌شود.
Contains با comparer در این نمونه از امکانات LINQ است.
Ordinal مقایسهٔ متن مشخصی می‌دهد و با case-insensitive یا ترتیب زبانی فارسی یکی نیست.
برای مثال Note و note طبق این قرارداد متفاوت‌اند.
محدودیت پنج یادداشت و duplicate دو دلیل مستقل برای رد عملیات‌اند.

ترتیب guardها تعیین می‌کند اگر چند قاعده هم‌زمان نقض شدند، کدام خطا گزارش شود.
در این نسخه وضعیت، فرمت، ظرفیت و سپس تکرار بررسی می‌شوند.
به ترتیب پیام‌ها به عنوان قرارداد عمومی API تکیه نمی‌کنیم؛ هنوز error code دامنه تعریف نکرده‌ایم.

## ۹. قاعدهٔ بین چند عضو

```csharp
public void Close()
{
    if (Status != TicketStatus.InProgress)
        throw new InvalidOperationException("Only in-progress tickets can be closed.");
    if (notes.Count == 0)
        throw new InvalidOperationException("A resolution note is required before closing.");
    Status = TicketStatus.Closed;
}
```

قاعدهٔ Closed فقط مربوط به property Status نیست؛ به collection یادداشت هم وابسته است.
validation هر setter به صورت مستقل ممکن است این رابطه را تضمین نکند.
پس operation یک نقطهٔ هماهنگ برای تغییر مدل می‌شود.
درخواست Closed بدون یادداشت هیچ حالت قانونی در این نسخه نیست.

## ۱۰. validation در چند مرز

UI می‌تواند برای پیام دوستانه عنوان را زود بررسی کند.
مدل هم باید قانون خودش را حفظ کند، چون caller فقط UI کنسولی نیست.
فایل، worker یا endpoint دیگری ممکن است مدل را فراخوانی کند.
تکرار کنترل در دو مرز همیشه اشتباه نیست؛ هدف مرزها فرق دارد.
در مراحل بعد راه جلوگیری از پراکندگی تعریف یک قانون را بررسی می‌کنیم.

## ۱۱. چه چیزی داخل Ticket نیست؟

| قاعده یا عملیات | محل اولیهٔ مناسب |
| --- | --- |
| طول عنوان و مسیر وضعیت | مدل Ticket |
| کاربر اجازهٔ بستن دارد؟ | application و authorization |
| Id بین همهٔ رکوردها یکتا است؟ | مخزن یا دیتابیس و هماهنگی ثبت |
| فایل JSON کجا نوشته شود؟ | storage |
| خطا به چه پاسخ HTTP تبدیل شود؟ | مرز وب |

مدل Ticket بدون دیدن بقیهٔ رکوردها نمی‌تواند یکتایی کل سیستم را اثبات کند.
private بودن اعضا هم احراز هویت یا امنیت کامل نیست.
encapsulation یک مرز طراحی برای استفادهٔ درست از مدل است؛ در برابر اجرای کد دلخواه مخرب داخل همان process ادعای sandbox ندارد.

## ۱۲. چیدمان فایل‌ها

| فایل | مسئولیت |
| --- | --- |
| Domain/Ticket.cs | state و قواعد یک درخواست |
| Program.cs | نمایش سناریو و بررسی رفتار |
| SupportDesk.csproj | target framework و بررسی warningها |

EnsureEditable جزئیات خصوصی مدل است؛ helper عمومی به نام TicketUtils نمی‌سازیم.
هنوز نیازی به repository یا dependency injection در این نمونهٔ خالص نیست.
در درس‌های بعدی مرزهای بزرگ‌تر را به اندازهٔ نیاز اضافه می‌کنیم.

## ۱۳. رابطه با ذخیرهٔ JSON قبلی

قرارداد فایل درس ۰۱۱ یادداشت ذخیره نمی‌کند.
اگر مدل جدید را مستقیماً در آن sample جایگزین کنی، بازیابی رکورد Closed ممکن است قاعدهٔ جدید را نقض کند.
مهاجرت schema و سیاست دادهٔ قدیمی لازم است؛ صرفاً setter عمومی نکن تا Load کار کند.
برای دادهٔ قدیمی بدون resolution باید تصمیم محصول بگیری: رد نسخه، مهاجرت با دادهٔ معتبر، یا مسیر recovery مستند.
یادداشت ساختگی «رفع شد» بدون evidence تولید نکن.
این درس sample مستقل دارد و نسخهٔ فایل قبلی را بی‌صدا تغییر نمی‌دهد.

## ۱۴. اجرا

[مدل](../../../samples/12-ticket-encapsulation/Domain/Ticket.cs) · [Program](../../../samples/12-ticket-encapsulation/Program.cs)

```powershell
dotnet run --project samples/12-ticket-encapsulation/SupportDesk.csproj
```

سناریو به ترتیب این رفتارها را نشان می‌دهد:

1. Rename نامعتبر عنوان اصلی را نگه می‌دارد.
2. یادداشت نامعتبر تعداد را صفر نگه می‌دارد.
3. Close زودهنگام و Close بدون یادداشت رد می‌شوند.
4. AddNote معتبر در view زنده دیده می‌شود.
5. تغییر copy روی notes اصلی اثر ندارد.
6. wrapper مسیر نوشتن بیرونی را رد می‌کند.
7. پس از Close، Rename و AddNote رد می‌شوند.

خروجی آخر:

```text
Final: Closed; notes: 1; title: Network issue
```

## تمرین مستقل

مدل نمونه را در پروژهٔ تمرین خودت بازسازی کن.
متد RemoveNote(int index) اضافه کن که فقط پیش از Closed مجاز باشد.
index باید در محدوده باشد و شکست عملیات هیچ داده‌ای حذف نکند.
بعد از حذف آخرین یادداشت، Close باید تا افزودن یادداشت جدید رد شود.
خود collection را برای آسان شدن حذف عمومی نکن.

## تمرین تکمیلی

برای ویرایش یادداشت، ReplaceNote(int index, string text) بساز.
فرمت، ظرفیت و duplicate را مطابق قرارداد حفظ کن؛ جایگزینی نباید به خاطر ظرفیت پنج‌تایی رد شود.
در بررسی duplicate، عنصر همان index را از مقایسه کنار بگذار.
جایگزینی متن با همان متن Trim شده را بدون تغییر یا خطای duplicate قبول کن.
سیاست no-op را در README توضیح بده.

## سناریوهای پذیرش

| سناریو | انتظار |
| --- | --- |
| یادداشت با طول ۳ یا ۵۰۰ | قبول |
| یادداشت با طول ۲ یا ۵۰۱ | رد؛ Count ثابت |
| ششمین یادداشت | رد؛ پنج قبلی ثابت |
| تکرار متن با فاصلهٔ بیرونی | رد؛ Count ثابت |
| Close بدون یادداشت | InProgress باقی بماند |
| Rename یا حذف پس از Closed | رد؛ state ثابت |
| index نامعتبر در RemoveNote | حذف انجام نشود |

## سرنخ

قبل از mutation، مقدارهای لازم را در متغیر محلی آماده و بررسی کن.
برای collection، نوع property و شیء implementation پشت آن را هر دو بررسی کن.
برای آزمون شکست، هم exception و هم state بعد از آن را بررسی کن.

## معیار عبور

- [ ] readonly field را immutable collection نمی‌دانم.
- [ ] caller نمی‌تواند لیست داخلی را مستقیم تغییر بدهد.
- [ ] view زنده و snapshot را تفکیک می‌کنم.
- [ ] قاعدهٔ بین Status و Notes در operation حفظ می‌شود.
- [ ] شکست هر عملیات state قبلی را حفظ می‌کند.
- [ ] یکتایی سراسری و authorization را به Ticket تحمیل نکرده‌ام.
- [ ] تغییر مدل را با سازگاری فایل قدیمی یکی فرض نمی‌کنم.

## منابع رسمی

- [Object-oriented programming](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/)
- [Access modifiers](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers)
- [List<T>.AsReadOnly](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.asreadonly?view=net-10.0)
- [Readonly fields](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly)

## قدم بعد

درس ۰۱۴ به composition و inheritance اختصاص دارد و هنوز نوشته نشده است.
پیش از آن تمرین RemoveNote را با سناریوهای شکست کامل کن.
