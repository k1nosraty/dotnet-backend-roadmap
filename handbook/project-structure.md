# چیدمان کلاس‌ها و دایرکتوری‌ها

این فایل نقشهٔ مفهومی است؛ پیاده‌سازی و تمرین کامل هر مرز در درس مربوط انجام می‌شود.
ساختار را به اندازهٔ مسئله رشد می‌دهیم.

## مرحلهٔ اول: کنسول کوچک

در درس‌های اول Program.cs برای یک جریان کوتاه کافی است.
با اضافه شدن منو و persistence، کلاس‌ها با مسئولیت مشخص ساخته می‌شوند.
فهرست پیشنهادی برای مرحلهٔ بعد:

| مسیر | مسئولیت |
| --- | --- |
| Program.cs | شروع برنامه و اتصال اجزا |
| Tickets/Ticket.cs | داده و قواعد یک درخواست |
| Tickets/TicketService.cs | اجرای عملیات ثبت یا بستن درخواست |
| Persistence/JsonTicketStore.cs | خواندن و نوشتن فایل |
| ConsoleUI/TicketMenu.cs | نمایش و دریافت ورودی |

نام پوشه به تنهایی جداسازی ایجاد نمی‌کند؛ وابستگی واقعی کلاس‌ها تعیین‌کننده است.

## مرحلهٔ API کوچک

| مسیر | مسئولیت | نباید انجام بدهد |
| --- | --- | --- |
| Controllers/TicketsController.cs | HTTP، binding و تبدیل نتیجه به پاسخ | اجرای query پیچیده و قواعد کسب‌وکار |
| Features/Tickets/CreateTicketRequest.cs | قرارداد ورودی API | نگهداری secret یا entity دیتابیس |
| Features/Tickets/TicketResponse.cs | قرارداد خروجی API | افشای همهٔ ستون‌ها به صورت خودکار |
| Features/Tickets/TicketService.cs | هماهنگی use case و قواعد عملیات | وابستگی به HttpContext برای همهٔ کارها |
| Data/SupportDeskDbContext.cs | EF mapping و persistence | تصمیم دربارهٔ مجوزهای HTTP |
| Domain/Ticket.cs | invariantهای درخواست | ساخت پاسخ HTTP یا اتصال به دیتابیس |

برای use case ساده ممکن است یک handler کافی باشد؛ هر endpoint لزوماً به پنج کلاس احتیاج ندارد.

## مثال مسیر ثبت درخواست

controller ورودی HTTP را دریافت می‌کند.
اعتبارسنجی شکل ورودی، مثل عنوان خالی، انجام می‌شود.
application service هویت کاربر و قوانین عملیات را بررسی می‌کند.
مدل domain قانون وضعیت و تغییرات مجاز را اعمال می‌کند.
data access داده را ذخیره می‌کند.
controller نتیجه را به status code و DTO تبدیل می‌کند.

ترتیب دقیق transaction و persistence به use case وابسته است؛ این توضیح نسخهٔ عمومی برای همهٔ عملیات نیست.

## مرحلهٔ چند پروژه‌ای

| پروژه | محتوای نمونه | وابستگی مجاز در این انتخاب معماری |
| --- | --- | --- |
| ServiceDesk.Domain | entity، value object، قواعد | کتابخانه‌های پایهٔ .NET |
| ServiceDesk.Application | use case و قرارداد persistence | Domain |
| ServiceDesk.Infrastructure | EF، فایل و سرویس خارجی | Application و Domain |
| ServiceDesk.Api | endpoint و composition root | Application و Infrastructure |
| ServiceDesk.Tests | سناریوهای تست | پروژهٔ تحت تست |

Api در composition root به Infrastructure ارجاع می‌دهد تا implementationها را ثبت کند.
Application نباید برای یک query ساده مجبور به دانستن connection string شود.
اگر همهٔ پروژه‌ها به هم ارجاع بدهند، صرفاً کد را جابه‌جا کرده‌ایم.

## چه زمانی جداسازی لازم است؟

- کلاس به دو دلیل مستقل تغییر می‌کند: مثلاً هم پیام کنسول و هم فرمت فایل.
- تست قانون کسب‌وکار به وب‌سرور یا دیتابیس واقعی نیاز پیدا کرده است.
- یک use case در چند endpoint یا worker تکرار می‌شود.
- تغییر provider دیتابیس به بخش‌های نامرتبط نشت می‌کند.

هر interface باید یک مرز مفید داشته باشد؛ ساخت interface برای تمام کلاس‌ها قانون دوره نیست.
DbContext خودش قابلیت‌های unit of work و repository مانند دارد؛ wrapper عمومی را بدون نیاز اضافه نمی‌کنیم.

## انتخاب لایه یا قابلیت

ساختار لایه‌ای برای دیدن مرز فنی واضح است، اما فایل‌های یک قابلیت را پراکنده می‌کند.
ساختار feature محور فایل‌های تغییر مرتبط را کنار هم نگه می‌دارد.
در درس‌های ۰۴۹ تا ۰۶۰ یک قابلیت را با هر دو ساختار مقایسه می‌کنیم.

## تمرین مشاهده، بدون پیاده‌سازی زودهنگام

برای «بستن درخواست»، تعیین کن کدام بخش شناسهٔ HTTP، قانون وضعیت و ذخیرهٔ تغییر را می‌فهمد.
اگر class به Request و DbContext و Console هم‌زمان نیاز دارد، مسئولیت‌هایش را بررسی کن.
پاسخت را همراه یک دلیل بنویس؛ نام کلاس به تنهایی پاسخ نیست.

## ساختار واقعیِ تحویل بخش سوم

نمونهٔ اجرایی [SupportDesk API](../samples/33-supportdesk-api/README.md) همین مرزها را در یک Web project پیاده می‌کند:

| مسیر | مسئولیت واقعی در این نسخه |
| --- | --- |
| Controllers/TicketsController.cs | route، guard ورودی و تبدیل نتیجه به HTTP |
| Contracts | requestهای کوچک و response عمومی |
| Mapping/TicketMapping.cs | تبدیل entity به response |
| Application/TicketService.cs | اجرای use case با قرارداد store |
| Application/ITicketStore.cs | عملیات مورد نیاز برنامه از ذخیره‌سازی |
| Domain/Ticket.cs | invariant و مسیر مجاز تغییر وضعیت |
| Infrastructure/InMemoryTicketStore.cs | dictionary و هماهنگی mutation در process |
| Configuration/TicketOptions.cs | تنظیمات typed با validation شروع host |
| Hosting | مدیریت exception و header شناسهٔ درخواست |
| Program.cs | ثبت dependencyها و ترتیب pipeline |

مسیر نوشتن از Controller به Application و از قرارداد store به implementation می‌رسد.
Domain نه HttpContext می‌خواهد و نه connection string.
Infrastructure به Controller وابسته نیست و فقط قرارداد ذخیره و مدل domain را می‌شناسد.
Program.cs برای اتصال interface به implementation می‌تواند هر دو طرف را بشناسد.

[درس ۰۲۶](../lessons/03-web/026-controllers/README.md) دلیل معرفی این مرزها و [درس ۰۳۴](../lessons/03-web/034-api-delivery/README.md) دفاع و ارزیابی آن‌ها را دارد.
Data/DbContext جدول مفهومی بالا مربوط به مرحلهٔ دیتابیس آینده است؛ نمونهٔ بخش سوم هنوز حافظه‌ای است.
پوشه dependency را در زمان کامپایل محدود نمی‌کند؛ تفکیک assembly و تست معماری در بخش پنجم بررسی می‌شود.

## مرز واقعی دسترسی به داده در بخش چهارم

[درس ۴۶](../lessons/04-data/046-data-access/README.md) قرارداد query را در Application و implementation آن را در Persistence قرار می‌دهد. خروجی DTO materialize شده است و IQueryable از مرز عبور نمی‌کند.

در [API پایدار درس ۴۸](../lessons/04-data/048-data-delivery/README.md)، Controller فقط HTTP را ترجمه می‌کند؛ TicketService رفتار دامنه را اجرا می‌کند؛ ITicketData قرارداد ذخیره است؛ EfTicketData تغییر را با token و SaveChanges اتمی ذخیره می‌کند. TicketRow و NoteRow برای EF هستند و به‌جای DTO یا Domain استفاده نمی‌شوند.

پوشهٔ Migrations تاریخچهٔ schema است؛ Infrastructure/Lab در آزمایش‌های قبلی فقط مالک schema موقتی بود و جای adapter دیتابیس سرویس نیست. جداسازی فعلی منطقی و داخل یک assembly است؛ enforcement وابستگی‌ها با چند پروژه در بخش معماری می‌آید.

## آغاز معماری اجرایی

[درس ۴۹](../lessons/05-architecture/049-responsibilities/README.md) مسئولیت را با دلیل تغییر مشخص می‌کند؛ [درس ۵۰](../lessons/05-architecture/050-folders/README.md) همان رفتار را با چیدمان ترکیبی feature و لایه مقایسه می‌کند.

[درس ۵۱](../lessons/05-architecture/051-projects/README.md) Domain، Application و Infrastructure را به سه کتابخانه منتقل می‌کند و host فقط اتصال و نمایش را برعهده دارد. Domain reference پروژه ندارد؛ Application به Domain و Infrastructure به قرارداد Application و Domain وابسته است. host در composition root factory زیرساخت را می‌شناسد؛ implementation داخلی مستقیماً در دسترس آن نیست.

[درس ۵۲](../lessons/05-architecture/052-application-services/README.md) نمونهٔ orchestration با دو تغییر Domain و یک save اتمی است. ثبت یادداشت و بستن به دو commit جدا تبدیل نمی‌شوند. این snapshotها برای تمرکز بر معماری حافظه‌ای‌اند؛ دوام و SQL به آزمایش‌های PostgreSQL قبلی تعلق دارند.

## aggregate، adapter و featureهای اجرایی

[درس ۵۳](../lessons/05-architecture/053-domain/README.md) مرز تغییر Ticket و noteها و بازسازی معتبر را بررسی می‌کند. [درس ۵۴](../lessons/05-architecture/054-ports/README.md) همان use case را با دو adapter حافظه و JSON اجرا می‌کند؛ lock فایل فقط درون process است و جای concurrency دیتابیس نیست.

[درس ۵۵](../lessons/05-architecture/055-mapping/README.md) request، command، Domain، row فایل و response را با mapping صریح جدا می‌کند. [درس ۵۶](../lessons/05-architecture/056-vertical-slices/README.md) ساختار ترکیبی feature را دارد: endpoint در Features نام قابلیت و handler در Application/Features همان قابلیت است؛ Domain و adapter مشترک در assembly مستقل می‌مانند. این نمونه هنوز CQRS کامل یا تحویل TeamDesk نیست.

## تحویل معماری TeamDesk

[درس‌های ۵۷ تا ۶۰](../lessons/05-architecture/README.md) خواندن را با ITicketQueries و پاسخ‌های materialize شده از تغییر aggregate جدا می‌کنند. FluentValidation در Application و adapterها در Infrastructure می‌مانند؛ host محل composition و ترجمهٔ HTTP است. policy پروژه و assembly و ADRها در نمونهٔ درس ۵۹ قرار دارند.

[تحویل درس ۶۰](../lessons/05-architecture/060-teamdesk/README.md) CRUD و query و validation را یکپارچه و NextId فایل را مستقل از حذف حفظ می‌کند. این نسخه تک‌workspace و تک‌process آموزشی است؛ سازمان و کاربر و مجوز در فصل امنیت اضافه می‌شوند.
