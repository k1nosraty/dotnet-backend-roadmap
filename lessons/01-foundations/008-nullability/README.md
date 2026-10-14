# درس ۰۰۸ — nullability و قرارداد داده

[قبلی](../007-classes/README.md) · [خانه](../../../README.md) · [بعدی](../009-exceptions/README.md)

## خروجی درس

برای دادهٔ اختیاری و نتیجهٔ جست‌وجوی ناموجود قرارداد مشخص می‌نویسی.
هشدار nullable را از خطای runtime تشخیص می‌دهی و بدون ! بی‌دلیل آن را رفع می‌کنی.

## پیش‌نیاز

کلاس Ticket در درس ۰۰۷ و null ورودی ReadLine در درس ۰۰۳.
Nullable در csproj نمونه‌ها enable است.
در نمونه‌های تازه TreatWarningsAsErrors نیز enable شده است.

## مسئله

شناسهٔ ۹۹ ممکن است وجود نداشته باشد.
اگر متد جست‌وجو ادعا کند حتماً Ticket می‌دهد، فراخواننده ممکن است Title را بدون بررسی بخواند.
قرارداد باید احتمال نبودن نتیجه را نشان بدهد.
ناموجود بودن درخواست با خراب بودن storage یکی نیست.

## ۱. null یک مقدار حالت است

برای reference، null یعنی reference به شیء اشاره نمی‌کند.
رشتهٔ خالی یک شیء string معتبر با طول صفر است.
عدد صفر هم با نبود انتخاب یکی نیست.
در قراردادهای داده باید فرق این‌ها روشن باشد.

| داده | قرارداد پیشنهادی |
| --- | --- |
| عنوان ضروری | string و قانون طول |
| نتیجهٔ جست‌وجو | Ticket? |
| شناسهٔ هنوز انتخاب‌نشده | int? |
| لیست بدون درخواست | لیست خالی، اگر عملیات موفق بوده |

## ۲. reference nullable

```csharp
Ticket? missing = null;
```

Ticket? به کامپایلر می‌گوید احتمال null در این reference وجود دارد.
برای referenceها Ticket و Ticket? از نظر نوع runtime همان نوع هستند.
تفاوت در annotation و تحلیل static است.
این ویژگی wrapper مشابه Nullable<int> نمی‌سازد.
همچنین از ورودی JSON یا reflection به صورت خودکار اعتبارسنجی کامل نمی‌کند.

## ۳. بررسی قبل از استفاده

```csharp
if (missing is not null)
{
    Console.WriteLine(missing.Title);
}
```

در شاخهٔ if، تحلیل جریان می‌فهمد missing null نیست.
بررسی باید دربارهٔ همان مقداری باشد که استفاده می‌کنیم.
اگر property mutable در محیط concurrent تغییر کند، annotation به تنهایی race را حل نمی‌کند.
نمونهٔ این درس تک‌نخی است.

## ۴. قرارداد جست‌وجو

```csharp
static Ticket? FindById(Dictionary<int, Ticket> tickets, int id)
{
    return tickets.TryGetValue(id, out Ticket? ticket) ? ticket : null;
}
```

متد ممکن است Ticket یا null بدهد.
ناموجود بودن کلید مسیر عادی است.
Dictionary این نمونه خودش null نگه نمی‌دارد.
اگر در نوع مقدار dictionary هم Ticket? اعلام کنیم، باید فرق کلید ناموجود و کلید با مقدار null را تعریف کنیم.
برای این دامنه چنین حالت دوم را ایجاد نمی‌کنیم.

## ۵. نمایش مقدار اختیاری

```csharp
Ticket? found = FindById(tickets, 99);
Console.WriteLine(found?.Title ?? "Ticket not found");
```

?. دسترسی را در صورت null کوتاه می‌کند.
?? مقدار جایگزین می‌دهد.
این expression تصمیم نمایشی است؛ مدل Ticket نباید عنوان Ticket not found داشته باشد.
ساخت یک Ticket جعلی با شناسهٔ صفر برای نمایش «ناموجود» مدل را مخدوش می‌کند.

## ۶. ! درمان نیست

```csharp
// این نمونه را در نسخهٔ تمرین خودت عمداً امتحان کن:
// Console.WriteLine(found!.Title);
```

! به کامپایلر اعلام می‌کند این احتمال را هشدار ندهد.
در runtime شیء تازه‌ای نمی‌سازد و null را حذف نمی‌کند.
اگر found واقعاً null باشد، دسترسی Title همچنان خطا می‌دهد.
از ! فقط با دلیل اثبات‌شده و محدود استفاده کن؛ برای خاموش کردن هشدارهای نامفهوم نیست.

## ۷. nullable value type

```csharp
int? selectedId = null;
if (selectedId.HasValue)
{
    Console.WriteLine(selectedId.Value);
}
```

int? شکل Nullable<int> است و با int متفاوت است.
Value در حالت بدون مقدار قابل استفاده نیست.
HasValue وجود مقدار را نشان می‌دهد.
برای نمایش کوتاه‌تر:

```csharp
Console.WriteLine(selectedId?.ToString() ?? "None");
```

اگر selectedId صفر باشد، متن ۰ نمایش داده می‌شود؛ null نمایش None می‌دهد.
استفاده از ?? 0 می‌تواند این دو حالت را یکی کند، پس فقط وقتی قانون اجازه می‌دهد انجامش بده.

## ۸. مقدار ضروری در سازنده

Ticket عنوان non-null می‌خواهد و در سازنده guard هم دارد.
چرا هر دو؟
annotation به مصرف‌کنندهٔ کد کمک می‌کند و guard ورودی runtime را بررسی می‌کند.
پکیج قدیمی بدون annotation یا دادهٔ خارجی ممکن است قرارداد static را رعایت نکند.
این دو ابزار مکمل‌اند.

## ۹. warning را بخوان

برای آزمایش، یک پروژهٔ تمرین بساز و این خط را فعال کن:

```csharp
string? input = null;
// Console.WriteLine(input.Length);
```

اگر کامنت خط دوم را برداری، تحلیل nullable باید هشدار dereference بدهد.
در نمونه‌های جدید هشدار به خطای build تبدیل شده است.
اصلاح مناسب، تصمیم دربارهٔ null است:

```csharp
if (input is null)
{
    Console.WriteLine("Input is absent.");
}
else
{
    Console.WriteLine(input.Length);
}
```

## ۱۰. null و خطای زیرساخت

متد FindById می‌تواند null را برای نبود داده بدهد.
اما اگر اتصال دیتابیس خراب شد، برگرداندن null می‌تواند خرابی را شبیه «کاربر وجود ندارد» نشان بدهد.
در این دوره خطای زیرساخت و نتیجهٔ ناموجود را جدا نگه می‌داریم.
نوع دقیق نتیجه در فصل طراحی بررسی می‌شود.

## ۱۱. collection خالی

```csharp
List<Ticket> tickets = new();
```

این لیست وجود دارد و Count آن صفر است.
اگر متد قرارداد List<Ticket> می‌دهد، بهتر است نتیجهٔ موفق بدون داده را لیست خالی بدهد.
List<Ticket>? یعنی خود collection ممکن است غایب باشد.
List<Ticket?> یعنی collection می‌تواند عنصر null داشته باشد.
این دو یک قرارداد نیستند.
در ورودی JSON درس ۰۱۱ این تفاوت را برای بررسی دادهٔ خارجی استفاده می‌کنیم.

## ۱۲. اجرا

[Program](../../../samples/07-ticket-nullability/Program.cs)

```powershell
dotnet run --project samples/07-ticket-nullability/SupportDesk.csproj
```

خروجی:

```text
Found: Network issue
Missing: Ticket not found
Selection: None
```

## تمرین مستقل

در منوی خودت نتیجهٔ جست‌وجو را با Ticket? برگردان.
اگر کاربر شناسهٔ ناموجود وارد کرد، پیام بده و در منو بمان.
شرح اختیاری درخواست را با string? مدل کن.
توضیح بده آیا رشتهٔ خالی شرح را به null تبدیل می‌کنی یا حفظ می‌کنی.
این تصمیم باید در README تمرین نوشته شود.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| شناسهٔ موجود | عنوان واقعی |
| شناسهٔ ناموجود | پیام ناموجود، بدون Ticket جعلی |
| شرح null | پیام شرح ثبت نشده |
| شرح فقط فاصله | رفتار طبق قرارداد مستند تو |
| لیست بدون Ticket | Count صفر؛ خود لیست null نیست |

## تمرین تکمیلی

سه اعلان List<Ticket>، List<Ticket>? و List<Ticket?> بنویس.
برای هر کدام یک مثال مجاز و یک استفادهٔ نیازمند بررسی نشان بده.
در نسخهٔ تمرین، build با هشدارهای nullable را اصلاح کن.

## سرنخ

annotation را مطابق واقعیت انتخاب کن، نه مطابق کمترین تعداد هشدار.
اگر داده ضروری است، صرفاً برای ساکت شدن build آن را nullable نکن.
اگر داده اختیاری است، یک مقدار ساختگی جای آن نگذار.

## معیار عبور

- [ ] Ticket? و int? را از نظر runtime یکی نمی‌دانم.
- [ ] null، empty و صفر را تفکیک می‌کنم.
- [ ] نتیجهٔ ناموجود را از خرابی زیرساخت جدا می‌کنم.
- [ ] بدون ! بی‌دلیل build تمیز دارم.
- [ ] معنای nullable خود لیست و عنصر لیست را توضیح می‌دهم.
- [ ] قرارداد شرح اختیاری را نوشته‌ام.

## منابع رسمی

- [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Nullable value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types)
- [Null-forgiving operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-forgiving)

## قدم بعد

دادهٔ غایب همیشه exception نیست؛ اما برای exception واقعی و منابع باز چه می‌کنیم؟
