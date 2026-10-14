# درس ۰۲۲ — تحویل و بازبینی SupportDesk CLI

[قبلی](../021-refactoring/README.md) · [خانه](../../../README.md) · [راهنمای اجرای محصول آموزشی](../../../samples/21-supportdesk-cli/README.md)

## خروجی درس

نسخهٔ یکپارچهٔ CLI را اجرا، بررسی و publish می‌کنی.
می‌توانی مسیر یک عملیات را از presentation تا فایل توضیح بدهی.
بخش دوم با یک تحویل قابل اجرا و ارزیابی مستقل کامل می‌شود.
این نسخه هنوز API وب یا محصول چندکاربره نیست.

## پیش‌نیاز

تمام درس‌های بخش اول و دوم تا ۰۲۱.
SDK 10 برای build و publish لازم است.
برای خروجی framework-dependent، runtime .NET 10 روی مقصد لازم است.
NuGet جدیدی در این نمونه اضافه نشده است.

## ۱. scope تحویل

| قابلیت | قرارداد |
| --- | --- |
| Add | عنوان معتبر، اولویت Low/Normal/High |
| List | ترتیب صریح Id؛ فیلتر Open اختیاری |
| Find | Id مثبت و نتیجهٔ ناموجود مشخص |
| Rename | فقط Ticket بسته‌نشده |
| Note | متن ۳ تا ۵۰۰، حداکثر پنج، بدون تکرار Ordinal |
| Start | فقط Open به InProgress |
| Close | فقط InProgress با یادداشت حل مسئله |
| Delete | حذف صریح در CLI آموزشی، بدون reuse شناسه |
| Report | همهٔ وضعیت‌ها، حتی صفر |
| Persistence | JSON نسخه‌دار، تک‌process، فایل کوچک |
| Cancellation | token تا I/O؛ pre-cancel قابل بررسی |

یادداشت هر نوعی در این مدل، resolution محسوب می‌شود؛ این policy آموزشی است.
Delete در سرویس واقعی ممکن است مجوز، تأیید، soft delete یا audit لازم داشته باشد؛ اینجا فرمان صریح کاربر دادهٔ آزمایشی را حذف می‌کند.

## ۲. چیدمان نهایی

| مسیر | نقش |
| --- | --- |
| Domain/TicketTitle.cs | value معتبر عنوان |
| Domain/Ticket.cs | invariant وضعیت، یادداشت و تغییرات |
| Application/Outcome.cs | success و expected failure |
| Application/Contracts.cs | port ذخیره، state عملیات، DTO |
| Application/TicketService.cs | use caseهای async |
| Infrastructure/Document.cs | قرارداد JSON |
| Infrastructure/JsonTicketStore.cs | Load/Save و validation فایل |
| Presentation/ConsoleCommands.cs | فرمان، render و exit code |
| Presentation/InteractiveMenu.cs | دریافت تعاملی و EOF |
| Program.cs | مسیر فایل، اتصال و مالک لغو |

همه در یک csproj هستند؛ مرزهای پوشه‌ای را با مرزهای project reference یکی ندان.
فصل معماری جداسازی به چند assembly را عمیق‌تر بررسی می‌کند.

## ۳. direction و responsibility

Domain از Console، File یا DTO خروجی خبر ندارد.
Application به port ITicketStore وابسته است.
Infrastructure port را با JSON پیاده می‌کند.
Presentation service را صدا می‌زند و result را به پیام تبدیل می‌کند.
Program concreteها را می‌سازد.
هیچ model برای چاپ گزارش dependency به UI نمی‌گیرد.

## ۴. state هر عملیات

TicketService برای هر عملیات state را از store Load می‌کند.
تغییر را روی همان state متعلق به عملیات انجام می‌دهد.
در success، Save awaited می‌شود و بعد نتیجه برمی‌گردد.
در expected failure، Save اجرا نمی‌شود.
در failure storage، success چاپ نمی‌شود و منو متوقف می‌شود.
این قرارداد تک‌نویسنده است؛ دو process هم‌زمان می‌توانند lost update ایجاد کنند.

## ۵. helper مشترک محدود

ChangeAsync مراحل Id، Load، Find، callback و Save را برای چند تغییر هماهنگ می‌کند.
callback باید قبل از mutation قانون را بررسی کند.
Outcome failure به معنی عدم ذخیره است.
Add و Delete helper جدا دارند، چون generator یا حذف collection قواعد متفاوت دارند.
این extraction از رفتارهای واقعی آمده است؛ generic engine برای همهٔ عملیات نساخته‌ایم.

## ۶. domain همچنان guard دارد

application expected invalid state را به Outcome تبدیل می‌کند.
Ticket.Start و Close نیز invariant خودشان را enforce می‌کنند.
اگر application guard اشتباه داشته باشد، مدل بی‌صدا نامعتبر نمی‌شود.
exception غیرمنتظرهٔ مدل را با catch عمومی «همه چیز InvalidState است» پنهان نمی‌کنیم.
در محصول concurrent، این guard به تنهایی کنترل هم‌زمانی دیتابیس نیست.

## ۷. DTO و reference

TicketMapping از Notes آرایهٔ جدا می‌سازد.
تغییر ساختار آن آرایه توسط caller نباید collection داخلی Ticket را تغییر دهد.
این DTO هنوز یک مجموعهٔ immutable عمیقِ عمومی نیست؛ آرایهٔ خودش می‌تواند mutable باشد.
چون عنصرها string هستند، مدل با reference به شیء note mutable لو نمی‌رود.
در پروژه‌های بزرگ‌تر قرارداد DTO را برای نیاز مصرف‌کننده تحلیل می‌کنیم.

## ۸. schema فایل

Schema برابر supportdesk-v1 است.
NextId از نوع long و مستقل از تعداد Ticketها ذخیره می‌شود.
Tickets شامل Id، Title، Priority، Status و Notes است.
propertyهای لازم با JsonRequired حضورشان را الزام می‌کنند؛ null و invariantها جدا در Load بررسی می‌شوند.
این schema با فایل درس ۰۱۱ و registration-v1 درس ۰۱۹ یکسان نیست.
فایل قدیمی بدون migration رد می‌شود و overwrite نمی‌شود.

## ۹. بازسازی مدل

Load دادهٔ خارجی را به TicketTitle معتبر تبدیل می‌کند.
Ticket.Restore یادداشت‌ها را اضافه و مسیر Start/Close را طی می‌کند.
Status یا Priority ناشناخته، یادداشت تکراری، Closed بدون note و Id تکراری رد می‌شوند.
مدل فعلی side effect بیرونی در این متدها ندارد.
اگر بعداً domain event یا notification اضافه کردی، restore نباید آن‌ها را دوباره ارسال کند؛ مسیر بازسازی باید بازبینی شود.

## ۱۰. شناسه پس از حذف

NextId باید از هر Id موجود بزرگ‌تر باشد.
Delete آن را کاهش نمی‌دهد.
پس حذف آخرین Ticket و restart باعث reuse آن شناسه نمی‌شود.
ظرفیت تا int.MaxValue برای Id و int.MaxValue + 1 برای حالت ظرفیت تمام‌شدهٔ NextId است.
فایل دست‌کاری‌شده با NextId برخوردکننده رد می‌شود.

## ۱۱. محدودیت persistence

نوشتن مرحله‌ای فایل موقت خطر truncate مستقیم مقصد را کم می‌کند.
این ذخیره transaction دیتابیس، قفل بین processها یا تضمین تحمل قطع برق نیست.
همهٔ JSON به حافظه خوانده می‌شود؛ برای دادهٔ نامحدود مناسب نیست.
backup، migration عملی و concurrency در مراحل بعدی توسعه لازم‌اند.
این محدودیت‌ها بخشی از تحویل درست هستند، نه نکتهٔ پنهان.

## ۱۲. اجرای فرمان مستقیم

از ریشهٔ دوره:

```powershell
New-Item -ItemType Directory -Force exercises | Out-Null
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json add "Network issue" High
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json find 1
```

برای مسیر آزمایشی تازه انتظار Saved #1 و Found #1 داریم.
اولویت نام‌ها case-sensitive است؛ شمارهٔ enum را به جای نام نمی‌پذیریم.
عنوان quote شده در shell یک argument است.

## ۱۳. مسیر حل درخواست

```powershell
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json start 1
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json note 1 "Router configuration corrected"
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json close 1
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json report
```

اگر تنها همان Ticket وجود داشته باشد، گزارش Open: 0، InProgress: 0 و Closed: 1 است.
Close پیش از note باید رد شود و فایل تغییر نکند.
Rename یا note پس از Closed نیز رد می‌شود.

## ۱۴. منوی تعاملی

```powershell
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/supportdesk.json
```

بدون command، منو باز می‌شود.
UI تعاملی همان ConsoleCommands را صدا می‌زند؛ validation business جدا در منو دوباره اختراع نشده است.
EOF در هر prompt خروج بدون حلقهٔ سریع می‌دهد.
storage failure منو را متوقف می‌کند؛ expected input failure به منو برمی‌گردد.

## ۱۵. exit code

| code | معنی |
| --- | --- |
| ۰ | موفقیت |
| ۱ | خطای زیرساخت یا دادهٔ فایل |
| ۲ | syntax، ورودی یا وضعیت نامعتبر؛ ظرفیت تمام‌شده |
| ۳ | Ticket ناموجود |
| ۱۳۰ | لغو درخواست‌شده در policy این CLI |

برای فرمان مستقیم، این code به process برمی‌گردد.
در منو، خطای expected فقط نمایش داده می‌شود و کاربر ادامه می‌دهد؛ خروج عادی منو صفر است.
کد خروج command قبلی را با نتیجهٔ کلی جلسه یکی نگیر.

## ۱۶. cancellation

Ctrl+C در Program source لغو را فعال می‌کند.
Load و Save token می‌گیرند و پیش از جایگزینی فایل لغو بررسی می‌شود.
ReadLine در منو synchronous است؛ هنگام prompt ممکن است لازم باشد Enter بزنی تا check بعدی لغو را ببیند.
لغو بعد از مرز commit تضمین rollback نیست.

برای pre-cancel تکرارپذیر:

```powershell
dotnet run --project samples/21-supportdesk-cli/SupportDesk.csproj -- exercises/cancel-demo.json --cancel-demo
```

فایل تازه نباید ساخته شود؛ فایل موجود هم باید ثابت بماند.
کد خروج ۱۳۰ است.
این تست، timing لغو وسط I/O را اثبات نمی‌کند.

## ۱۷. publish خروجی قابل اجرا

```powershell
dotnet publish samples/21-supportdesk-cli/SupportDesk.csproj -c Release -p:UseAppHost=false -o exercises/published-supportdesk
dotnet exercises/published-supportdesk/SupportDesk.dll exercises/published-data.json report
```

این خروجی framework-dependent است و runtime .NET 10 روی مقصد لازم دارد.
کد publish شده نباید برای اجرا به پوشهٔ source وابسته باشد.
مسیر داده را جدا از پوشهٔ خروجی انتخاب کن تا build جدید داده را overwrite نکند.
خروجی self-contained برای هر RID بحث جداگانهٔ انتشار است؛ این درس آن را ادعا نمی‌کند.

## ۱۸. بررسی‌های خودکار تحویل

اسکریپت smoke نمونه‌های قبلی و جدید را build می‌کند.
سناریوهای ثبت، rename، note، transition، report، delete و restart را اجرا می‌کند.
JSON خراب، schema قدیمی، NextId برخوردکننده و Closed نامعتبر باید فایل را حفظ کنند.
pre-cancel و EOF نیز بررسی می‌شوند.
publish به پوشهٔ موقت و اجرای DLL منتشرشده جزو بررسی است.
نتیجهٔ آخرین commit را در تب Actions ببین.

## تمرین مستقل تحویل

پروژهٔ خودت را از روی کد مرجع کامل کپی نکن.
قابلیت‌های جدول scope را با مرزهای روشن پیاده کن.
یک README با روش اجرا، فرمان‌ها، schema، exit code و محدودیت تک‌نویسنده تحویل بده.
همراهش خروجی سناریوهای پذیرش را نگه دار.

## دفاع از طراحی

با اشاره به کلاس واقعی پروژهٔ خودت پاسخ بده:

- قانون عنوان در کجاست و چرا؟
- کنترل syntax کجاست و چرا domain آن را نمی‌فهمد؟
- Success چه زمانی ساخته می‌شود؟
- چرا DTO Notes از مدل جدا شده است؟
- چرا NextId جدا از Count ذخیره می‌شود؟
- اگر Save شکست خورد، چه چیزی به کاربر گفته می‌شود؟
- چه چیزی را پوشه جدا کرده و چه چیزی را کامپایلر هنوز enforce نمی‌کند؟

## معیار قبول پایان بخش

همهٔ سناریوهای پذیرش باید درست و مسیر یک عملیات قابل توضیح باشد.
فایل خراب هرگز به فایل خالیِ موفق تبدیل نشود.
expected failure و زیرساخت failure مسیرهای جدا داشته باشند.
نمونهٔ منتشرشده خارج از source اجرا شود.
تعداد کلاس‌ها و interfaceها را با نیاز توجیه کن.

## منابع رسمی

- [Dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
- [Application publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [Required JSON properties](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/required-properties)

## پایان بخش دوم

هر ده درس ۰۱۳ تا ۰۲۲ کامل شده‌اند.
درس ۰۲۳ آغاز HTTP و ASP.NET Core است و هنوز نوشته نشده است.
پیش از بخش وب، تحویل CLI و دفاع از ساختار کد خودت را کامل کن.
