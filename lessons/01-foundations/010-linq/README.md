# درس ۰۱۰ — LINQ و زمان اجرای query

[قبلی](../009-exceptions/README.md) · [خانه](../../../README.md) · [بعدی](../011-files-json/README.md)

## خروجی درس

درخواست‌های باز را فیلتر، مرتب و به متن گزارش تبدیل می‌کنی.
تفاوت query تعریف‌شده، enumeration و snapshot را با تغییر داده می‌بینی.
برای queryهایی که دوباره اجرا می‌شوند، هزینه و رفتار را توضیح می‌دهی.

## پیش‌نیاز

Ticket، List و foreach و nullable.
این درس LINQ to Objects است؛ هنوز query دیتابیس یا EF Core نداریم.

## دادهٔ ورودی مثال‌ها

قطعه‌ها از [نمونهٔ همین درس](../../../samples/09-ticket-linq/Program.cs) هستند؛ برای آزمایش، پوشهٔ همین نمونه را در exercises کپی کن. کلاس‌ها در [Domain](../../../samples/09-ticket-linq/Domain/Ticket.cs) قرار دارند. مدل نسخه‌های بعدی را جای این مدل نگذار: قواعد آن‌ها عمداً تغییر می‌کنند.

Program با `using SupportDesk.Domain;` شروع می‌شود. دادهٔ اولیه، پیش از queryها، این است:

```csharp
List<Ticket> tickets = new()
{
    new Ticket(3, "Email issue"),
    new Ticket(1, "Network issue"),
    new Ticket(2, "Printer issue")
};
tickets[1].Start();
tickets[1].Close();
```

`[1]` یعنی عنصر دوم List، یعنی تیکت با Id برابر ۱؛ شمارهٔ index همان شناسه نیست. اکنون Idهای ۲ و ۳ باز و Id برابر ۱ بسته است. تعریف‌های query را از بخش‌های بعد دنبال کن؛ lambda بخش ۲ به‌تنهایی یک statement کامل برای Program نیست.

## ۱. مسئله

می‌خواهیم درخواست‌های Open را به ترتیب شناسه ببینیم.
می‌توان با foreach و if این کار را کرد، اما ترکیب filter و sort و projection با LINQ خواناتر می‌شود.
LINQ خودکار سریع‌ترین راه نیست؛ رفتار query را باید بشناسیم.

LINQ مخفف Language Integrated Query است؛ در این درس یعنی استفاده از متدهای C# برای پرسیدن سؤال از یک مجموعهٔ درون حافظه. query همین درخواست توصیف‌شدهٔ ماست: «از بین tickets، بازها را پیدا کن، مرتب کن و متن گزارش بده.» قرار نیست اکنون SQL یا زبان جداگانه‌ای یاد بگیری.

| واژه | سؤال سادهٔ آن در مثال |
| --- | --- |
| filter / Where | کدام Ticketها بمانند؟ |
| sort / OrderBy | با چه ترتیبی دیده شوند؟ |
| projection / Select | از هر Ticket چه چیزی تحویل بدهیم؟ |
| enumeration | چه زمانی عنصرها را یکی‌یکی مصرف کنیم؟ |
| materialization / ToList | چه زمانی نتیجه را در یک List تازه نگه داریم؟ |

در بخش‌های بعد این‌ها را جدا می‌بینی؛ لازم نیست زنجیرهٔ کامل را از همین حالا حفظ کنی.

## ۲. lambda

```csharp
t => t.Status == TicketStatus.Open
```

t پارامتر ورودی تابع کوتاه است.
عبارت سمت راست نتیجهٔ bool می‌دهد.
نوع t از collection مشخص می‌شود.
برای شروع این را مثل یک تابع بررسی وضعیت بخوان.
این expression در Where نقش predicate دارد.

آن را این‌طور بخوان: «یک Ticket بگیر و بگو آیا وضعیتش Open است؟» علامت `=>` ورودی سمت چپ را به محاسبهٔ سمت راست وصل می‌کند؛ مقایسه یا تغییر وضعیت نیست. `==` در سمت راست مقایسه است و نتیجه‌اش true یا false می‌شود.

نام t اجباری نیست؛ اگر آن را به ticket تغییر بدهی، باید استفادهٔ سمت راست را هم تغییر بدهی. predicate فقط نام یک تابعِ شرطی با نتیجهٔ bool است؛ چیز جداگانه‌ای برای حفظ‌کردن نیست. Where بعداً این شرط را برای Ticketهای منبع به کار می‌برد.

## ۳. Where

```csharp
IEnumerable<Ticket> openTickets = tickets.Where(t => t.Status == TicketStatus.Open);
```

Where دادهٔ منبع را حذف یا تغییر نمی‌دهد.
یک توصیف از پیمایش فیلترشده می‌دهد.
IEnumerable<Ticket> قرارداد امکان پیمایش است؛ الزاماً List نیست.
نام openTickets به تنهایی معنی «نتیجه همین حالا محاسبه شده» ندارد.

در سمت چپ، `<Ticket>` یعنی عنصرهای قابل پیمایش از نوع Ticket هستند. IEnumerable را فعلاً چنین بخوان: «می‌توانم عنصرهایش را با foreach بخوانم.» این نام تضمین نمی‌کند که نتیجه از قبل داخل یک فهرست تازه ذخیره شده باشد.

اگر شرط را با foreach و if خودت اجرا می‌کردی، برای هر Ticket می‌پرسیدی آیا Status آن Open است و فقط جواب‌های مثبت را مصرف می‌کردی. Where همین معیار انتخاب را بیان می‌کند؛ هیچ Ticket بسته‌ای از tickets حذف نمی‌شود.

## ۴. enumeration

```csharp
foreach (Ticket ticket in openTickets)
{
    Console.WriteLine(ticket.Title);
}
```

با پیمایش، predicate روی عنصرها اجرا می‌شود.
در این query، تا وقتی پیمایش انجام نشود، filter اجرا نشده است.
این deferred execution است.
Count() و ToList() نیز query را پیمایش می‌کنند.
همهٔ متدهای LINQ deferred نیستند؛ نتیجهٔ عددی Count و Any باید در همان فراخوانی به دست بیاید.

enumeration یعنی طی‌کردن عنصرها، مثلاً توسط foreach. در این حلقه، ticket هر بار یکی از نتیجه‌هاست و Title همان عنصر چاپ می‌شود.

deferred یعنی «به‌تعویق‌افتاده»: تعریف Where فعلاً دستور انتخاب را می‌سازد؛ درخواست مصرف نتیجه باعث اجرای آن می‌شود. این موضوع ربطی به async یا اجرای خودکار در پس‌زمینه ندارد. در نمونهٔ ما، تا کسی openTickets را مصرف نکند، فیلتر خودش شروع به کار نمی‌کند.

با دادهٔ اولیه، این حلقه Email issue و سپس Printer issue را چاپ می‌کند؛ چون ترتیب اولیه Idها ۳، ۱، ۲ بود و هنوز OrderBy نداشتیم. خروجیِ مرتب ۲ و ۳ را در بخش مرتب‌سازی می‌سازیم.

## ۵. نمونهٔ deferred و snapshot

```csharp
IEnumerable<Ticket> deferredOpen = tickets.Where(t => t.Status == TicketStatus.Open);
List<Ticket> snapshotOpen = deferredOpen.ToList();
tickets.Add(new Ticket(4, "Backup issue"));
```

deferredOpen بعداً دادهٔ تازهٔ منبع را نیز می‌بیند.
snapshotOpen مجموعهٔ referenceهای انتخاب‌شده در زمان ToList را نگه داشته است.
ToList یک کپی عمیق از Ticketها نیست.

در این سه خط دقیقاً چه اتفاقی می‌افتد؟

| لحظه | اتفاق | Idهای باز |
| --- | --- | --- |
| تعریف deferredOpen | دستور فیلتر ساخته می‌شود | هنوز نتیجه ذخیره نشده |
| اجرای ToList | همان موقع فیلتر مصرف و List تازه ساخته می‌شود | snapshot شامل ۳ و ۲ |
| اجرای Add | Ticket تازه با Id برابر ۴ به منبع اضافه می‌شود | منبع اکنون ۳، ۲ و ۴ را باز دارد |
| Count روی deferredOpen در ادامه | فیلتر روی منبع فعلی اجرا می‌شود | تعداد ۳ |
| Count روی snapshotOpen در ادامه | تعداد اعضای List ساخته‌شده خوانده می‌شود | تعداد ۲ |

snapshot اینجا یعنی فهرست اعضای انتخاب‌شده در لحظهٔ ToList. خود List تازه است، ولی عضوهای آن به همان شیءهای Ticket اشاره می‌کنند. پس اضافه‌شدن عضو جدید به tickets، عضوهای snapshot را زیاد نمی‌کند؛ اما تغییر عنوان یکی از Ticketهای مشترک، از هر دو مسیر دیده می‌شود. این همان reference درس ۷ است.
اگر Title یکی از شیءهای مشترک عوض شود، snapshot نیز همان شیء تغییرکرده را می‌بیند.

برای snapshot مستقل از عنوان فعلی، می‌توان مقدار string عنوان‌ها را project کرد:

```csharp
List<string> titlesAtThisMoment = deferredOpen.Select(t => t.Title).ToList();
```

این فهرست رشته‌های آن لحظه را نگه می‌دارد؛ Rename یک Ticket بعدی، خود فهرست را بازنویسی نمی‌کند.

## ۶. OrderBy

```csharp
IEnumerable<Ticket> ordered = deferredOpen.OrderBy(t => t.Id);
```

مرتب‌سازی به دادهٔ لازم برای ترتیب کامل نیاز دارد.
OrderBy در LINQ to Objects deferred است، اما مثل Where streaming ساده نیست.
هنگام enumeration دادهٔ ورودی را برای sort پردازش می‌کند.
برای صفحه‌بندی قابل اتکا باید ترتیب صریح و قابل پیش‌بینی داشته باشیم.
اگر کلید اول یکسان است، ThenBy یک tie-breaker اضافه می‌کند.

`t => t.Id` دیگر سؤال بله/خیر نیست؛ مقدارِ کلید مرتب‌سازی، یعنی Id را می‌دهد. OrderBy به‌صورت پیش‌فرض صعودی مرتب می‌کند. پس بعد از اضافه‌شدن Backup issue، Idهای بازِ ۳، ۲، ۴ را به ترتیب ۲، ۳، ۴ تحویل می‌دهد؛ جای اعضا در خود tickets را عوض نمی‌کند.

tie-breaker یعنی معیار دوم برای وقتی معیار اول برابر است: مثلاً اول بر اساس عنوان، و میان دو عنوان یکسان بر اساس Id. ThenBy معیار دوم را به همان ترتیب اضافه می‌کند. streaming یعنی بتوانیم نتیجه‌ها را در حین پیمایش منبع تحویل بدهیم؛ برای مرتب‌سازی کلی باید دادهٔ لازم برای ترتیب را جمع کنیم، بنابراین به سادگی Where نیست.

## ۷. Select

```csharp
IEnumerable<string> lines = ordered.Select(t => $"#{t.Id}: {t.Title}");
```

Select شکل نتیجه را عوض می‌کند: اینجا Ticket به string تبدیل می‌شود.
Select filter نیست؛ هر عنصر ورودی یک نتیجه در این مثال دارد.
در آینده برای response DTO هم projection به کار می‌رود.
از Select برای تغییر Status همراه side effect استفاده نکن؛ عملیات تغییر را صریح اجرا کن.

در `$"#{t.Id}: {t.Title}"` مقدار Id و Title داخل متن قرار می‌گیرند؛ مثلاً Ticket با Id برابر ۲ به رشتهٔ `#2: Printer issue` تبدیل می‌شود. به همین دلیل نوع خروجی در سمت چپ IEnumerable<string> است و دیگر IEnumerable<Ticket> نیست.

تفاوت دو lambda مهم است: Where تابعی می‌خواهد که bool بدهد، ولی Select اینجا تابعی می‌خواهد که string بدهد. «projection» همان ساختن شکل خروجی دلخواه از هر ورودی است. side effect یعنی در ضمن محاسبه، چیزی بیرون از نتیجه را هم تغییر بدهیم؛ اینجا هدف ما گزارش‌دادن است، نه بستن Ticketها.

## ۸. ترکیب pipeline

```csharp
var lines = tickets
    .Where(t => t.Status == TicketStatus.Open)
    .OrderBy(t => t.Id)
    .Select(t => $"#{t.Id}: {t.Title}");
```

این قطعه جایگزین تعریف‌های میانی است؛ هم‌زمان با مثال قبلی در همان scope اعلان lines را تکرار نکن.
var نوع را از expression می‌گیرد.
هر مرحله وظیفهٔ مشخصی دارد.
خوانایی chain طولانی را با نام‌گذاری مراحل یا متدهای کوچک حفظ کن.

زنجیره را از بالا به پایین بخوان: از tickets شروع کن؛ بازها را انتخاب کن؛ بر اساس Id مرتب کن؛ از هر کدام یک رشته بساز. متغیر lines هنوز یک List آماده نیست؛ در این زنجیره ToList نداریم.

وقتی foreach نهایی نمونه lines یا زنجیرهٔ معادلش را مصرف می‌کند، متن‌ها چاپ می‌شوند. فقط نوشتن این تعریف، چیزی در ترمینال چاپ نمی‌کند. pipeline در این صفحه نام همین ترکیب مراحل است، نه ابزار یا thread تازه.

## ۹. FirstOrDefault و Single

```csharp
Ticket? ticket = tickets.FirstOrDefault(t => t.Id == 99);
```

برای نوع مرجع، نبود نتیجه اینجا null است.
FirstOrDefault در اولین match می‌تواند متوقف شود.
اگر قرارداد می‌گوید حداکثر یک match مجاز است، SingleOrDefault تکراری بودن را هم بررسی می‌کند.
SingleOrDefault با چند match exception می‌دهد.
انتخاب آن جای enforce کردن یکتایی هنگام ثبت را نمی‌گیرد.

| متد | صفر نتیجه | یک نتیجه | بیش از یک نتیجه |
| --- | --- | --- | --- |
| FirstOrDefault | null برای Ticket | همان Ticket | اولین match |
| SingleOrDefault | null برای Ticket | همان Ticket | exception |
| Single | exception | همان Ticket | exception |

OrDefault یعنی در نبود نتیجه، مقدار پیش‌فرض نوع را بده؛ برای Ticket که نوع مرجع است اینجا null داریم، نه برای هر نوعی در C#. در قطعهٔ بالا Id برابر ۹۹ وجود ندارد؛ پس قبل از استفاده از Title باید null را بررسی کنیم. enforce یعنی واقعاً قانون را هنگام ثبت داده اعمال کنیم؛ انتخاب متد جست‌وجو خودش جلوی ثبت Id تکراری را نمی‌گیرد.

## ۱۰. Any و Count

```csharp
bool hasOpenTickets = tickets.Any(t => t.Status == TicketStatus.Open);
int openCount = tickets.Count(t => t.Status == TicketStatus.Open);
```

برای سؤال «آیا وجود دارد؟»، Any معنی واضحی دارد و می‌تواند زودتر متوقف شود.
Count با predicate باید تعداد همهٔ matchها را به دست بیاورد.
Count property یک List با Count(predicate) یکی نیست.
فراخوانی Count و سپس foreach روی یک query deferred ممکن است دو پیمایش ایجاد کند.
اگر یک snapshot لازم است، یک بار ToList را آگاهانه انتخاب کن.

Any فقط می‌پرسد «حداقل یکی هست؟»؛ با پیدا شدن اولین Open می‌تواند true بدهد. Count با همین شرط می‌پرسد «چند تا هستند؟» و باید تعداد همهٔ بازها را به دست بیاورد. با دادهٔ اولیه نتیجهٔ این دو به ترتیب true و ۲ است؛ پس از Add تیکت چهارم، true و ۳ می‌شود.

پرانتزها را هم ببین: `snapshotOpen.Count` تعداد اعضای List موجود را می‌خواند؛ `deferredOpen.Count()` متد LINQ را روی query صدا می‌زند. در مثال ما این فراخوانی، فیلتر بازها را مصرف می‌کند.

## ۱۱. GroupBy

```csharp
foreach (var group in tickets.GroupBy(t => t.Status).OrderBy(g => g.Key))
{
    Console.WriteLine($"{group.Key}: {group.Count()}");
}
```

group.Key وضعیت مشترک است و خود گروه قابل پیمایش است.
GroupBy فقط وضعیت‌هایی را می‌دهد که در داده وجود دارند.
اگر Closed صفر است، گروه Closed خودکار ساخته نمی‌شود.
برای گزارش شامل صفرها باید روی همهٔ مقدارهای enum پیمایش کنیم و تعداد هرکدام را بگیریم.
همین تفاوت در نمونهٔ نهایی بخش اول دیده می‌شود.

GroupBy را مثل دسته‌کردن کارت‌ها بر اساس وضعیت بخوان. هر group یک دسته است؛ Key نام وضعیت آن دسته و Count تعداد Ticketهای داخل آن است. با دادهٔ پس از Add، دستهٔ Open سه عضو و Closed یک عضو دارد؛ InProgress عضوی ندارد، پس اصلاً گروهی برایش تولید نمی‌شود.

در lambda مربوط به OrderBy، حرف g این بار یک گروه است، نه یک Ticket؛ بنابراین `g.Key` کلید گروه را می‌خواند. اگر گزارش باید صفر را هم نشان بدهد، باید نام همهٔ وضعیت‌ها را از enum بگیریم، نه فقط گروه‌های موجود را. این کار موضوع تمرین است؛ قرار نیست GroupBy خودش گروه خالی اختراع کند.

## ۱۲. تغییر collection حین پیمایش

Where source را از خطر mutation مصون نمی‌کند.
اگر هنگام foreach روی query مبتنی بر List، ساختار منبع را تغییر بدهی، پیمایش می‌تواند خطا بدهد.
برای برنامهٔ concurrent، snapshot و synchronization باید آگاهانه طراحی شوند.
نمونهٔ این درس تک‌نخی است و تغییر قبل از پیمایش انجام می‌شود.

mutation یعنی تغییر داده. Add و Remove ساختار List را تغییر می‌دهند؛ اگر این کار را در حین foreach روی همان منبع انجام بدهی، شمارندهٔ پیمایش ممکن است نامعتبر شود و exception بگیری. Add بخش ۵ پیش از مصرف query انجام شده، پس آن مثال با «Add وسط حلقه» فرق دارد.

concurrent یعنی چند جریان اجرا بتوانند هم‌زمان به داده برسند؛ synchronization یعنی هماهنگ‌کردن این دسترسی‌ها. در این نمونه چنین اجرای هم‌زمانی نداریم؛ فعلاً کافی است تغییر قبل از پیمایش را با تغییر حین آن یکی نگیری.

## ۱۳. اجرا

[Program](../../../samples/09-ticket-linq/Program.cs)

```powershell
dotnet run --project samples/09-ticket-linq/SupportDesk.csproj
```

خروجی نمونه:

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

عددها را حفظ نکن؛ زمان ToList و Add را دنبال کن.
اگر Add را قبل از ToList ببری، Snapshot count باید ۳ شود.

## ۱۴. IEnumerable و IQueryable

IEnumerable در این مثال روی objectهای در حافظه اجرا می‌شود.
IQueryable می‌تواند expression را برای provider مانند EF Core توصیف کند.
همان syntax الزاماً همان رفتار، SQL یا هزینه را ندارد.
ToList زودهنگام روی دیتابیس می‌تواند حجم زیادی داده را به حافظه بیاورد.
آموزش ترجمه و محدودیت query دیتابیس در فصل data access انجام می‌شود.

provider یعنی بخشی که query را می‌فهمد و اجرا می‌کند؛ مثلاً provider دیتابیس ممکن است توصیف query را به SQL ترجمه کند. در این درس منبع ما List در حافظه است و هیچ اتصال دیتابیسی نداریم. لازم نیست اکنون IQueryable بسازی؛ هدف این بخش این است که بعداً از شباهت نوشتار، یکسان‌بودن هزینه و محل اجرا را نتیجه نگیری.

## تمرین مستقل

قبل از تمرین گزارش، هر بار تنها یک تغییر روی کپی نمونه بده:

1. Add تیکت چهارم را قبل از ToList ببر. حدس بزن چرا این بار هر دو تعداد ۳ می‌شوند.
2. دوباره به نسخهٔ اصلی برگرد. بعد از Add، عنوان Ticket با Id برابر ۲ را با Rename عوض کن؛ تعداد snapshot همچنان ۲ است، ولی آن عضو عنوان تازه دارد.
3. Where را فقط با foreach روی همهٔ tickets مقایسه کن: چرا در حالت بدون فیلتر، Ticket بسته هم دیده می‌شود؟

برای مرحلهٔ دوم، در کپی خودت snapshotOpen را هم پیمایش و Title اعضایش را چاپ کن؛ خروجی اصلی نمونه عنوان‌های snapshot را چاپ نمی‌کند.

گزارش را شامل همهٔ وضعیت‌ها، حتی تعداد صفر، کن.
درخواست‌های باز را به ترتیب عنوان و سپس Id نمایش بده.
چون عنوان فارسی و انگلیسی می‌تواند باشد، قرارداد مقایسهٔ متن را در README توضیح بده.
برای گزارش این تمرین می‌توانی StringComparer.Ordinal را انتخاب کنی؛ آن را ترتیب زبانی فارسی معرفی نکن.

## تمرین صفحه‌بندی

با OrderBy، Skip و Take پنج درخواست از صفحهٔ دوم نمایش بده.
صفحه از ۱ شروع می‌شود؛ page را بین ۱ و ۱۰۰۰ و pageSize را بین ۱ و ۱۰۰ محدود کن.
ورودی page صفر یا منفی باید پیش از query رد شود.
فقط محدود کردن page کافی نیست؛ pageSize بسیار بزرگ هم می‌تواند `(page - 1) * pageSize` را دچار overflow کند. هر دو ورودی را پیش از این ضرب بررسی کن؛ با سقف‌های بالا، بیشترین offset برابر ۹۹۹۰۰ است.

## سناریوهای پذیرش

| حالت | انتظار |
| --- | --- |
| هیچ درخواست باز | لیست خالی؛ گزارش Open صفر |
| شناسهٔ ناموجود | پیام ناموجود |
| دو عنوان یکسان | ترتیب دوم بر اساس Id |
| Add پس از ساخت query | query deferred دادهٔ تازه را ببیند |
| Add پس از ToList | تعداد snapshot ثابت بماند |
| Rename شیء مشترک | snapshot referenceها عنوان تازه را ببیند |

## سرنخ

برای صفرهای گزارش، Enum.GetValues<TicketStatus>() را بررسی کن.
برای snapshot عمیق، باید شکل دادهٔ مستقل تعریف کنی؛ ToList به تنهایی کافی نیست.
predicate را بدون تغییر دادن مدل نگه دار.

## معیار عبور

- [ ] Where و Select را یکی نمی‌دانم.
- [ ] زمان اجرای query را نشان می‌دهم.
- [ ] ToList را کپی عمیق نمی‌دانم.
- [ ] گزارش شامل صفرهاست.
- [ ] ترتیب صفحه‌بندی صریح است.
- [ ] query در حافظه و query دیتابیس را یکسان فرض نمی‌کنم.

## منابع رسمی

- [Introduction to LINQ queries](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries)
- [Standard query operators](https://learn.microsoft.com/en-us/dotnet/csharp/linq/standard-query-operators/)
- [Lambda expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)

## قدم بعد

فهرست Ticketها را روی دیسک ذخیره می‌کنیم تا اجرای بعدی آن را بازیابی کند.
