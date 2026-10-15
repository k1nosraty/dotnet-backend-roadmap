# درس ۰۲۳ — HTTP، status code و idempotency

[قبلی](../../02-design/022-cli-delivery/README.md) · [خانه](../../../README.md) · [بعدی](../024-web-host/README.md)

## خروجی درس

برای Ticket درخواست HTTP می‌فرستی و status، header و body پاسخ را می‌خوانی.
ایجاد و حذف منبع را با قرارداد روشن انجام می‌دهی.
safe و idempotent را از «پاسخ همیشه یکسان» جدا می‌کنی.
این آغاز بخش سوم است و روی نمونهٔ وب محلی اجرا می‌شود.

## پیش‌نیاز

بخش اول و دوم، SDK 10 و اجرای CLI.
نمونهٔ این درس API ثبت ساده است؛ منطق کامل وضعیت و JSON پایدار CLI هنوز منتقل نشده است.
controller در درس ۰۲۶ معرفی می‌شود.
برای مشاهدهٔ HTTP فعلاً Minimal API کوچک داریم.

## ۱. از فراخوانی متد به درخواست شبکه

در CLI مستقیماً service را صدا می‌زدیم.
در وب، client یک پیام درخواست به server می‌فرستد.
server آن را به handler مناسب هدایت و یک پیام پاسخ تولید می‌کند.
شبکه می‌تواند timeout یا قطع شود؛ client همیشه از نتیجهٔ عملیات server مطمئن نیست.
این نکته برای retry مهم خواهد بود.

## ۲. اجزای درخواست

یک نمایش متنی ساده‌شدهٔ درخواست:

```http
POST /tickets HTTP/1.1
Host: 127.0.0.1:5230
Content-Type: application/json
Accept: application/json

{"title":"Network issue"}
```

این قالب برای فهم پیام HTTP/1.1 است؛ HTTP/2 و HTTP/3 framing متفاوت دارند.
POST نام method است و /tickets هدف درخواست.
Content-Type نوع body ارسالی را می‌گوید.
Accept ترجیح client دربارهٔ representation پاسخ است، نه نوع body درخواست.
جزئیات framing واقعی را ابزار HTTP می‌سازد؛ Content-Length را دستی حدس نزن.

## ۳. اجزای پاسخ

```http
HTTP/1.1 201 Created
Location: /tickets/1
Content-Type: application/json; charset=utf-8

{"id":1,"title":"Network issue"}
```

status معنی نتیجه را اعلام می‌کند.
Location مسیر منبع ایجادشده را در این قرارداد معرفی می‌کند.
body نمایش JSON از دادهٔ نتیجه است.
متن reason phrase در نمایش آموزشی برای فهم است؛ قرارداد client را روی آن بنا نکن.

## ۴. method و قصد عملیات

| method | قصد متداول | نمونهٔ این درس |
| --- | --- | --- |
| GET | خواندن representation | فهرست یا یک Ticket |
| POST | پردازش/ایجاد طبق قرارداد مقصد | ساخت Ticket با Id تازه |
| PUT | جایگزینی وضعیت منبع طبق قرارداد | در این نمونه پیاده نشده |
| PATCH | تغییر بخشی طبق قرارداد patch | در این نمونه پیاده نشده |
| DELETE | حذف association منبع | حذف Ticket |

انتخاب method فقط نام‌گذاری ظاهری نیست.
خواندن با GET نباید به عنوان عملیات مورد انتظار کاربر Ticket تازه بسازد.
GET می‌تواند logging فنی داشته باشد، اما client نباید با آن درخواست حذف کند.

## ۵. statusهای مورد استفاده

| code | معنی در این نمونه |
| --- | --- |
| ۲۰۰ | خواندن موفق همراه body |
| ۲۰۱ | ساخت موفق همراه Location |
| ۲۰۴ | حذف موفق بدون body |
| ۴۰۰ | ورودی نامعتبر |
| ۴۰۴ | منبع یا مسیر پیدا نشده |
| ۴۰۵ | method برای مسیر مناسب نیست |
| ۴۱۵ | media type body پشتیبانی نمی‌شود |
| ۵۰۰ | failure غیرمنتظرهٔ server |

همهٔ شکست‌ها ۲۰۰ با متن error نیستند.
همهٔ ۴۰۴ها هم الزاماً از lookup دیتابیس نیامده‌اند؛ routing می‌تواند قبل از handler نتیجهٔ ۴۰۴ بدهد.
در درس ۰۲۵ این تفاوت را آزمایش می‌کنیم.

## ۶. safe

safe یعنی method از نظر قصد قراردادی client خواندنی است و تغییر business state درخواست نمی‌کند.
GET از methodهای safe است.
ساخت منبع با POST safe نیست.
این تعریف تضمین نبودن log، metric یا مصرف منابع در server نیست.
فرض «safe یعنی بدون هیچ side effect فنی» دقیق نیست.

## ۷. idempotent

اثر مورد انتظار اجرای چندبارهٔ یک درخواست idempotent باید معادل یک بار اجرای آن باشد.
DELETE و PUT طبق semantics استاندارد idempotent هستند؛ GET نیز safe و idempotent است.
این به معنی status و body یکسان در هر پاسخ نیست.
مثلاً حذف اول ۲۰۴ و حذف دوم ۴۰۴ می‌دهد، اما اثر نهایی هر دو «منبع وجود ندارد» است.

## ۸. POST تکراری

در این قرارداد، هر POST معتبر یک Id تازه می‌سازد.
اگر پاسخ گم شود و client همان POST را دوباره بفرستد، ممکن است دو Ticket ساخته شوند.
یکسان بودن متن JSON درخواست، idempotency خودکار ایجاد نمی‌کند.
پشتیبانی از idempotency key نیازمند قرارداد، ذخیرهٔ کلید و نتیجه، هم‌زمانی و مدت نگهداری است.
در این نمونه آن را پیاده نکرده‌ایم.
retry کورکورانهٔ عملیات نوشتن می‌تواند دادهٔ تکراری بسازد.

## ۹. حافظه و هم‌زمانی نمونه

TicketStore در DI به صورت singleton ثبت می‌شود تا درخواست‌ها دادهٔ مشترک همین process را ببینند.
متدهای lookup، snapshot و mutation برای این نمونه از lock استفاده می‌کنند.
Titleها value معتبر و responseها record با دادهٔ ساده‌اند.
این store چندسرور، دیتابیس یا persistence نیست و پس از restart پاک می‌شود.
کپی کردن JsonTicketStore تک‌نویسندهٔ CLI به handlerهای concurrent بدون بازطراحی مناسب نیست.

## ۱۰. فایل‌های نمونه

| فایل | نقش |
| --- | --- |
| Program.cs | ثبت routeها و انتخاب پاسخ |
| Contracts/CreateTicketRequest.cs | قرارداد JSON ورودی |
| Contracts/TicketResponse.cs | قرارداد JSON خروجی |
| Domain/TicketTitle.cs | عنوان معتبر |
| Infrastructure/TicketStore.cs | state حافظه‌ای demo |
| SupportDesk.csproj | Web SDK و target framework |

برای شروع HTTP، handlerها کوچک‌اند و serviceهای کامل CLI هنوز منتقل نشده‌اند.
در درس‌های controller و DTO این مرزها را با رشد پروژه عمیق‌تر می‌کنیم.

## ۱۱. اجرای server

از ریشهٔ دوره در PowerShell:

```powershell
dotnet run --project samples/22-http-basics/SupportDesk.csproj --no-launch-profile -- --urls http://127.0.0.1:5230
```

این ترمینال را باز نگه دار و در ترمینال دوم درخواست بفرست.
server مثل یک فرمان CLI پس از پاسخ اول تمام نمی‌شود.
آدرس loopback برای همین دستگاه است.
این نمونه local HTTP و فاقد احراز هویت است؛ راهنمای انتشار عمومی و HTTPS فصل‌های بعدی است.

## ۱۲. ساخت درخواست با PowerShell

```powershell
$body = @{ title = "Network issue" } | ConvertTo-Json
$response = Invoke-WebRequest -Method Post -Uri http://127.0.0.1:5230/tickets -ContentType application/json -Body $body
$response.StatusCode
$response.Headers.Location
$response.Content
```

انتظار ۲۰۱، مسیر /tickets/1 و JSON داده است، اگر process تازه باشد.
ConvertTo-Json جلوی خطاهای quote در ساخت دستی JSON را می‌گیرد.
در PowerShell قدیمی و جدید مدیریت پاسخ‌های غیرموفق فرق دارد؛ برای مشاهدهٔ خطا try/catch را به کار ببر.

## ۱۳. خواندن با curl روی Windows

```powershell
curl.exe -i http://127.0.0.1:5230/tickets
curl.exe -i http://127.0.0.1:5230/tickets/1
curl.exe -i http://127.0.0.1:5230/tickets/99
```

از curl.exe استفاده شده تا با alias احتمالی PowerShell اشتباه نشود.
-i headerها را هم نشان می‌دهد.
فهرست خالی باید JSON array خالی با ۲۰۰ باشد، نه ۴۰۴.
یک Ticket ناموجود ۴۰۴ است.

## ۱۴. حذف دوباره

```powershell
curl.exe -i -X DELETE http://127.0.0.1:5230/tickets/1
curl.exe -i -X DELETE http://127.0.0.1:5230/tickets/1
```

اولی برای رکورد موجود ۲۰۴ بدون body است.
دومی ۴۰۴ است.
یک GET بعدی باید ۴۰۴ بدهد و فهرست آن رکورد را نداشته باشد.
این مثال عملی تفاوت idempotency و پاسخ یکسان است.

## ۱۵. timeout و نتیجهٔ نامعلوم

timeout client ثابت نمی‌کند server عملیات را انجام نداده است.
ممکن است server ثبت کرده ولی پاسخ به client نرسیده باشد.
برای خواندن retry معمولاً ساده‌تر است؛ برای نوشتن باید قرارداد عملیات و idempotency بررسی شود.
قطع اتصال، rollback خودکار business operation نیست.

## تمرین مستقل

جدول method، مسیر، body، status موفق و failure هر endpoint را بنویس.
دو POST با عنوان یکسان بفرست و دو Id متفاوت را مشاهده کن.
سپس GET و DELETE تکراری را مقایسه کن.
در README توضیح بده کدام اثر تکرارشونده مجاز است و کدام نیاز به تصمیم محصول دارد.

## تمرین تکمیلی

برای PUT جایگزینی عنوان یک قرارداد روی کاغذ طراحی کن.
مشخص کن Id ناموجود ۴۰۴ می‌دهد یا اجازهٔ ایجاد دارد، و عنوان خراب چه status می‌دهد.
فقط نام PUT را روی عملیات append نگذار و ادعای idempotency نکن.
پیاده‌سازی این تمرین باید بعداً با قرارداد controller و validation هماهنگ شود.

## معیار عبور

- [ ] method، header و body را در درخواست می‌شناسم.
- [ ] ۲۰۱ را با Location بررسی کرده‌ام.
- [ ] ۲۰۴ body ندارد.
- [ ] safe و idempotent را جدا توضیح می‌دهم.
- [ ] DELETE تکراری با status متفاوت را تحلیل می‌کنم.
- [ ] POST تکراری را خودکار امن برای retry فرض نمی‌کنم.
- [ ] محدودیت حافظه‌ای و restart نمونه را می‌دانم.

## منابع رسمی

- [HTTP Semantics — RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html)
- [Minimal APIs quick reference](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)

## قدم بعد

به جای فقط مصرف endpoint، host، configuration و pipeline برنامهٔ ASP.NET Core را بررسی می‌کنیم.
