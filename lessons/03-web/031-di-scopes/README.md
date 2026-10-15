# درس ۰۳۱ — DI scope و lifetime در درخواست

[قبلی](../030-configuration/README.md) · [خانه](../../../README.md) · [بعدی](../032-rest/README.md)

## مسئله و خروجی

DI فقط ساختن کلاس بدون new نیست؛ طول عمر وابستگی باید با مصرف‌کننده سازگار باشد.
در این درس با شناسهٔ instance رفتار scoped را بین دو dependency در یک request اندازه می‌گیریم.
هم‌زمان ثابت می‌کنیم دادهٔ singleton store بین requestها باقی می‌ماند.
به جای حفظ کردن تعریف‌ها، شواهد واقعی از HTTP می‌گیریم.

## ۱. سه طول عمر

| ثبت | در یک provider / scope | خطر متداول |
| --- | --- | --- |
| Singleton | یک instance برای provider | state مشترک و thread safety |
| Scoped | یک instance در scope | resolve از root یا capture در singleton |
| Transient | instance هنگام resolve | فرض اشتباه دربارهٔ disposal یا استفادهٔ مشترک |

در host وب معمول، هر request یک scope دارد.
«scoped یعنی یک نمونه برای هر user» درست نیست.
دو request یک کاربر هم می‌توانند scopeهای جدا داشته باشند.
WebSocket، background job و scope دستی نیاز به تحلیل جدا دارند.

## ۲. probe کوچک

```csharp
public sealed class RequestStamp
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

RequestStamp در Application ثبت scoped دارد.
Guid اینجا ابزار مشاهدهٔ instance است و هیچ مجوزی ایجاد نمی‌کند.
در constructor TicketService هم همین type تزریق شده است.
service یک property خواندنی RequestId برای آزمایش دارد.
در محصول نهایی چنین probeای را فقط در صورت نیاز تشخیصی حفظ کن.

## ۳. دو مسیر resolve

```csharp
app.MapGet("/diagnostics/scope", (RequestStamp stamp, TicketService service) =>
    Results.Ok(new { direct = stamp.Id, viaService = service.RequestId }));
```

framework هر دو dependency را از scope همین request resolve می‌کند.
stamp مستقیم و stamp داخل service باید Id یکسان داشته باشند.
این نشان می‌دهد scoped در همان scope به یک instance برمی‌گردد.
endpoint فقط داخل شرط Development map می‌شود و در مستندات OpenAPI پنهان خواهد بود.

## ۴. دو request متفاوت

در Development دو بار /diagnostics/scope را بخوان.
در هر پاسخ direct و viaService باید برابر باشند.
بین دو پاسخ Guid باید متفاوت باشد.
اگر یکسان مانده، registration یا محل cache کردن dependency را بررسی کن.
برای تحلیل lifetime تعداد requestها و scope را ثبت کن، نه فقط تعداد بار فراخوانی متد را.

## ۵. service scoped و store singleton

```csharp
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<RequestStamp>();
```

جهت مصرف service scoped به store singleton مجاز است.
پایان scope درخواست، store مشترک را حذف نمی‌کند.
یک create و سپس GET در request بعدی باید همان داده را ببینند.
اگر store را هم scoped کنی، در این نمونه هر request dictionary تازه می‌گیرد و Location دیگر دادهٔ ساخته‌شده را نمی‌یابد.
این نشان می‌دهد انتخاب lifetime بخشی از رفتار محصول است.

## ۶. captive dependency

اگر singleton سازنده‌اش TicketService scoped بخواهد، وابستگی کوتاه‌عمر در شیء بلندعمر گیر می‌کند.
تزریق موفق ظاهری با خاموش بودن validation طراحی را درست نمی‌کند.
این نمونه validation را برای همهٔ environmentها روشن کرده است.
عوض کردن همهٔ serviceها به singleton راه‌حل عمومی خطای DI نیست.

## ۷. validation container

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

ValidateScopes به تشخیص مصرف scope نامناسب کمک می‌کند.
ValidateOnBuild بخشی از گراف قابل ساخت serviceها را هنگام Build بررسی می‌کند.
این‌ها اثبات thread safety یا درستی همهٔ factoryهای پویا نیستند.
open genericها و رفتارهایی که فقط هنگام اجرا رخ می‌دهند ممکن است به تست جدا نیاز داشته باشند.
تست HTTP همچنان ضروری است.

## ۸. thread safety با scoped یکی نیست

دو request متفاوت می‌توانند هم‌زمان به singleton store برسند.
بنابراین lock یا طراحی concurrency مناسب برای state مشترک لازم است.
scoped بودن TicketService به dictionary singleton قفل خودکار اضافه نمی‌کند.
همچنین در یک request با Task.WhenAll هم می‌توان چند کار concurrent روی dependency یک scope اجرا کرد.
یک instance مشترک الزاماً مجوز مصرف parallel نیست؛ این نکته برای DbContext مهم خواهد بود.

## ۹. مالکیت disposal

serviceهایی که container می‌سازد، طبق قرارداد lifetime توسط آن dispose می‌شوند.
وابستگی‌ای که با new بیرون از container ساخته و instance آن را ثبت کرده‌ای می‌تواند مالکیت متفاوتی داشته باشد.
dependency تزریق‌شده را در پایان action دستی Dispose نکن.
برای resource کوتاه‌عمر خودت، مالکیت و using را صریح نگه دار.
رفتار IDisposable و IAsyncDisposable را با نوع واقعی resource بررسی کن.

## ۱۰. middleware و scoped dependency

conventional middleware معمولاً instance بلندعمر دارد.
dependency scoped را می‌توان در InvokeAsync دریافت کرد تا از request scope resolve شود.
نباید TicketService scoped را در constructor RequestIdMiddleware فعلی تزریق کنیم.
راه دیگر IMiddleware با registration مناسب است که در این درس لازم نشده است.
این دو سبک را بدون توجه به activation یکسان فرض نکن.

## ۱۱. کار پس‌زمینه

خارج از request خودکار scope درخواست وجود ندارد.
اگر background worker به dependency scoped نیاز داشته باشد، باید scope مشخص بسازد و آن را dispose کند.
HttpContext را بعد از پایان request در یک Task رهاشده ذخیره نکن.
جزئیات BackgroundService و shutdown در بخش مربوط آموزش داده می‌شود.
فعلاً هیچ fire-and-forget برای ذخیرهٔ Ticket ایجاد نمی‌کنیم.

## بررسی عملی

Development: برابری داخل پاسخ و تفاوت بین دو پاسخ probe را تأیید کن.
Production: مسیر diagnostics باید ۴۰۴ بدهد.
یک Ticket بساز و از request تازه بخوان.
برنامه را restart کن؛ store process تازه خالی است.
این چهار مشاهده مرز scope درخواست، عمر process و persistence را جدا می‌کنند.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/30-request-scopes/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5310
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5310/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5310/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5310/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/30-request-scopes/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

در کپی نمونه RequestStamp را موقتاً transient کن و نتایج probe را ثبت کن.
سپس آن را singleton کن و نتایج بین دو request را مقایسه کن.
در پایان registration صحیح scoped را برگردان.
تفاوت مشاهده‌شده را با تعداد resolve توضیح بده.

## تمرین تشخیصی

یک singleton آزمایشی بساز که TicketService scoped بخواهد.
شکست startup با validation را مشاهده و وابستگی غلط را حذف کن.
برای اصلاح، اول lifetime واقعی مصرف‌کننده را بررسی کن؛ container validation را خاموش نکن.

## معیار عبور

- [ ] scoped را به user یا thread نسبت نمی‌دهم.
- [ ] در یک request probeهای scoped برابر و بین requestها متفاوت‌اند.
- [ ] دادهٔ store singleton بین requestها باقی می‌ماند.
- [ ] captive dependency و مرز disposal را توضیح می‌دهم.
- [ ] singleton بودن را تضمین thread safety نمی‌دانم.

## منابع رسمی

- [Dependency injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)
- [DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
