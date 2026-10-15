# درس ۰۲۶ — controller و action و قرارداد API

[قبلی](../025-routing/README.md) · [خانه](../../../README.md) · [بعدی](../027-dto/README.md)

## مسئله و خروجی

Minimal API درس قبل کار می‌کند، اما همهٔ عملیات را در Program.cs نگه داشتن با رشد قابلیت‌ها خوانایی را کم می‌کند.
این بار routeهای Ticket را به controller منتقل می‌کنیم و startup را در composition root نگه می‌داریم.
در پایان باید بتوانی مسیر POST را از HTTP تا حافظه ردیابی کنی و برای جای هر کلاس دلیل بیاوری.
این درس هم‌زمان نسخهٔ کامل CLI را منتقل نمی‌کند؛ فعلاً create، list، find و delete داریم.

## ۱. Controller دقیقاً چیست؟

Controller کلاس presentation است که framework برای اجرای actionها به کار می‌گیرد.
مسئولیت آن دریافت ورودی HTTP، فراخوانی عملیات و انتخاب پاسخ HTTP است.
قاعدهٔ معتبر بودن عنوان متعلق به HTTP نیست و در Domain باقی می‌ماند.
ذخیره و قفل dictionary هم نباید به controller منتقل شود.
ControllerBase امکانات API را دارد؛ Controller قابلیت view هم اضافه می‌کند که در این پروژه لازم نداریم.

## ۲. سه بخش startup

```csharp
builder.Services.AddControllers();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddScoped<TicketService>();
// پس از builder.Build():
app.MapControllers();
```

AddControllers سرویس‌ها و قابلیت‌های MVC را ثبت می‌کند.
MapControllers endpointهای attribute routing را به برنامه متصل می‌کند.
اگر اولی یا دومی را فراموش کنی، داشتن فایل controller کافی نیست.
MapGet مربوط به /health همچنان می‌تواند کنار controllerها وجود داشته باشد.
این انتخاب معماری الزام نمی‌کند همهٔ endpointها یک سبک داشته باشند.

## ۳. attributeهای سطح کلاس

```csharp
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(TicketService service) : ControllerBase
```

Route یک prefix ثابت برای این گروه تعریف می‌کند.
ApiController رفتارهایی مانند نیاز به attribute routing و پاسخ خودکار برای ModelState نامعتبر فعال می‌کند.
این attribute جای تمام قواعد business را نمی‌گیرد.
پارامتر سازنده از DI می‌آید؛ framework آن را از JSON request نمی‌سازد.
شیوهٔ primary constructor اینجا همان وابستگی سازنده را کوتاه می‌کند، نه یک نوع service locator.

## ۴. ترکیب route کلاس و action

| attribute action | مسیر نهایی | method |
| --- | --- | --- |
| HttpGet بدون template | /api/tickets | GET |
| HttpGet با {id:int} | /api/tickets/1 | GET |
| HttpPost بدون template | /api/tickets | POST |
| HttpDelete با {id:int} | /api/tickets/1 | DELETE |

در URL از نام متد C# استفاده نشده است.
تغییر نام Create به Register نباید به تنهایی URL عمومی را عوض کند.
مسیر /tickets درس قبل و /api/tickets این snapshot متفاوت‌اند؛ client را هم آگاهانه تغییر بده.
در محصول منتشرشده چنین تغییر URL بدون برنامهٔ سازگاری انجام نمی‌شود.

## ۵. پاسخ typed و نتیجهٔ HTTP

ActionResult<TicketResponse> هم payload موفق را مستند می‌کند و هم نتیجه‌هایی مثل NotFound یا Problem را می‌پذیرد.
IActionResult برای DELETE که payload موفق ندارد کافی است.
Ok دادهٔ domain را خودکار به قرارداد مناسب طراحی‌شده تبدیل نمی‌کند؛ mapping را خودمان انجام می‌دهیم.
Returning string با متن «404» با status واقعی ۴۰۴ برابر نیست.

## ۶. ساخت و Location

```csharp
var ticket = service.Create(title);
return CreatedAtAction(nameof(GetById), new { id = ticket.Id },
    new TicketResponse(ticket.Id, ticket.Title));
```

CreatedAtAction پاسخ ۲۰۱ و Location منبع تازه را از route action مقصد می‌سازد.
نام route parameter باید با id مقصد هماهنگ باشد.
nameof اشتباه تایپی در نام متد را کم می‌کند، اما سازگاری template و مقادیر همچنان باید تست شود.
Location را فقط به عنوان رشته تماشا نکن؛ همان URL را GET کن و همان Id را بخواه.
این نمونه URL را با دادهٔ request می‌سازد و موضوع reverse proxy در بخش استقرار بررسی خواهد شد.

## ۷. ساختار فایل و جهت استفاده

| پوشه / کلاس | کار اصلی | به چه چیزی دسترسی دارد؟ |
| --- | --- | --- |
| Controllers/TicketsController | قرارداد HTTP | Application، Contracts، Domain output برای mapping این مرحله |
| Contracts | شکل request و response | نوع‌های سادهٔ داده |
| Application/TicketService | اجرای use case | ITicketStore و Domain |
| Application/ITicketStore | نیاز برنامه از ذخیره‌سازی | Ticket و TicketTitle |
| Domain | داده و قانون مستقل از HTTP | کتابخانهٔ پایه |
| Infrastructure/InMemoryTicketStore | پیاده‌سازی حافظه‌ای | قرارداد Application و Domain |
| Program.cs | اتصال implementationها | همهٔ بخش‌های لازم برای composition |

فعلاً همه در یک csproj هستند؛ پوشه جلوی reference اشتباه را در کامپایل نمی‌گیرد.
جدا کردن assembly و تست وابستگی در بخش معماری می‌آید.
هدف فعلی خوانایی مسئولیت‌هاست، نه زیاد کردن تعداد پروژه‌ها.

## ۸. چرا service و interface؟

TicketService عنوان خام را به TicketTitle تبدیل و store را صدا می‌زند.
ITicketStore نیاز مشخص این مثال را می‌گوید؛ generic repository برای هر entity نساخته‌ایم.
interface جای طراحی query کارآمد یا transaction را نمی‌گیرد.
وجود متد یک‌خطی در این مرحله برای نشان دادن مرز use case است؛ در پروژهٔ کوچک می‌توان هزینهٔ آن را دوباره ارزیابی کرد.
هیچ کلاسی برای انجام I/O مصنوعی Task.Run یا Task.FromResult اضافه نمی‌کند.
store فعلی در حافظه است و متدهای sync آن صادقانه sync مانده‌اند.

## ۹. singleton و scoped

store یک singleton در هر process است تا داده پس از پایان request پاک نشود.
service scoped است و در هر request scope قابل استفاده است.
singleton بودن به تنهایی thread safety ایجاد نمی‌کند؛ store با lock دسترسی را هماهنگ می‌کند.
این lock فقط یک process را پوشش می‌دهد و جای concurrency دیتابیس نیست.
رفتار scope را در درس ۰۳۱ اندازه می‌گیریم.

## ۱۰. snapshot و ایمنی داده

All زیر lock یک آرایهٔ مرتب‌شده می‌سازد و live Dictionary را بیرون نمی‌دهد.
Ticket رکوردی با propertyهای get-only و سازندهٔ خصوصی است.
factory ساخت، Id غیرمثبت و title برابر null را حتی برای caller داخلی رد می‌کند؛ validation controller جای guard Domain را نمی‌گیرد.
Rename مدل تازه‌ای می‌سازد؛ نتیجهٔ Find را نمی‌توان با setter تغییر داد.
این انتخاب باعث می‌شود serialize پاسخ با mutation مستقیم entity مشترک درگیر نباشد.
هرچند store هنوز تاریخچه، persistence و قابلیت چندسرور ندارد.

## ۱۱. بررسی عملی

در server تازه یک عنوان معتبر بساز و Location را بخوان.
GET همان آدرس باید ۲۰۰ با Id=1 بدهد.
GET /api/tickets/0 باید ۴۰۰ و Id مثبت ناموجود ۴۰۴ بدهد.
DELETE اول ۲۰۴ و تکرارش ۴۰۴ است.
restart برنامه فهرست را خالی می‌کند؛ این محدودیت آگاهانهٔ بخش سوم است.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/25-controllers/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5260
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5260/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5260/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5260/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/25-controllers/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

برای هر action یک جدول ورودی، dependency، موفقیت و failure بنویس.
یک endpoint خواندنی برای تعداد Ticketها طراحی کن و مشخص کن محاسبه کجا قرار می‌گیرد.
controller نباید dictionary جدیدی برای ذخیرهٔ مستقل بسازد.
در README تمرین، مسیر واقعی یک create را با نام فایل‌ها توضیح بده.

## تمرین تشخیصی

در نسخهٔ تمرینی MapControllers را موقتاً حذف کن و تفاوت /health و /api/tickets را ببین.
سپس آن را برگردان؛ تست GET و POST باید دوباره موفق شود.
یک service را عمداً ثبت نکن و خطای resolve را با خطای routing مقایسه کن.

## معیار عبور

- [ ] AddControllers و MapControllers را از هم جدا می‌شناسم.
- [ ] controller ذخیره‌سازی و قاعدهٔ عنوان را مالک نیست.
- [ ] GET روی Location منبع ساخته‌شده موفق است.
- [ ] جهت وابستگی و محدودیت پوشه در یک assembly را توضیح می‌دهم.
- [ ] علت scoped بودن service و singleton بودن store را می‌دانم.

## منابع رسمی

- [Controller-based APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Controller action return types](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0)
