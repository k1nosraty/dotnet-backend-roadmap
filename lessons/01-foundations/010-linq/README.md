# درس ۰۱۰ — انتخاب و تبدیل داده‌ها با LINQ

[قبلی](../009-exceptions/README.md) · [خانه](../../../README.md) · [بعدی](../011-files-json/README.md)

## اول بدان قرار است چه کاری انجام بدهیم

فرض کن پنج عدد یا چند Ticket در یک List داری و فقط بعضی از آن‌ها را می‌خواهی.
در این درس ابتدا همین کار را با foreach انجام می‌دهیم، بعد شکل LINQ آن را می‌نویسیم.
هدف اول فهمیدن سه کار است: **انتخاب کردن با Where، تبدیل کردن با Select، مرتب کردن با OrderBy**.
بعد از این سه مورد، زمان اجرای query و ToList را با دو مثال مقایسه می‌کنیم.

پیش‌نیاز: List، foreach، if و propertyهای Ticket. این درس روی داده‌های حافظه است؛ هنوز دیتابیس نداریم.
قسمت‌های ۱ تا ۷ مسیر اصلی‌اند؛ مطالب تکمیلی انتهای درس برای مرور دوم هستند.

## پیش از شروع مثال‌ها

از ریشهٔ repo یک پروژهٔ تمرین بساز:

```powershell
dotnet new console -o exercises/lesson10 --framework net10.0
Set-Location exercises/lesson10
```

اگر از درس ۹ داخل exercises/lesson09 هستی، ابتدا با `Set-Location ../..` به ریشه برگرد.
اگر پروژهٔ lesson10 قبلاً ساخته شده، دوباره نساز.
در هر مثالِ عددی، کد را **جای تمام Program.cs** بگذار و `dotnet run` بزن.
مثال‌های عددی مستقل‌اند؛ کد آن‌ها را زیر هم نچسبان، چون متغیرهای هم‌نام را دوباره تعریف می‌کنند.
نام‌های معمول List و LINQ در پروژهٔ console این SDK با implicit usings در دسترس‌اند.

## ۱. اول با چیزی که بلدیم: foreach و if

می‌خواهیم فقط عددهای بزرگ‌تر از ۳ را چاپ کنیم.

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

foreach (int number in numbers)
{
    if (number > 3)
    {
        Console.WriteLine(number);
    }
}
```

خروجی:

```text
4
5
```


foreach هر عدد را برمی‌دارد؛ if تصمیم می‌گیرد آن عدد چاپ شود یا نه.
در پایان، numbers همچنان پنج عدد اصلی را دارد؛ چیزی از آن حذف نشده است.
همین هدف را در قدم بعد با LINQ می‌نویسیم.

## ۲. Where یعنی کدام عضوها را انتخاب کنیم؟

LINQ مجموعه‌ای از روش‌ها برای کار با داده‌های قابل پیمایش است.
برای شروع، Where را مانند همان if داخل foreach بخوان.

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

var selected = numbers.Where(number => number > 3);

foreach (int number in selected)
{
    Console.WriteLine(number);
}
```

خروجی:

```text
4
5
```


نتیجه همان ۴ و ۵ است. Where عددها را از numbers حذف یا مقدارشان را تغییر نداده است.
selected چیزی است که می‌توانیم با foreach پیمایش کنیم و فقط نتیجه‌های مطابق شرط را بگیریم.

### علامت => را چگونه بخوانیم؟

در `number => number > 3`:

| قسمت | معنی |
| --- | --- |
| number سمت چپ | نام موقتِ عضوی که داریم بررسی می‌کنیم |
| => | ورودی سمت چپ را بده، نتیجهٔ سمت راست را حساب کن |
| number > 3 | شرطی که true یا false می‌دهد |

این یک تابع کوتاه، یا lambda، است: «یک عدد بگیر و بگو از ۳ بزرگ‌تر هست یا نه.»
number اسم خاص و اجباری نیست؛ `x => x > 3` همان معنی را دارد.
نام واضح‌تر را برای یادگیری انتخاب کرده‌ایم.

`var` هم یعنی compiler نوع متغیر را از سمت راست مشخص کند؛ متغیر بی‌نوع یا dynamic نیست.
نوع selected در این مثال IEnumerable<int> است: یعنی قابل پیمایشِ عددها؛ لازم نیست خود آن List باشد.

### یک تغییر کوچک

شرط را به `number < 3` تبدیل کن. قبل از اجرا حدس بزن: این بار ۱ و ۲ چاپ می‌شوند.
سپس شرط را به `number % 2 == 0` تغییر بده تا فقط عددهای زوج را انتخاب کنی.

## ۳. Select یعنی هر عضو را به چه چیزی تبدیل کنیم؟

حالا به جای انتخاب بعضی عددها، می‌خواهیم از **هر عدد یک نتیجهٔ دوبرابر** بسازیم.

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

var doubled = numbers.Select(number => number * 2);

foreach (int number in doubled)
{
    Console.WriteLine(number);
}
```

خروجی:

```text
2
4
6
8
10
```


این بار برای هرکدام از پنج عدد، یک مقدار خروجی داریم. numbers همچنان ۱ تا ۵ است.
Select در این مثال مقدار نتیجه را حساب می‌کند؛ برای عوض کردن عضوهای فهرست اصلی استفاده نشده است.

| متد | سؤال | نمونه |
| --- | --- | --- |
| Where | کدام عضوها بمانند؟ | عددهای بزرگ‌تر از ۳ |
| Select | از هر عضو چه نتیجه‌ای بسازیم؟ | دوبرابر هر عدد |

فعلاً این دو سؤال را از هم جدا نگه دار؛ کوتاه بودن syntax به معنی یکی بودن کارشان نیست.

## ۴. OrderBy یعنی نتیجه با چه ترتیبی بیاید؟

این بار فهرست اولیه مرتب نیست. اول انتخاب می‌کنیم، سپس نتیجه را مرتب می‌کنیم.

```csharp
List<int> numbers = new List<int> { 5, 1, 4, 2, 3 };

var selected = numbers
    .Where(number => number > 3)
    .OrderBy(number => number);

foreach (int number in selected)
{
    Console.WriteLine(number);
}
```

خروجی:

```text
4
5
```


بدون OrderBy، ترتیب نتیجهٔ Where در این مثال ۵ و ۴ بود؛ OrderBy آن را ۴ و ۵ کرده است.
عبارت `number => number` یعنی برای مرتب‌سازی، خود مقدار عدد را معیار قرار بده.
نوشتن متدها در چند خط فقط برای خوانایی است؛ نقطهٔ هر خط، مرحلهٔ بعدی را به نتیجهٔ قبلی وصل می‌کند.

حالا می‌توانی `.Select(number => number * 10)` را بعد از OrderBy اضافه کنی؛ نتیجه باید ۴۰ و ۵۰ شود.
این تمرین را بعد از اجرای مثال اصلی انجام بده، نه قبل از فهمیدن مراحل آن.

## ۵. نکتهٔ مهم: انتخاب چه زمانی اجرا می‌شود؟

در مثال‌های قبلی foreach نتیجه را می‌خواند.
در Where، تعریف کردن query معمولاً همان لحظه یک فهرست آماده از جواب‌ها نمی‌سازد.
مثال زیر را کامل جای Program.cs بگذار:

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

var selected = numbers.Where(number => number > 3);

numbers.Add(6);

Console.WriteLine(selected.Count());
```

خروجی:

```text
3
```


چرا ۳؟ چون selected هنگام Count() روی دادهٔ آن زمان پیمایش شده است.
در آن زمان numbers شامل ۶ هم هست، پس عضوهای بزرگ‌تر از ۳ عبارت‌اند از ۴، ۵ و ۶.
فراخوانی Where را مثل نوشتن دستور انتخاب بخوان؛ foreach یا Count() آن دستور را روی منبع اجرا می‌کند.
به این عقب افتادن اجرا، **deferred execution** می‌گویند.

## ۶. ToList یعنی نتیجهٔ همین لحظه را به فهرست تبدیل کن

این بار فقط یک متد به تعریف selected اضافه می‌کنیم:

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

var selected = numbers.Where(number => number > 3).ToList();

numbers.Add(6);

Console.WriteLine(selected.Count);
```

خروجی:

```text
2
```


ToList قبل از Add اجرا شده است. در آن لحظه فقط ۴ و ۵ انتخاب شده بودند و در فهرست تازه قرار گرفتند.
اضافه شدن ۶ به numbers، آن فهرست تازه را بزرگ نمی‌کند.

| ترتیب اتفاق‌ها | بدون ToList | با ToList قبل از Add |
| --- | --- | --- |
| تعریف انتخاب | دستور انتخاب را نگه می‌داریم | انتخاب اجرا و فهرست ساخته می‌شود |
| اضافه کردن ۶ به numbers | منبع تغییر می‌کند | فقط منبع تغییر می‌کند |
| خواندن تعداد نتیجه | ۳ | ۲ |

در مثال اول `Count()` متد است و نتیجهٔ query را می‌شمارد.
در مثال دوم selected یک List است و `Count` بدون پرانتز، تعداد عضوهای همان فهرست آماده را می‌دهد.
ToList را به همهٔ queryها اضافه نکن؛ وقتی نتیجهٔ آمادهٔ آن لحظه را لازم داری از آن استفاده کن.

### خودت پیش‌بینی کن

اگر Add(6) را **قبل از ToList** ببری، تعداد فهرست تازه هم ۳ می‌شود.
عدد خروجی را حفظ نکن؛ جای ToList و Add را دنبال کن.

## ۷. حالا همین مراحل را روی Ticket انجام بدهیم

برای این مثال، کلاس Ticket را هم به پروژهٔ تمرین اضافه کن؛ از داخل exercises/lesson10:

```powershell
Copy-Item ../../samples/09-ticket-linq/Domain/Ticket.cs ./Ticket.cs
```

سپس کد زیر را جای Program.cs بگذار و اجرا کن. کلاس استفاده‌شده همان [Ticket.cs](../../../samples/09-ticket-linq/Domain/Ticket.cs) نمونه است؛ Program.cs اصلی repo را لازم نیست تغییر بدهی.

```csharp
using SupportDesk.Domain;

List<Ticket> tickets = new List<Ticket>
{
    new Ticket(3, "Email issue"),
    new Ticket(1, "Network issue"),
    new Ticket(2, "Printer issue")
};

tickets[1].Start();
tickets[1].Close();

var lines = tickets
    .Where(ticket => ticket.Status == TicketStatus.Open)
    .OrderBy(ticket => ticket.Id)
    .Select(ticket => "#" + ticket.Id + ": " + ticket.Title);

foreach (string line in lines)
{
    Console.WriteLine(line);
}
```

خروجی:

```text
#2: Printer issue
#3: Email issue
```


`tickets[1]` یعنی دومین عضو فهرست، نه Ticket با Id برابر ۱ به‌عنوان یک قاعدهٔ عمومی.
در این فهرست، دومین عضو اتفاقاً Id برابر ۱ دارد و با Start و Close بسته شده است.
دو Ticket دیگر هنوز Open هستند.

سه مرحلهٔ query را بلند بخوان:

۱. Where: فقط Ticketهای Open را انتخاب کن.
۲. OrderBy: آن‌ها را با Id مرتب کن.
۳. Select: از هرکدام یک خط متن برای چاپ بساز.

ticket سمت چپ lambda این بار یک عدد نیست؛ یک شیء Ticket است و Id و Title و Status دارد.
منطق lambda همان است؛ نوع ورودی از فهرست مشخص می‌شود.

## ۸. اجرای نمونهٔ کامل repo

اگر داخل پوشهٔ تمرین هستی، با `Set-Location ../..` به ریشه برگرد.
کد نمونه: [Program.cs](../../../samples/09-ticket-linq/Program.cs).

```powershell
dotnet run --project samples/09-ticket-linq/SupportDesk.csproj
```

خروجی:

```text
Deferred count: 3
Snapshot count: 2
#2: Printer issue
#3: Email issue
#4: Backup issue
Ticket not found.
Open: 3
Closed: 1
```

نمونهٔ کامل پس از گرفتن snapshot، Ticket شمارهٔ ۴ را اضافه می‌کند؛ به همین دلیل تعدادها ۳ و ۲ هستند.
سه خط میانی همان Where و OrderBy و Select هستند.
جست‌وجوی Id برابر ۹۹ چیزی پیدا نمی‌کند و دو خط آخر تعداد Ticketهای هر وضعیت را گزارش می‌کنند.
برای فهمیدن Where لازم نیست همین حالا همهٔ گزارش‌های انتهایی را بازنویسی کنی؛ توضیحشان در بخش مرور دوم است.

## تمرین‌های کوچک، به همین ترتیب

۱. از عددهای ۱ تا ۵ فقط زوج‌ها را با Where چاپ کن.
۲. از هر عدد یک متن مثل `Number: 1` با Select بساز.
۳. عددهای نامرتب را فیلتر و مرتب کن و خروجی را قبل از اجرا بنویس.
۴. جای Add و ToList را عوض کن و تغییر تعداد را توضیح بده.
۵. در نمونهٔ Ticket، فقط Ticketهای Closed را انتخاب کن و ببین چه چیزی چاپ می‌شود.

<details>
<summary>مرور دوم: ToList از objectها کپی عمیق نمی‌سازد</summary>

در مثال عددی، فهرست تازه تعداد مستقل دارد. اما وقتی عضوها شیء Ticket هستند، ToList فهرست تازه‌ای از reference همان شیءها می‌سازد.
این مثال به کلاس Ticket نمونه نیاز دارد:

```csharp
using SupportDesk.Domain;

List<Ticket> tickets = new List<Ticket>
{
    new Ticket(1, "Network issue")
};

var copiedList = tickets.ToList();
var savedTitles = tickets.Select(ticket => ticket.Title).ToList();

tickets[0].Rename("Renamed issue");

Console.WriteLine(copiedList[0].Title);
Console.WriteLine(savedTitles[0]);
```

خروجی:

```text
Renamed issue
Network issue
```


copiedList و tickets دو فهرست‌اند، اما عضو اولشان همان Ticket است؛ Rename از هر دو دیده می‌شود.
savedTitles مقدار رشته‌های عنوان را پیش از Rename گرفته است؛ تغییر property عنوان Ticket، آن فهرست رشته‌ها را بازنویسی نمی‌کند.
پس ثابت ماندن تعداد snapshot را با مستقل بودن همهٔ objectهای داخل آن یکی ندان.

</details>

<details>
<summary>مرور دوم: جست‌وجو، شمارش، گروه‌بندی و محدودیت‌ها</summary>

- `FirstOrDefault(ticket => ticket.Id == 99)` اولین مورد مطابق شرط را می‌دهد. برای Ticket که نوع مرجع است، اگر پیدا نشود نتیجه null است؛ پیش از استفاده بررسی‌اش کن.
- `SingleOrDefault` علاوه بر جست‌وجو، انتظار حداکثر یک match دارد؛ با چند match خطا می‌دهد. این انتخاب جای جلوگیری از ثبت شناسهٔ تکراری را نمی‌گیرد.
- `Any` می‌پرسد «آیا حداقل یکی هست؟» و می‌تواند زودتر متوقف شود. `Count` با شرط باید تعداد همهٔ موارد مطابق را به دست بیاورد. همهٔ LINQها deferred نیستند؛ نتیجهٔ عددی Count در همان فراخوانی محاسبه می‌شود.
- `GroupBy(ticket => ticket.Status)` Ticketهای هم‌وضعیت را دسته‌بندی می‌کند. group.Key نام وضعیت گروه است و group.Count() تعداد آن گروه را می‌دهد. وضعیت بدون عضو، گروه خودکار با تعداد صفر ندارد.
- برای گزارش شامل صفرها، روی `Enum.GetValues<TicketStatus>()` حرکت کن و تعداد هر وضعیت را حساب کن.
- `ThenBy` معیار مرتب‌سازی دوم است؛ مثلاً اول Title و در تساوی عنوان، Id. اگر StringComparer.Ordinal را برای متن انتخاب کردی، آن را ترتیب زبانی فارسی معرفی نکن.
- Where معمولاً عضوها را هنگام پیمایش یکی‌یکی بررسی می‌کند؛ OrderBy برای مرتب‌سازی باید دادهٔ لازم را پردازش کند. deferred بودن الزاماً به معنی streaming ساده نیست.
- Count() و سپس foreach روی یک query deferred ممکن است دو پیمایش ایجاد کنند. اگر واقعاً یک snapshot آماده لازم داری، آگاهانه ToList بگیر.
- هنگام foreach روی query مبتنی بر List، ساختار منبع را با Add یا Remove تغییر نده؛ ممکن است پیمایش خطا بدهد. تغییر مثال‌های این درس قبل از پیمایش انجام می‌شود.
- از Select برای تغییر Status همراه اثر جانبی استفاده نکن؛ عملیات تغییر مدل را صریح انجام بده.
- IEnumerable این درس روی objectهای حافظه کار می‌کند. IQueryable می‌تواند query را برای provider مثل EF Core توصیف کند؛ بحث SQL و هزینهٔ ToList روی دیتابیس را در بخش data یاد می‌گیریم.

### تمرین تکمیلی پس از فهم مسیر اصلی

گزارش همهٔ وضعیت‌ها، حتی تعداد صفر، و ترتیب Title سپس Id را بساز.
بعد با OrderBy و Skip و Take صفحهٔ دوم پنج‌عضوی را نمایش بده.
page را از ۱ تا ۱۰۰۰ و pageSize را مثبت و محدود بگیر؛ ورودی نامعتبر را پیش از محاسبهٔ offset رد کن تا وارد مشکل overflow و صفحه‌بندی نامعتبر نشوی.
این تمرین‌ها برای مرور دوم‌اند، نه شرط فهمیدن اولین مثال Where.

</details>

## برای رفتن به درس بعد، این‌ها کافی است

- [ ] lambda را به‌صورت «ورودی بگیر، نتیجه بده» می‌خوانم.
- [ ] می‌توانم تفاوت Where، Select و OrderBy را با یک مثال توضیح بدهم.
- [ ] می‌دانم در دو مثال Add و ToList چرا یک تعداد ۳ و دیگری ۲ شد.
- [ ] سه مرحلهٔ گزارش Ticketها را جدا دنبال می‌کنم.

## منابع رسمی برای مراجعه

- [Introduction to LINQ queries](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries)
- [Standard query operators](https://learn.microsoft.com/en-us/dotnet/csharp/linq/standard-query-operators/)
- [Lambda expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)

## قدم بعد

در درس ۱۱، فهرست Ticketها را در فایل JSON ذخیره و در اجرای بعد بازیابی می‌کنیم.
