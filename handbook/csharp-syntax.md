# مرجع syntaxهای C# در این مسیر

این راهنما همراه درس‌هاست، نه پیش‌نیاز حفظی جدید.
اگر یک قطعهٔ کوتاه فشرده به نظر می‌رسد، شکل بازشدهٔ همان مفهوم را بخوان و بعد به کد اصلی برگرد.

| syntax | نخستین جای مناسب برای مطالعه |
| --- | --- |
| generic collection و new() | درس ۰۰۶ |
| namespace، property و enum | درس ۰۰۷ |
| lambda و IEnumerable | درس ۰۱۰ |
| Task و event handler لغو | درس ۰۱۲ |
| record و with | درس ۰۱۸ |
| generic type و constraint | درس ۰۲۰ |
| type pattern و switch expression | درس ۰۲۱ |
| primary constructor | درس ۰۲۶ |
| extension method | درس ۰۲۷ |
| collection expression و callback mutation | تحویل وب ۰۳۴ |

## ۱. generic: نوع به جای یک نوع ثابت

```csharp
List<string> titles = new();
List<int> ids = new();
```

List<T> یک قرارداد generic است و T جای نوع عنصر قرار می‌گیرد.
List<string> و List<int> نوع یکسانی نیستند و عضو دلخواهِ بی‌نوع قبول نمی‌کنند.
new() وقتی type سمت چپ معلوم است همان ساختن نوع مشخص را کوتاه می‌کند.
`var titles = new();` بدون نوع هدف کافی، همین معنی را ندارد و کامپایل نمی‌شود.
در Outcome<T>، عبارت `where T : class` نوع‌های مرجع را می‌پذیرد؛ Outcome<int> این قرارداد را ندارد.

## ۲. lambda و delegate

```csharp
Func<int, bool> isPositive = number => number > 0;
bool accepted = isPositive(3);
Action<string> print = text => Console.WriteLine(text);
print("Network issue");
```

Func<int, bool> تابعی با یک ورودی int و خروجی bool است؛ آخرین type آرگومان Func، نوع نتیجه است.
Action<string> ورودی string دارد و نتیجه‌ای برنمی‌گرداند.
delegate یک مقدار قابل فراخوانی با signature مشخص است؛ خودِ نتیجهٔ اجرای تابع نیست.
lambda می‌تواند از متغیر بیرونی capture کند؛ static lambda چنین captureای ندارد.
برای mutation در store نهایی، `Func<Ticket, Ticket?>` مدل جاری را می‌گیرد و مدل تازه یا null برای رد قاعده می‌دهد.
callback باید کوتاه و بدون I/O باشد، چون زیر lock اجرا می‌شود.

## ۳. دو کاربرد متفاوت =>

```csharp
static bool IsPositive(int number) => number > 0;
Func<int, bool> check = number => number > 0;
```

اولی بدنهٔ expression-bodied یک متد نام‌دار است.
دومی lambda و یک مقدار تابعی برای delegate است.
هر دو مقدار bool محاسبه می‌کنند، ولی نقش declaration آن‌ها یکسان نیست.

## ۴. primary constructor کلاس

```csharp
public sealed class CounterService(CounterStore store)
{
    public int Count() => store.Count;
}
```

پارامتر store ورودی constructor است و برای متد instance قابل استفاده است.
در یک class عادی، این syntax خودکار property عمومی به نام Store نمی‌سازد.
نام typeهای Counter در این مثال آموزشی است؛ باید تعریف شوند تا قطعه به تنهایی کامپایل شود.
شکل بازتر همان ایده:

```csharp
public sealed class CounterService
{
    private readonly CounterStore store;
    public CounterService(CounterStore store) => this.store = store;
    public int Count() => store.Count;
}
```

این دو declaration جایگزین هم‌اند و نباید هم‌زمان با یک نام در یک namespace تعریف شوند.
DI به constructor وابسته است؛ syntax کوتاه به تنهایی ثبت service یا validation وابستگی انجام نمی‌دهد.
primary constructor یک positional record با تولید propertyهای آن را با primary constructor class یکی نگیر.

## ۵. extension method

```csharp
public static class TextRules
{
    public static bool HasContent(this string? text) => !string.IsNullOrWhiteSpace(text);
}
```

کلاس و متد static هستند؛ this اولین پارامتر، شکل فراخوانی عضوگونه را ممکن می‌کند.
`title.HasContent()` همان فراخوانی static `TextRules.HasContent(title)` را به کار می‌گیرد.
نوع اصلی تغییر نکرده و دسترسی private آن شکسته نشده است.
namespace extension باید در scope باشد.
در دوره از syntax this استفاده می‌کنیم؛ دانستن extension blockهای جدید C# برای اجرای این نمونه‌ها لازم نیست.

## ۶. collection expression

```csharp
string[] empty = [];
List<string> notes = ["First note", "Second note"];
```

نوع هدف تعیین می‌کند collection expression به چه نوعی تبدیل شود.
`[]` لزوماً همیشه List نیست؛ در خط اول آرایه است.
`var empty = [];` نوع هدف مشخصی ندارد و قابل استفادهٔ این شکل نیست.
در شرط صفحه‌بندی، نوع نتیجه صریح نوشته شده: `TicketResponse[] items = ...`.
`[]` با `{ ... }` بدنهٔ کد و با indexer خواندن عضو در `notes[0]` یک نقش ندارد.

## ۷. pattern را به جای parsing اشتباه نگیر

```csharp
if (status is "Open" or "InProgress") { /* یک نام مجاز */ }
```

این pattern نام‌های مشخص را مقایسه می‌کند؛ متن دلخواه را به enum تبدیل نمی‌کند.
`value is not null` بررسی null است.
`AddTicket command => ...` در switch expression یک type pattern و معرفی متغیر همان branch است.
برای هر ورودی ناشناخته، branch روشن یا failure لازم است؛ default را بی‌دلیل success نکن.

## ۸. event و اتصال handler

در نمونهٔ Ctrl+C، `Console.CancelKeyPress += handler` تابع را مشترک event می‌کند.
وقتی event رخ دهد framework آن تابع را فراخوانی می‌کند؛ `+=` اینجا افزایش عدد نیست.
`Console.CancelKeyPress -= handler` در finally اشتراک همان delegate را حذف می‌کند.
handler فقط درخواست لغو می‌دهد و فایل را مستقیم ذخیره نمی‌کند.
نگه داشتن اشتراک event بلندعمر می‌تواند عمر referenceها را طولانی کند؛ مالک اتصال باید مسیر جدا شدن را هم بداند.

## ۹. ValueTask در قرارداد framework

IExceptionHandler از ValueTask<bool> استفاده می‌کند؛ مقدار bool می‌گوید exception مدیریت شده است یا نه.
این نوع مانند Task<bool> نمایندهٔ تکمیل عملیات است و نتیجهٔ آماده یا مسیر asynchronous را می‌تواند پوشش دهد.
در پیاده‌سازی نمونه همان قرارداد framework را نگه می‌داریم و با await کار می‌کنیم.
برای متدهای عادی application بدون نیاز و اندازه‌گیری، Task را خودکار با ValueTask جایگزین نکن.
در مصرف معمول ValueTask را یک بار await کن و آن را مانند Task قابل ذخیره و await تکراری فرض نکن.

## ۱۰. lock و state مشترک

`lock (gate)` یک critical section می‌سازد؛ callerهایی که همان gate را قفل می‌کنند هم‌زمان وارد آن بخش نمی‌شوند.
قفل کردن روی دو object متفاوت هماهنگی ایجاد نمی‌کند.
در store، lookup و بررسی ظرفیت و جایگزینی زیر یک قفل‌اند تا check و mutation از هم جدا نشوند.
داخل lock معمول C# نمی‌توان await نوشت؛ I/O طولانی را داخل این callback کوتاه نمی‌گذاریم.
این قفل بین processها یا سرورهای متفاوت مشترک نیست.

## تمرین کوتاه

یک lambda فیلتر LINQ را به متد نام‌دار تبدیل کن و خروجی را مقایسه کن.
mapping extension را یک بار به شکل static صدا بزن.
یک collection expression را به new List<string>() بازنویسی کن.
برای هر تغییر توضیح بده syntax عوض شده یا قرارداد رفتار هم تغییر کرده است.

## منابع رسمی

- [Generic types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics)
- [Func delegates](https://learn.microsoft.com/en-us/dotnet/api/system.func-2)
- [Primary constructors](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/primary-constructors)
- [Extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)
- [Collection expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions)

- [ValueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-10.0)
- [Lock](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)
