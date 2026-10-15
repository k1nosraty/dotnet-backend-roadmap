# درس ۰۳۳ — OpenAPI و مصرف API

[قبلی](../032-rest/README.md) · [خانه](../../../README.md) · [بعدی](../034-api-delivery/README.md)

## مسئله و خروجی

client نباید فقط با حدس زدن URL و نوع JSON با API کار کند.
در این درس سند OpenAPI را از metadata endpointها تولید و محتوای آن را با رفتار واقعی مقایسه می‌کنیم.
کتابخانهٔ Microsoft.AspNetCore.OpenApi به دو snapshot آخر اضافه می‌شود.
ابزارهای UI و client generation را هم می‌شناسیم، اما برای این هدف dependency اضافی نصب نمی‌کنیم.

## ۱. OpenAPI چیست؟

OpenAPI یک قرارداد ماشین‌خوان برای توصیف مسیرها، methodها، ورودی‌ها، schemaها و پاسخ‌هاست.
این سند خودِ server یا ابزار اجرای request نیست.
Swagger UI و Scalar نمایشگرها و ابزارهای تعاملی مصرف سند هستند.
داشتن سند تضمین نمی‌کند implementation دقیقاً مطابق آن است؛ به تست رفتار نیاز داریم.

## ۲. package نمونه

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.13" />
```

نسخهٔ این snapshot روی 10.0.13 ثابت است و برای net10.0 بررسی می‌شود.
نسخه و dependencyها در NuGet رسمی هنگام تهیهٔ درس در ۲۰۲۶/۱۰/۱۵ بررسی شده‌اند.
این به معنی «همیشه آخرین نسخه» نیست؛ پس از گذشت زمان audit و بررسی سازگاری را تکرار کن.
نسخهٔ ۱۱ پیش‌انتشار را صرفاً چون عدد بزرگ‌تری دارد وارد پروژهٔ net10.0 نمی‌کنیم.
در زمان restore، TreatWarningsAsErrors هشدار آسیب‌پذیری dependency را هم قابل مشاهده نگه می‌دارد.

## ۳. ثبت و map

```csharp
builder.Services.AddOpenApi();
// بعد از Build و فقط برای محیط توسعه:
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

AddOpenApi سرویس تولید سند را ثبت می‌کند.
MapOpenApi endpoint خواندن سند را اضافه می‌کند.
نام سند پیش‌فرض v1 است و آدرس معمول این پروژه /openapi/v1.json خواهد بود.
نام سند v1 خودکار versioning مسیر API را اجرا نمی‌کند.
/api/tickets در این نمونه هنوز بدون version segment است.

## ۴. سند در Development

قبل از اجرای نمونه در ترمینال server:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
```

پس از شروع server در ترمینال دوم:

```powershell
$spec = Invoke-RestMethod http://127.0.0.1:5330/openapi/v1.json
$spec.openapi
$spec.paths.PSObject.Properties.Name
```

در Production همان URL در این نمونه ۴۰۴ است.
شرط Development ابزار محدود کردن سطح مستندات محلی است و authentication محسوب نمی‌شود.
اگر محیط را اشتباه تنظیم کنی، همان شرط هم رفتار دیگری دارد.

## ۵. metadata موفقیت و failure

```csharp
[ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
```

این attributeها در action Create وجود دارند.
خروجی موفق ساخته‌شده با CreatedAtAction باید با ۲۰۱ مستندشده منطبق باشد.
برای GetById، ۲۰۰ و ۴۰۰ و ۴۰۴ ثبت شده‌اند.
متد حذف ۲۰۴ بدون body دارد.
attribute رفتار اجرایی را تحمیل نمی‌کند؛ اگر کد ۲۰۰ برگرداند، metadata ۲۰۱ آن را جادویی اصلاح نمی‌کند.

## ۶. ActionResult و schema

ActionResult<T> به ابزار کمک می‌کند نوع payload موفق را بشناسد.
TicketResponse قرارداد خروجی است و entity داخلی نباید جای آن در schema عمومی ظاهر شود.
DataAnnotations propertyهای DTO کلاسی می‌توانند برای schema نیز metadata بدهند.
اما guard پس از Trim و قواعد domain همیشه از یک annotation ساده قابل استخراج نیستند.
برای همین محدودیت normalization و status transition را در README و مثال‌ها هم مستند می‌کنیم.

## ۷. جاهایی که باید در سند ببینی

| بخش سند | انتظار |
| --- | --- |
| paths /api/tickets | GET و POST |
| post responses | ۲۰۱ و ۴۰۰ |
| /api/tickets/{id} get | ۲۰۰ و ۴۰۴ و ۴۰۰ |
| components.schemas | نوع‌های request، response و page |
| query parameters | q، page و pageSize |
| diagnostics | نباید وجود داشته باشد |

آدرس {id:int} پیاده‌سازی در سند معمولاً با placeholder {id} نمایش داده می‌شود.
Constraint را جزئی از رشتهٔ URL واقعی client نکن.
نمایش schema را با JSON پاسخ واقعی مقایسه کن.

## ۸. exclusion مستندات

endpointهای diagnostics با ExcludeFromDescription علامت‌گذاری شده‌اند.
این کار فقط آن‌ها را از سند حذف می‌کند و به تنهایی endpoint را غیرفعال نمی‌کند.
غیرفعال بودن در Production نتیجهٔ شرط map محیط است.
پنهان کردن مسیر از UI مستندات جای مجوز دسترسی را نمی‌گیرد.

## ۹. UI تعاملی

AddOpenApi به تنهایی Swagger UI یا Scalar UI نصب نمی‌کند.
در این درس سند JSON را مستقیم می‌خوانیم و request را با PowerShell یا curl می‌فرستیم.
اگر UI اضافه می‌کنی، package و نسخهٔ سازگار و روش اتصال به همین سند را از مستندات رسمی همان ابزار بخوان.
دو generator متفاوت را بدون نیاز کنار هم نصب نکن.
در پروژهٔ واقعی هزینهٔ dependency و دسترسی محیطی ابزار را هم ارزیابی کن.

## ۱۰. مصرف API و تولید client

می‌توان از سند برای تولید client استفاده کرد؛ NSwag و Microsoft Kiota از ابزارهای شناخته‌شدهٔ این حوزه‌اند.
client تولیدشده باید با نسخهٔ قرارداد، naming و authentication پروژه هماهنگ باشد.
خطای شبکه یا failure ۴۰۰ با deserialize موفق payload تجاری یکسان نیست.
برای نمونه ابتدا status و media type را بخوان و سپس مدل مناسب را deserialize کن.
مدیریت HttpClient، timeout و resilience در بخش مربوط آموزش داده می‌شود.

## ۱۱. بررسی وابستگی‌ها

```powershell
dotnet list samples/32-openapi/SupportDesk.csproj package --include-transitive
dotnet list samples/32-openapi/SupportDesk.csproj package --vulnerable --include-transitive
```

package مستقیم تنها dependency پروژه نیست؛ وابستگی غیرمستقیم هم باید بررسی شود.
برای عبور از هشدار شناخته‌شده audit را خاموش نکن.
در اجرای اولیهٔ این نمونه نسخهٔ پایه وابستگی آسیب‌پذیر داشت و patch سازگار جایگزین شد.
نسخهٔ نهایی کد و restore آن در CI بررسی می‌شود.
معنی advisory و محدودهٔ اثر را از منبع اصلی بخوان، نه فقط از رنگ هشدار.

## ۱۲. contract و breaking change

تغییر array به PageResponse، تغییر نام فیلد و حذف status پشتیبانی‌شده می‌توانند client را بشکنند.
صرف کامپایل شدن server معیار سازگاری client نیست.
تغییر additive هم باید با رفتار serializer و client واقعی ارزیابی شود.
برای محصول منتشرشده diff سند و تست consumer می‌تواند بخشی از review باشد.
در این دوره snapshotهای مستقل اجازه می‌دهند مراحل را بدون تغییر تاریخچه مقایسه کنی.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/32-openapi/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5330
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5330/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5330/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5330/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/32-openapi/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

سند را دانلود و requestBody عملیات POST و response ۲۰۱ را بررسی کن.
یک نمونهٔ request معتبر بساز و Location را دنبال کن.
سپس عنوان خراب بفرست و body ۴۰۰ را با خانوادهٔ ProblemDetails مقایسه کن.
اگر metadata و رفتار اختلاف داشتند، علت را در attribute یا implementation اصلاح کن.

## تمرین تکمیلی

برای endpoint count تمرینی metadata پاسخ موفق و failureهای ممکن را اضافه کن.
بررسی کن diagnostics با وجود فعال بودن در Development در سند دیده نشود.
طرح انتخاب UI یا client generator را با نیاز، نسخه، license و هزینهٔ نگهداری در چند خط ثبت کن.

## معیار عبور

- [ ] OpenAPI را با UI و server یکی نمی‌دانم.
- [ ] سند در Development قابل خواندن و در Production غایب است.
- [ ] metadata ۲۰۱ و ۴۰۴ را با HTTP واقعی مقایسه کرده‌ام.
- [ ] entity داخلی ناخواسته قرارداد عمومی نشده است.
- [ ] dependencyهای transitive و هشدارهای restore را بررسی می‌کنم.

## منابع رسمی

- [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
- [Microsoft.AspNetCore.OpenApi package](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.13)
- [OpenAPI metadata](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0)
- [Dependency advisory](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)
