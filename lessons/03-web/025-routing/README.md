# درس ۰۲۵ — routing و binding ورودی

[قبلی](../024-web-host/README.md) · [خانه](../../../README.md) · [بعدی](../026-controllers/README.md)

## خروجی درس

route، query string، header و JSON body را به پارامترهای handler متصل می‌کنی.
تطبیق مسیر، تبدیل نوع و validation business را از هم جدا می‌کنی.
علت تفاوت ۴۰۰، ۴۰۴، ۴۰۵ و ۴۱۵ را در یک API واقعی مشاهده می‌کنی.

## پیش‌نیاز

HTTP، host، DI، nullable و TryParse.
این درس Minimal API دارد؛ controller و action درس ۰۲۶ است.
validation پیشرفته و ProblemDetails نیز درس ۰۲۸ خواهد بود.

## ۱. routing

routing انتخاب handler مناسب بر اساس مسیر، method و metadata endpoint است.
binding تبدیل ورودی انتخاب‌شده به پارامترهای handler است.
validation بررسی می‌کند مقدار تبدیل‌شده برای عملیات مورد نظر مجاز است یا نه.
این سه مرحله معنی یکسانی ندارند.

## ۲. جدول قرارداد endpointها

| method و مسیر | ورودی | خروجی |
| --- | --- | --- |
| GET /tickets | q و limit اختیاری | آرایهٔ فیلترشده |
| GET /tickets/{id:int} | Id route با constraint | Ticket یا خطا |
| GET /lookup/{id} | Id بدون constraint | مشاهدهٔ خطای binding |
| GET /client | X-Client header اختیاری | label یا unknown |
| POST /tickets | JSON title | ۲۰۱ و Location |

lookup فقط برای مقایسهٔ آموزشی است؛ در یک API واقعی endpoint تکراری بی‌دلیل نمی‌سازیم.
ذخیرهٔ این نمونه حافظه‌ای است و پس از restart پاک می‌شود.

## ۳. route parameter

```csharp
app.MapGet("/tickets/{id:int}", ([FromRoute] int id, [FromServices] TicketStore store) => Find(id, store));
```

{id} بخشی از مسیر را نام‌گذاری می‌کند.
:int یک constraint برای تطبیق مسیر است.
FromRoute محل دریافت مقدار را صریح نشان می‌دهد.
store از DI می‌آید و نباید از JSON کاربر ساخته شود.

## ۴. constraint و validation

/tickets/abc با constraint :int تطبیق نمی‌کند و معمولاً به ۴۰۴ می‌رسد.
/tickets/-1 با int قابل تطبیق است، اما Id مثبت نیست و handler ۴۰۰ می‌دهد.
استفاده از :min(1) می‌تواند مقدار منفی را به no route match تبدیل کند؛ اگر policy خطای ورودی ۴۰۰ است، این انتخاب را آگاهانه انجام بده.
constraint را فقط برای «همهٔ validationها» استفاده نکن.

## ۵. بدون constraint

```csharp
app.MapGet("/lookup/{id}", ([FromRoute] int id, [FromServices] TicketStore store) => Find(id, store));
```

اینجا /lookup/abc با template مسیر تطبیق می‌کند.
اما binding به int شکست می‌خورد و framework پاسخ ۴۰۰ می‌دهد.
این خطا پیش از منطق Find رخ می‌دهد.
تفاوت دو endpoint نشان می‌دهد ۴۰۴ همیشه نتیجهٔ «Ticket وجود ندارد» نیست.

## ۶. query parameter اختیاری

```csharp
app.MapGet("/tickets", ([FromQuery(Name = "q")] string? query,
    [FromQuery] int? limit, [FromServices] TicketStore store) =>
{
    int take = limit ?? 20;
    if (take < 1 || take > 50)
        return Results.BadRequest(new { error = "Limit must be 1-50." });
    if (query is not null && query.Length > 120)
        return Results.BadRequest(new { error = "Query is too long." });
    var found = store.All()
        .Where(t => string.IsNullOrWhiteSpace(query) || t.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
        .Take(take).ToArray();
    return Results.Ok(found);
});
```

q نام بیرونی query است و query نام متغیر C#.
limit اگر غایب باشد null است و پیش‌فرض ۲۰ اعمال می‌شود.
اگر limit=abc باشد، تبدیل به int? شکست می‌خورد و پیش از handler ۴۰۰ می‌دهد.
nullable بودن به معنی پذیرفتن هر متن غیرقابل تبدیل نیست.

## ۷. قواعد query

limit باید بین ۱ و ۵۰ باشد.
q طول حداکثر ۱۲۰ دارد؛ مقدار غایب، خالی یا whitespace در این نمونه به معنی بدون فیلتر است.
Title.Contains با OrdinalIgnoreCase انجام می‌شود و ترتیب نتایج از store بر اساس Id است.
این جست‌وجوی زبانی پیشرفتهٔ فارسی یا جست‌وجوی full-text دیتابیس نیست.
Take حداکثر نتایج را کم می‌کند، اما این pagination کامل با offset یا cursor نیست.

## ۸. header input

```csharp
app.MapGet("/client", ([FromHeader(Name = "X-Client")] string? client) =>
{
    if (client is not null && client.Length > 50)
        return Results.BadRequest(new { error = "Client label is too long." });
    return Results.Ok(new { client = client ?? "unknown" });
});
```

header اختیاری است و absence مقدار unknown می‌دهد.
این label فقط دادهٔ ارسالی client است.
با ارسال X-Client: admin کسی مجوز admin پیدا نمی‌کند.
authentication و authorization مرزهای جدا دارند و در فصل امنیت آموزش داده می‌شوند.

## ۹. JSON body

```csharp
public sealed record CreateTicketRequest(string? Title);
```

در handler از [FromBody] CreateTicketRequest استفاده می‌کنیم.
record قرارداد transport است؛ Title هنوز ممکن است null یا کوتاه باشد.
در این نمونه AddValidation خودکار ثبت نشده و طول عنوان را صریح بررسی می‌کنیم.
بعد از موفقیت، TicketTitle.Create ارزش معتبر می‌سازد.
DTO معتبر از نظر JSON الزاماً domain value معتبر نیست.

## ۱۰. binding body و validation title

| body یا header | نتیجهٔ مورد انتظار |
| --- | --- |
| JSON با title معتبر | ۲۰۱ |
| JSON با title کوتاه | ۴۰۰ از guard خودمان |
| JSON با title غایب یا null | ۴۰۰ از guard خودمان |
| JSON نحوی خراب | ۴۰۰ از binding |
| JSON با title عددی | ۴۰۰ از تبدیل JSON |
| body غایب برای پارامتر ضروری | ۴۰۰ |
| media type نامناسب مانند text/plain | ۴۱۵ |

جزئیات body خطای framework با environment و configuration فرق می‌کند.
روی متن خطای داخلی یا stack trace قرارداد API پایدار نساز.
در فصل ProblemDetails شکل failureهای عمومی را استاندارد می‌کنیم.

## ۱۱. service binding

FromServices صریح نشان می‌دهد TicketStore dependency برنامه است.
در Minimal API بعضی typeهای ثبت‌شده بدون attribute نیز از DI inferred می‌شوند.
برای آموزش این درس منبع پارامترها را صریح نگه داشته‌ایم.
ثبت service را با ارسال property از body جایگزین نکن.
همچنین نام Id در body نباید بی‌دلیل route Id را override کند.

## ۱۲. query در GET و body در POST

ورودی جست‌وجو از query گرفته می‌شود.
GET body در این قرارداد نداریم و به پشتیبانی یکسان آن در clientها و واسطه‌ها تکیه نمی‌کنیم.
POST title را از body JSON می‌گیرد.
پیام request، URL و body بخش‌های متفاوت‌اند و binding باید مطابق قرارداد باشد.

## ۱۳. اجرای نمونه

```powershell
dotnet run --project samples/24-routing-binding/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5250
```

برای seed در ترمینال دوم:

```powershell
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5250/tickets -ContentType application/json -Body $body
```

حالا این مسیرها را بررسی کن:

```powershell
curl.exe -i "http://127.0.0.1:5250/tickets?q=network&limit=5"
curl.exe -i http://127.0.0.1:5250/tickets/abc
curl.exe -i http://127.0.0.1:5250/lookup/abc
curl.exe -i "http://127.0.0.1:5250/tickets?limit=abc"
curl.exe -i -H "X-Client: desktop" http://127.0.0.1:5250/client
```

query دارای & را در PowerShell quote کن تا shell آن را جدا تفسیر نکند.

## ۱۴. تست وضعیت منبع

قبل از درخواست نامعتبر، فهرست Ticketها را بخوان.
چند body خراب یا عنوان کوتاه بفرست.
بعد فهرست را دوباره بخوان؛ تعداد و Idها نباید به خاطر ورودی نامعتبر زیاد شده باشند.
فقط مشاهدهٔ ۴۰۰ کافی نیست؛ اثر business نیز باید بررسی شود.

## ۱۵. ۴۰۵ در برابر ۴۰۴

اگر مسیر موجود باشد ولی method مناسب ثبت نشده باشد، framework می‌تواند ۴۰۵ همراه Allow بدهد.
اگر هیچ endpoint مطابق مسیر نباشد، ۴۰۴ است.
برای نمونه PUT /tickets در این پروژه وجود ندارد و قرارداد ایجاد POST است.
GET /tickets/1 و POST /tickets یک قرارداد مشترک برای methodهای متفاوت نیستند.

## ۱۶. route ثابت و parameter

هنگام افزودن مسیر ثابت مانند /tickets/search به templateهای پارامتری دقت کن.
constraintها می‌توانند به تفکیک routeها کمک کنند.
routeهای مبهم با ترتیب تصادفی Mapها حل قابل اتکا ندارند.
در این نمونه جست‌وجو با query روی /tickets است تا مسیر تکراری لازم نباشد.

## تمرین مستقل

یک query اختیاری برای انتخاب prefix عنوان اضافه کن.
اگر دادهٔ فیلتر نامعتبر است، پیام روشن و ۴۰۰ بده.
نبود فیلتر و فیلتر نامعتبر را یکی نکن.
requestهای معتبر، غایب و خراب را با status و اثر روی داده بررسی کن.

## تمرین تکمیلی

قرارداد endpoint جدیدی را بنویس که route Id و body title بگیرد.
مشخص کن Id در کدام بخش authority دارد و اگر body هم Id داشت چه می‌کنی.
پیاده‌سازی تغییر عنوان در درس controller و DTO توسعه داده می‌شود؛ فعلاً contract و سناریوها را کامل بنویس.

## معیار عبور

- [ ] routing، binding و validation را تفکیک می‌کنم.
- [ ] /tickets/abc و /lookup/abc را مقایسه کرده‌ام.
- [ ] query nullable با تبدیل ناموفق همچنان ۴۰۰ می‌دهد.
- [ ] منبع route، query، header، body و service روشن است.
- [ ] DTO raw و value domain را یکی نمی‌دانم.
- [ ] ورودی خراب state را تغییر نمی‌دهد.
- [ ] header label را هویت تأییدشده فرض نمی‌کنم.

## منابع رسمی

- [Minimal API parameter binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0)
- [Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0)
- [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)

## قدم بعد

[درس ۰۲۶](../026-controllers/README.md) به controller، action و قرارداد API اختصاص دارد.
قبل از آن، جدول ورودی و status endpointهای تمرین خودت را کامل کن.
