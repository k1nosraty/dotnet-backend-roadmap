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

## ۲. lambda

```csharp
t => t.Status == TicketStatus.Open
```

t پارامتر ورودی تابع کوتاه است.
عبارت سمت راست نتیجهٔ bool می‌دهد.
نوع t از collection مشخص می‌شود.
برای شروع این را مثل یک تابع بررسی وضعیت بخوان.
این expression در Where نقش predicate دارد.

## ۳. Where

```csharp
IEnumerable<Ticket> openTickets = tickets.Where(t => t.Status == TicketStatus.Open);
```

Where دادهٔ منبع را حذف یا تغییر نمی‌دهد.
یک توصیف از پیمایش فیلترشده می‌دهد.
IEnumerable<Ticket> قرارداد امکان پیمایش است؛ الزاماً List نیست.
نام openTickets به تنهایی معنی «نتیجه همین حالا محاسبه شده» ندارد.

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

## ۵. نمونهٔ deferred و snapshot

```csharp
IEnumerable<Ticket> deferredOpen = tickets.Where(t => t.Status == TicketStatus.Open);
List<Ticket> snapshotOpen = deferredOpen.ToList();
tickets.Add(new Ticket(4, "Backup issue"));
```

deferredOpen بعداً دادهٔ تازهٔ منبع را نیز می‌بیند.
snapshotOpen مجموعهٔ referenceهای انتخاب‌شده در زمان ToList را نگه داشته است.
ToList یک کپی عمیق از Ticketها نیست.
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

## ۷. Select

```csharp
IEnumerable<string> lines = ordered.Select(t => $"#{t.Id}: {t.Title}");
```

Select شکل نتیجه را عوض می‌کند: اینجا Ticket به string تبدیل می‌شود.
Select filter نیست؛ هر عنصر ورودی یک نتیجه در این مثال دارد.
در آینده برای response DTO هم projection به کار می‌رود.
از Select برای تغییر Status همراه side effect استفاده نکن؛ عملیات تغییر را صریح اجرا کن.

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

## ۹. FirstOrDefault و Single

```csharp
Ticket? ticket = tickets.FirstOrDefault(t => t.Id == 99);
```

برای نوع مرجع، نبود نتیجه اینجا null است.
FirstOrDefault در اولین match می‌تواند متوقف شود.
اگر قرارداد می‌گوید حداکثر یک match مجاز است، SingleOrDefault تکراری بودن را هم بررسی می‌کند.
SingleOrDefault با چند match exception می‌دهد.
انتخاب آن جای enforce کردن یکتایی هنگام ثبت را نمی‌گیرد.

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

## ۱۲. تغییر collection حین پیمایش

Where source را از خطر mutation مصون نمی‌کند.
اگر هنگام foreach روی query مبتنی بر List، ساختار منبع را تغییر بدهی، پیمایش می‌تواند خطا بدهد.
برای برنامهٔ concurrent، snapshot و synchronization باید آگاهانه طراحی شوند.
نمونهٔ این درس تک‌نخی است و تغییر قبل از پیمایش انجام می‌شود.

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

## تمرین مستقل

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
