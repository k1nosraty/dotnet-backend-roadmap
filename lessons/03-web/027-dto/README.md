# درس ۰۲۷ — DTO و serialization و mapping دستی

[قبلی](../026-controllers/README.md) · [خانه](../../../README.md) · [بعدی](../028-validation/README.md)

## مسئله و خروجی

مدل domain برای حفظ قواعد ساخته می‌شود و مدل JSON برای ارتباط با client.
اگر هر property تازهٔ domain مستقیماً به اینترنت برود، تغییر داخلی می‌تواند قرارداد عمومی را بی‌برنامه تغییر دهد.
در این درس request و response را جدا نگه می‌داریم، mapping را نام‌دار می‌کنیم و عملیات جایگزینی عنوان را اضافه می‌کنیم.
نمونه روی controller درس قبل بنا شده و همچنان حافظه‌ای است.

## ۱. سه شکل داده

| نوع | نمونه | مرز اعتماد |
| --- | --- | --- |
| CreateTicketRequest | title خام و nullable | دادهٔ نامطمئن client |
| Ticket / TicketTitle | Id و عنوان معتبر | منطق برنامه |
| TicketResponse | id و title خروجی | قرارداد خواندن client |

DTO همان entity با نام تازه نیست.
CreateTicketRequest عمداً Id ندارد چون Id را server می‌سازد.
Response عمداً setter برای تغییر state برنامه ندارد.
وجود قرارداد مجزا اجازه می‌دهد در آینده فیلد داخلی بدون تغییر API اضافه شود.

## ۲. قرارداد ورودی

```csharp
public sealed record CreateTicketRequest(string? Title);
public sealed record ReplaceTitleRequest(string? Title);
```

nullable بودن Title واقعیت ورودی غایب یا null را نشان می‌دهد.
پس از deserialize نباید فقط به نام type اعتماد کرد.
دو record فعلاً شبیه هم‌اند اما دو عملیات جدا را نمایندگی می‌کنند.
اگر بعداً create فیلد بیشتری بخواهد، مجبور نیستیم update را هم تغییر دهیم.
shared DTO فقط به خاطر یکسان بودن امروز propertyها تصمیم کافی نیست.

## ۳. mapping صریح

```csharp
public static TicketResponse ToResponse(this Ticket ticket) =>
    new(ticket.Id, ticket.Title);
```

این extension در Mapping/TicketMapping.cs قرار دارد.
controller پس از service از آن استفاده می‌کند.
Domain به TicketResponse یا System.Text.Json وابسته نمی‌شود.
Application هم برای تولید پاسخ HTTP ساخته نشده است.
Mapping اینجا یک انتخاب presentation است و جادوی runtime ندارد.

## ۴. مسیر نوشتن و خواندن

ورودی HTTP به request تبدیل می‌شود.
controller syntax و guard transport را بررسی می‌کند.
TicketService عنوان را به TicketTitle معتبر تبدیل می‌کند.
store یک Ticket immutable برمی‌گرداند.
mapping پاسخ مناسب client را می‌سازد.
serialization در پایان از پاسخ JSON تولید می‌کند.
هیچ مرحله‌ای به client اجازهٔ ساخت مستقیم entity ذخیره‌شده نمی‌دهد.

## ۵. پیش‌فرض JSON وب

پیش‌فرض وب ASP.NET Core با System.Text.Json نام propertyهای خروجی را camelCase می‌کند.
پس property C# با نام Id معمولاً در JSON به صورت id دیده می‌شود.
پیش‌فرض خواندن نام‌ها در این مسیر case-insensitive است؛ قرارداد مستندمان همچنان title است.
JSON با title عددی به string دلخواه ما تبدیل نمی‌شود و می‌تواند binding را شکست دهد.
در این پروژه converter سفارشی برای پذیرش هر نوع داده نداریم.
نام C# و نام بیرونی API را یکی فرض نکن؛ JsonPropertyName می‌تواند آن را صریح تعیین کند.

## ۶. property اضافه و overposting

```json
{"title":"Network issue","id":999,"status":"Closed"}
```

در پیش‌فرض این snapshot، property ناشناختهٔ JSON نادیده گرفته می‌شود.
Id ساخته‌شده در process تازه ۱ است، نه ۹۹۹.
status هم وارد domain نمی‌شود چون در DTO ورودی چنین propertyای نیست.
این رفتار به معنی validation کامل یا authorization نیست.
اگر سیاست محصول رد کردن property ناشناخته است، باید آن را صریح تنظیم و تست کنی.
بعداً با اضافه شدن status در response هم نباید خودبه‌خود آن را در CreateTicketRequest اضافه کنیم.

## ۷. چرا entity را FromBody نمی‌گیریم؟

با دریافت entity، شکل داخلی و شکل transport به هم گره می‌خورند.
ممکن است client فیلدهایی مثل OwnerId یا وضعیت داخلی بفرستد که مجاز به کنترلشان نیست.
DTO کوچک سطح نوشتن را مشخص می‌کند.
مجوز عملیاتی همچنان به کنترل هویت و مالکیت نیاز دارد که بخش امنیت دارد.
حذف یک property از DTO به تنهایی همهٔ مسیرهای دسترسی برنامه را ایمن نمی‌کند.

## ۸. جایگزینی عنوان

```http
PUT /api/tickets/1/title HTTP/1.1
Content-Type: application/json

{"title":"Printer issue"}
```

منبع هدف در این قرارداد زیرمنبع title است و کل نمایش قابل نوشتن آن فقط همین فیلد است.
این PUT ادعای جایگزینی همهٔ Ticket را ندارد.
موفقیت ۲۰۰ با TicketResponse می‌دهد.
Id ناموجود ۴۰۴ است و upsert انجام نمی‌شود.
body دارای Id اضافی نمی‌تواند route Id را عوض کند.

## ۹. اثر تکرار PUT

دو بار همان عنوان معتبر را به همان مسیر بفرست.
عنوان و Id نهایی باید یکسان بماند.
این عملیات append نمی‌کند و Id تازه نمی‌سازد.
نوشتن یک note با POST در پروژهٔ نهایی متفاوت خواهد بود.
انتخاب method باید با اثر واقعی عملیات هماهنگ باشد.

## ۱۰. guard قبل از service

عنوان با Trim نرمال و سپس طول ۳ تا ۱۲۰ بررسی می‌شود.
بعد TicketTitle.Create نیز invariant را حفظ می‌کند.
این دو لایه به دو ورودی متفاوت پاسخ می‌دهند: HTTP نامعتبر و caller داخلی اشتباه.
اگر قرار باشد قانون تغییر کند، سناریوهای مرزی هر دو را هماهنگ کن.
درس بعد اعتبارسنجی transport را با annotation و خطای استاندارد دقیق‌تر می‌کند.

## ۱۱. خطای serialization را کجا حل کنیم؟

اگر خروجی نام غیرمنتظره دارد، contract و تنظیمات serializer را بررسی کن.
برای رفع آن نام جدول دیتابیس را تصادفی عوض نکن.
اگر deserialize عدد به string شکست خورد، آن را با ۵۰۰ domain اشتباه نگیر.
اگر فیلدی نباید عمومی باشد، اول طراحی response را بازبینی کن.
JsonIgnore ابزار مفیدی است، اما جای مرز روشن DTO و entity را برای همهٔ نیازها نمی‌گیرد.

## ۱۲. mapping دستی یا کتابخانه؟

برای این دو property mapping دستی کوتاه، قابل جست‌وجو و کم‌هزینه است.
ابزارهایی مثل Mapster یا AutoMapper گزینه‌های ecosystem هستند؛ در این مرحله نصب نشده‌اند.
پیش از انتخاب در پروژهٔ واقعی نسخه، license، سازگاری، code generation و قابلیت debugging را بررسی کن.
صرف دیدن یک نام کتابخانه دلیل افزودن dependency نیست.
فعلاً با تغییر TicketResponse، compiler محل‌های mapping ناسازگار را نشان می‌دهد.

## بررسی عملی

یک Ticket با id اضافی در body بساز؛ Id واقعی را از پاسخ بخوان.
عنوان آن را با PUT تغییر بده و GET بعدی را بررسی کن.
همان PUT را تکرار و تعداد کل رکوردها را مقایسه کن.
عنوان کوتاه و Id ناموجود نباید رکورد تازه ایجاد کنند.
فقط id و title در response این درس وجود دارد.


## نمونهٔ قابل اجرا و ابزارهای لازم

پیش‌نیاز: درس‌های قبلی بخش سوم، SDK 10 و یک ترمینال دوم برای requestها.
از ریشهٔ repository:

```powershell
dotnet run --project samples/26-dto-mapping/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5270
```

ترمینال server را باز نگه دار؛ در ترمینال دوم:

```powershell
curl.exe -i http://127.0.0.1:5270/health
$body = @{ title = "Network issue" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5270/api/tickets -ContentType application/json -Body $body
curl.exe -i http://127.0.0.1:5270/api/tickets/1
```

اگر process تازه باشد Id نخست ۱ است.
دادهٔ هر نمونه مستقل و در حافظهٔ همان process است.
[فایل‌ها و تغییر مرحلهٔ نمونه](../../../samples/26-dto-mapping/README.md) را کنار این درس باز کن.
فرمان `python scripts/controller_smoke.py` همهٔ snapshotهای controller این بخش را بررسی می‌کند.
برای توقف server از Ctrl+C استفاده کن.

## تمرین مستقل

فیلد description اختیاری را فقط به قرارداد create اضافه کن و دربارهٔ نیاز به ذخیرهٔ آن تصمیم مستند بگیر.
اگر قرار است ذخیره شود، مسیر Domain و Store را آگاهانه تغییر بده؛ فقط اضافه کردن فیلد به DTO کافی نیست.
یک response کوچک برای فهرست طراحی کن و توضیح بده چرا ممکن است با جزئیات متفاوت باشد.
تست کن input اضافی نتواند Id رکورد را تغییر دهد.

## تمرین تکمیلی

برای نام بیرونی متفاوت از نام property C# از JsonPropertyName استفاده کن.
تفاوت JSON خروجی و شکل داخلی را ثبت کن.
تغییر نام بیرونی را به عنوان تغییر قرارداد ارزیابی کن، حتی اگر کد داخلی کامپایل شود.

## معیار عبور

- [ ] request، entity و response مسئولیت جدا دارند.
- [ ] mapping به Domain وابستگی معکوس تحمیل نمی‌کند.
- [ ] property اضافی در create اختیار Id نمی‌دهد.
- [ ] PUT عنوان را تکرار کرده‌ام و تعداد رکوردها ثابت مانده است.
- [ ] بدون نیاز واقعی کتابخانهٔ mapping نصب نمی‌کنم.

## منابع رسمی

- [System.Text.Json web defaults](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/configure-options)
- [JSON property names](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)
