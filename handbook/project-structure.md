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
