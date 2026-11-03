# درس ۰۲۸ — validation و ProblemDetails

[قبلی](../027-dto/README.md) · [خانه](../../../README.md) · [بعدی](../029-middleware/README.md)

## مسئله و خروجی

اگر عنوان واردشده نامعتبر است، client باید بداند کدام فیلد مشکل دارد تا پیام را کنار همان ورودی نشان بدهد. در این درس، با `DataAnnotations` قواعدی را روی propertyهای DTO می‌نویسیم و خروجی شکست را در قالب `ValidationProblemDetails` می‌گیریم. این مسیر را از JSON خراب، تبدیل ناموفق نوع و خطای غیرمنتظرهٔ server جدا آزمایش می‌کنیم.
این درس کتابخانهٔ validation خارجی نصب نمی‌کند.

## ۱. چهار مرز نامعتبر بودن

| مرحله | ورودی نمونه | نتیجه در این API |
| --- | --- | --- |
| JSON syntax | آکولاد ناقص | ۴۰۰ پیش از action |
| binding / conversion | title عددی | ۴۰۰ پیش از action |
| DTO validation | title غایب یا خیلی بلند | ۴۰۰ خودکار ApiController |
| guard پس از normalization | عنوان دوحرفی همراه فاصله | ۴۰۰ از controller |

لایهٔ Domain نیز باید در برابر caller داخلی عنوان خراب محافظت کند.
ندیدن action در debugger به معنی اجرا نشدن هیچ کدی در framework نیست.
ModelState می‌تواند پیش از ورود به action نامعتبر شده باشد.

## ۲. DataAnnotations روی کلاس DTO

```csharp
public sealed class CreateTicketRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string? Title { get; init; }
}
```

صفت Required مقدار null، خالی و whitespace را با پیش‌فرض این annotation رد می‌کند.
StringLength طول string را محدود می‌کند.
از این snapshot requestهای validated کلاس با init هستند و annotation روی property قرار دارد.
این تغییر نسبت به record کوچک درس قبل انتخاب آگاهانه برای metadata روشن property است.
در positional recordهای MVC، metadata اعتبارسنجی باید روی پارامتر constructor باشد؛ property: را بی‌دلیل از مثال‌های schema یا Minimal API کپی نکن.
رفتار record و class را با نوع واقعی DTO و pipeline همان framework تست کن.

## ۳. raw در برابر normalized

قواعد annotation روی متن دریافتی یا `raw` اجرا می‌شوند و خودشان `Trim` نمی‌کنند. سپس controller فاصله‌های ابتدا و انتها را حذف می‌کند؛ این شکل آمادهٔ مقایسه را در این درس `normalized` می‌نامیم. طول همین متن آماده نیز دوباره بررسی می‌شود، چون حذف فاصله می‌تواند نتیجهٔ قانون حداقل طول را عوض کند.
مثلاً عنوان « ab » ممکن است از حداقل طول خام عبور کند ولی پس از Trim رد می‌شود.
در مقابل raw طولانی‌تر از ۱۲۰ حتی اگر پس از Trim کوتاه شود، در این قرارداد رد می‌شود.
این سیاست ورودیِ نمونه است؛ اگر محصول سیاست دیگری می‌خواهد باید validation قبل و بعد را هماهنگ طراحی کنی.

## ۴. ApiController و ModelState

مجموعهٔ `ModelState` نتیجهٔ binding و validation مدل ورودی را نگه می‌دارد. اگر نامعتبر باشد، رفتار `[ApiController]` پیش از ورود به action پاسخ ۴۰۰ می‌سازد؛ پس لازم نیست ابتدای همهٔ actionها همان `if (!ModelState.IsValid)` را تکرار کنیم. اما ModelState نمی‌داند شناسهٔ درخواست در storage موجود است یا تغییر وضعیت آن مجاز است؛ این بررسی‌ها بعداً در مسیر عملیات انجام می‌شوند.
annotation نباید برای خواندن تصادفی دیتابیس در getter به کار برود.

## ۵. شکل ProblemDetails

```json
{
  "type":"https://example.test/problems/input",
  "title":"Validation failed",
  "status":400,
  "errors":{"title":["Title must contain 3-120 characters after trimming."]}
}
```

این JSON فقط نمایش آموزشیِ ساختار است، نه پاسخ byte-for-byte سرور و نه URL واقعی پروژه.
type نوع مسئله، title شرح کوتاه و status کد HTTP را بیان می‌کند.
ValidationProblemDetails بخش errors را برای fieldها اضافه می‌کند.
detail، instance و extensionهای trace نیز می‌توانند وجود داشته باشند.
type و متن پیش‌فرض framework را با نام دامنهٔ ساختگی خودمان در کد جایگزین نکرده‌ایم.

## ۶. خطای guard با همان خانوادهٔ پاسخ

```csharp
ModelState.AddModelError("title", "Title must contain 3-120 characters after trimming.");
return ValidationProblem(ModelState);
```

این مسیر خطای title پس از normalization را برمی‌گرداند.
کلید title کمک می‌کند UI پیام را کنار ورودی درست بگذارد.
در خطای binding ممکن است کلید errors نام property، body یا مسیر JSON باشد.
client نباید همهٔ خطاهای JSON syntax را حتماً زیر title فرض کند.
برای قرارداد پیچیده‌تر به error code پایدار و schema مستند نیاز خواهیم داشت.

## ۷. خطای منبع ناموجود

```csharp
return Problem(statusCode: 404, title: "Ticket not found");
```

این ValidationProblemDetails نیست؛ body فرم معتبر دارد ولی منبع وجود ندارد.
فهرست بدون نتیجه همچنان ۲۰۰ با آرایهٔ خالی است.
Id غیرمثبت ۴۰۰ و route متن غیرعددی با constraint درس قبل ۴۰۴ می‌دهد.
یک schema مشابه برای failureها، معنی statusها را حذف نمی‌کند.

## ۸. AddProblemDetails و status code pages

متد AddProblemDetails سرویس نوشتن مشکل را ثبت می‌کند.
UseStatusCodePages می‌تواند پاسخ خطای بدون body مانند مسیر ناموجود را به body مسئله تبدیل کند.
این middleware پاسخ موفق را به خطا تبدیل نمی‌کند و body موجود را دوباره نمی‌نویسد.
انتخاب قالب پاسخ یا `content negotiation` به header برابر `Accept` و writerهای ثبت‌شده بستگی دارد؛ برنامه باید قالبی پیدا کند که بتواند بنویسد و درخواست‌کننده آن را بپذیرد.
تست‌های این دوره درخواست‌های JSON معمولی را بررسی می‌کنند؛ ادعای خروجی یکسان برای هر Accept نداریم.

## ۹. ۴۱۵ جدا از ۴۰۰

اگر endpoint JSON می‌خواهد و text/plain بفرستی، media type نامناسب می‌تواند ۴۱۵ بدهد.
اگر Content-Type از نوع JSON مناسب باشد ولی متن JSON نحوی خراب باشد، ۴۰۰ است.
برای تست body خالی نیز Content-Type را application/json بگذار؛ ابزار client ممکن است POST بدون تنظیم header را با media type فرم بفرستد و پاسخ ۴۱۵ بگیرد.
اگر درخواست به مسیر ناشناخته برسد، قبل از بررسی عنوان می‌تواند ۴۰۴ باشد.
قرارداد ورودی را با Content-Type مناسب تست کن تا علت خطا مشخص بماند.

## ۱۰. بررسی failure بدون تغییر state

قبل از تست‌ها یک Ticket معتبر بساز و فهرست را نگه دار.
null، body خالی، JSON خراب، عنوان عددی، عنوان دوحرفی و عنوان ۱۲۱حرفی بفرست.
status و Content-Type و errors را بررسی کن.
فهرست پس از خطاها باید همان رکوردها را داشته باشد.
اولین create معتبر بعدی نباید Idهایی را به دلیل validation ناموفق از دست داده باشد.

## ۱۱. خطاهای مورد انتظار و exception

ورودی خراب یک نتیجهٔ متداول است و نباید با throw عمومی تا ۵۰۰ بالا برود.
در مقابل خرابی غیرمنتظرهٔ زیرساخت را نباید «عنوان نامعتبر» جا زد.
catch(Exception) و return BadRequest برای همهٔ failureها معنی API را خراب می‌کند.
درس بعد exception غیرمنتظره را در مرز HTTP به پاسخ عمومی ۵۰۰ تبدیل می‌کند.
جزئیات فنی برای log سرور می‌ماند.

## ۱۲. معیار طول متن

عضو string.Length تعداد واحدهای UTF-16 است و لزوماً تعداد کاراکتر دیداری کاربر نیست.
emoji و بعضی ترکیب‌های نوشتاری می‌توانند شمارش متفاوتی داشته باشند.
این درس از سیاست سادهٔ UTF-16 مدل قبلی استفاده می‌کند.
اگر معیار محصول تعداد `grapheme`، یعنی واحدهایی است که کاربر به شکل یک نویسه می‌بیند، قانون domain، رابط کاربر و ذخیره‌سازی باید با همین معیار هماهنگ شوند؛ عوض‌کردن فقط شمارش داخل DTO کافی نیست.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/27-validation/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5280
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5280/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5280/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5280/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/27-validation/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

برای هر ردیف جدول مرزها یک درخواست و نتیجهٔ واقعی ثبت کن.
در body خطا status و errors را بررسی کن، ولی تست را به جملهٔ دقیق انگلیسی framework گره نزن.
یک DTO برای note با حداقل و حداکثر طول طراحی کن؛ هنوز endpoint نهایی آن را اضافه نکن.
مشخص کن whitespace و raw خیلی بلند در سیاست تو چه نتیجه‌ای دارند.

## تمرین تکمیلی

گزینهٔ InvalidModelStateResponseFactory را در شاخهٔ تمرینی بررسی کن.
اگر آن را سفارشی می‌کنی status ۴۰۰، errors و media type را حفظ کن.
customization نباید همهٔ خطاها را به ۲۰۰ تبدیل کند.
برای تغییر شکل عمومی پاسخ یک تصمیم کوتاه در README ثبت کن.

## معیار عبور

- [ ] می‌دانم چرا گاهی action برای ورودی خراب اجرا نمی‌شود.
- [ ] validation raw و normalized را جدا بررسی کرده‌ام.
- [ ] ValidationProblemDetails دارای errors است.
- [ ] ۴۰۰، ۴۰۴ و ۴۱۵ علت یکسان ندارند.
- [ ] پس از درخواست نامعتبر داده و Id جدید ساخته نمی‌شود.

## منابع رسمی

- [MVC model validation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0)
- [Record model binding](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0)
- [Problem Details — RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html)
