# درس 048 — تحویل API پایدار و بررسی خرابی دیتابیس

پیش‌نیاز: درس 047 و [محیط PostgreSQL](../../../handbook/postgresql-lab.md).
خروجی این درس یک مثال قابل اجرا و یک تمرین مستقل است؛ صرفاً خواندن کد معیار عبور نیست.

این تحویل، قرارداد HTTP بخش قبل را به PostgreSQL وصل می‌کند.
ساخت تیکت و یادداشت و تغییر وضعیت پس از بسته‌شدن process باقی می‌مانند.
migration از startup معمول جداست و تعارض دو writer به پاسخ 409 تبدیل می‌شود.
این نسخه آموزشی هنوز authentication و authorization ندارد؛ فصل‌های بعد آن‌ها را اضافه می‌کنند.
آن را پیش از تکمیل امنیت برای دادهٔ واقعی عمومی منتشر نکن.

## نقشهٔ فایل‌ها و مسئولیت

| مسیر | مسئولیت |
| --- | --- |
| Controllers/TicketsController.cs | ورودی HTTP، validation و status code |
| Contracts/ | request/response و صفحه‌بندی |
| Application/TicketService.cs | اجرای use case از طریق Domain و port |
| Application/ITicketData.cs | قرارداد ذخیره و snapshot نسخه‌دار |
| Domain/ | عنوان معتبر، رفتار تغییرناپذیر تیکت و قواعد note/status |
| Persistence/ | entityهای EF، context، adapter، طراحی اتصال ابزار |
| Migrations/ | نسخهٔ قابل review از schema و snapshot |
| Hosting/ | ProblemDetails و request ID |
| Program.cs | DI، pipeline و فرمان maintenance |

Domain به EF و ASP.NET وابسته نیست.
Controller نه SaveChanges می‌زند و نه TicketRow را برمی‌گرداند.
EfTicketData عملیات دیتابیس را async انجام می‌دهد و مدل قابل ذخیره را به مدل دامنه تبدیل می‌کند.
همهٔ فایل‌ها فعلاً در یک project هستند؛ این جداسازی منطقی است و enforcement بین assemblyها در بخش معماری می‌آید.

## نخستین راه‌اندازی پایدار

در محیط تمرین، دیتابیس اختصاصی طبق راهنمای PostgreSQL بساز و SUPPORTDESK_PG را تنظیم کن.
برای این API از یک database/schema ثابت استفاده کن؛ Lab موقتی درس‌های قبل را جایگزین ذخیره‌سازی نکن.
در اتصال تمرین، Search Path=public یا schema اختصاصیِ ازقبل‌ساخته‌شده را آگاهانه مشخص کن.
برای schema اختصاصی باید CREATE SCHEMA را یک بار با نقش مجاز انجام بدهی.

```powershell
dotnet tool restore
dotnet build samples/47-persistent-api/SupportDesk.csproj
dotnet ef migrations has-pending-model-changes --project samples/47-persistent-api/SupportDesk.csproj
dotnet run --project samples/47-persistent-api/SupportDesk.csproj -- --migrate
dotnet run --project samples/47-persistent-api/SupportDesk.csproj -- --urls http://127.0.0.1:5080
```

--migrate تغییرهای ثبت‌شده را اجرا می‌کند و بدون بالا آوردن HTTP خارج می‌شود.
اجرای دوبارهٔ آن نباید جدول یا تیکت را دوباره ایجاد کند.
بدون این فرمان، startup هیچ DDL اجرا نمی‌کند.
در production، migration با نقش maintenance و مجوز DDL جدا و runtime با حداقل مجوز DML اجرا می‌شود.
اتصال سادهٔ آزمایش با یک role، طراحی نهایی مدیریت credential محسوب نمی‌شود.
script idempotent درس ۴۲ را می‌توان برای review و اجرای کنترل‌شده تولید کرد.

## قرارداد HTTP

| عملیات | مسیر | پاسخ موفق | شکست عادی |
| --- | --- | --- | --- |
| ایجاد | POST /api/tickets | 201 و Location | 400 |
| فهرست | GET /api/tickets | 200 و PageResponse | 400، 503 |
| مشاهده | GET /api/tickets/{id} | 200 | 400، 404 |
| تغییر عنوان | PUT /api/tickets/{id}/title | 200 | 400، 404، 409 |
| یادداشت | POST /api/tickets/{id}/notes | 200 | 400، 404، 409 |
| وضعیت | PUT /api/tickets/{id}/status | 200 | 400، 404، 409 |
| حذف | DELETE /api/tickets/{id} | 204 | 400، 404، 409 |
| زنده بودن process | GET /health | 200 | process در دسترس نیست |
| آماده بودن دیتابیس | GET /health/ready | 200 | 503 |

قطع دیتابیس می‌تواند همهٔ مسیرهای وابسته به آن را با 503 متوقف کند.
شناسهٔ نامثبت و عنوان/یادداشت نامعتبر قبل از ذخیره رد می‌شوند.
سقف یادداشت پنج است؛ متن trim می‌شود، تکرار دقیق مجاز نیست و تیکت Closed تغییر عنوان/یادداشت نمی‌پذیرد.
Open فقط به InProgress و سپس با داشتن یادداشت به Closed می‌رود.
ارسال وضعیت برابر با وضعیت فعلی موفق و بدون تغییر معنایی است.
حذف سخت در این مرحله مجاز است و childها همراه parent اتمی حذف می‌شوند.

## یک تفاوت عمدی در جست‌وجو

q در این نسخه با Contains روی PostgreSQL ترجمه می‌شود و نسبت به حروف حساس است.
نسخهٔ حافظه‌ای از OrdinalIgnoreCase استفاده می‌کرد؛ این تغییر قرارداد را باید ببینی، نه اینکه مساوی فرض کنی.
برای جست‌وجوی بدون حساسیت، ILIKE با escaping wildcard و تصمیم روشن دربارهٔ collation/Unicode لازم است.
ترتیب فهرست Id است؛ count و صفحه در transaction خواندنی RepeatableRead از یک snapshot می‌آیند.
محاسبهٔ skip از long استفاده می‌کند و صفحهٔ بسیار بزرگ به‌جای overflow خروجی خالی دارد.
نام فیلد شمارش JSON برابر totalCount است.

## اتمی بودن تغییر aggregate

LoadAsync با یک SELECT، parent و noteها و Revision را می‌خواند.
Service تغییر را با متد دامنه تولید می‌کند؛ Domain حق رد کردن آن را دارد.
SaveAsync همان identity را بررسی می‌کند، noteهای تازه را اضافه می‌کند و token parent را تغییر می‌دهد.
SaveChanges یک transaction دارد؛ به تعارض خوردن parent نباید child را باقی بگذارد.
DbUpdateConcurrencyException و duplicate مورد انتظار به 409 تبدیل می‌شوند.
در DELETE هم تغییر concurrent و FK conflict پاسخ 409 می‌گیرند.
context پس از پایان درخواست dispose می‌شود و خطای writer بازنده را دوباره commit نمی‌کنیم.

این پروتکل حین واحد کار سرور از lost update جلوگیری می‌کند.
نسخهٔ مورد انتظار client در DTO نیست؛ ویرایش از صفحهٔ قدیمی را مانند ETag تشخیص نمی‌دهد.
تمام writerها باید token را رعایت کنند؛ SQL مستقیم می‌تواند سقف note و workflow را دور بزند.
DB constraint فقط حداقل‌های schema و FK و unique و status معتبر را enforce می‌کند.
طول .NET بر اساس UTF-16 و char_length در PostgreSQL بر اساس code point است؛ بازهٔ DB عمداً superset است.
بازسازی Domain از دادهٔ ذخیره‌شده هم اعتبار آن را بررسی می‌کند و corruption را موفقیت حساب نمی‌کند.

## خرابی دیتابیس و پاسخ امن

ready یک query کوچک روی جدول واقعی می‌زند؛ باز شدن connection به‌تنهایی کافی نیست.
اگر migration اجرا نشده باشد، /health همچنان 200 و /health/ready برابر 503 است.
اگر server خاموش باشد، درخواست دیتابیس با ProblemDetails عمومی و requestId جواب می‌گیرد.
handler نوع خطا و request ID را ثبت می‌کند و متن SQL/credential را به پاسخ نمی‌فرستد.
logging خام EF/Npgsql در تنظیمات نمونه خاموش است تا جزئیات command به log عادی نرود.
برای observability واقعی، در فصل مربوطه log و trace کنترل‌شده اضافه می‌کنیم.
این probe فقط خواندن یک جدول را بررسی می‌کند؛ تضمین سلامت همهٔ جدول‌ها، write permission یا migration history نیست.

پس از timeout هنگام commit، نتیجه ممکن است نامعلوم باشد.
به‌خصوص POST را کور retry نکن؛ این نسخه idempotency key ندارد و retry می‌تواند تیکت دوم بسازد.
ابتدا state را با شناسه/قرارداد مناسب reconcile کن؛ طراحی idempotency یک قابلیت جداست.

## سناریوی تحویل دستی

```powershell
$ticket = Invoke-RestMethod http://127.0.0.1:5080/api/tickets -Method Post -ContentType application/json -Body '{"title":"Persistent ticket"}'
Invoke-RestMethod "http://127.0.0.1:5080/api/tickets/$($ticket.id)/notes" -Method Post -ContentType application/json -Body '{"text":"First durable note"}'
```

process را با Ctrl+C ببند، با همان اتصال دوباره اجرا کن و GET همان Id را بزن.
تیکت و یادداشت باید باقی مانده باشند؛ پاک شدن process نباید داده را پاک کند.
خاموش کردن PostgreSQL یا حذف schema آزمایش باید readiness را خراب کند، نه اینکه دادهٔ حافظه‌ای جعلی برگرداند.

## بررسی خودکار تحویل

```powershell
python scripts/check_content.py
python scripts/ef_smoke.py
```

مسیر پروژهٔ مرجع: samples/47-persistent-api/SupportDesk.csproj.
این runner به SDK 10 و PostgreSQL واقعی و psql نیاز دارد؛ متغیرهای اتصال تست در README نمونه توضیح داده شده‌اند.
بررسی build-only برای Windows دیتابیس را اجرا نمی‌کند؛ job PostgreSQL لینوکس بررسی زنده را انجام می‌دهد.

## تمرین و معیار عبور

1. بدون migration اجرا کن: health باید 200، ready و درخواست دیتابیس 503 بدهند؛ سپس migration را اجرا کن.
2. ایجاد و note و وضعیت را انجام بده، process را ببند و پس از restart همان داده را بخوان.
3. چهار note بساز و چند POST هم‌زمان بفرست؛ تعداد نهایی هرگز نباید از پنج بیشتر شود.
4. دو تغییر هم‌زمانِ عنوان/وضعیت را بررسی کن و سیاست 409/reload را برای client توضیح بده.
5. schema قدیمی و server خاموش را جدا آزمایش کن و مطمئن شو پاسخ SQL و رمز ندارد.
6. از روی فایل‌ها مسیر یک درخواست note را از Controller تا transaction تشریح کن.

معیار عبور: علاوه بر کارکرد CRUD، دوام داده، rollback تعارض، قواعد Closed، صفحه‌بندی و شکست امن را با شاهد تحویل بدهی.
بخش چهارم پس از این تحویل کامل است؛ تکمیل امنیت، معماری و عملیات همچنان در درس‌های بعد لازم است.

## منبع رسمی

[مستندات مرتبط](https://learn.microsoft.com/en-us/ef/core/saving/transactions/)

[Npgsql EF provider](https://www.npgsql.org/efcore/)

[بخش چهارم](../README.md) · [نقشهٔ دوره](../../../ROADMAP.md)
