# درس 046 — مرز data access و بحث repository

پیش‌نیاز: درس 045 و [محیط PostgreSQL](../../../handbook/postgresql-lab.md).
خروجی این درس یک مثال قابل اجرا و یک تمرین مستقل است؛ صرفاً خواندن کد معیار عبور نیست.

جداسازی پوشه‌ها باید مرز مسئولیت را روشن کند، نه تعداد فایل‌ها را زیاد کند.
در این درس یک use case گزارش از دیتابیس پشت یک قرارداد کوچک قرار می‌گیرد.
Service دربارهٔ SQL یا DbSet تصمیم نمی‌گیرد؛ implementation دیتابیس دربارهٔ متن HTTP تصمیم نمی‌گیرد.

## مسیر درخواست گزارش

| فایل | نقش | وابستگی مجاز در این نمونه |
| --- | --- | --- |
| Application/TicketReport.cs | use case و query port و DTO | انواع خود Application |
| Persistence/EfTicketQueries.cs | query قابل ترجمه و materialization | EF Core و قرارداد Application |
| Persistence/DeskContext.cs | mapping و واحد کار | EF Core و entityهای Persistence |
| Program.cs | اتصال قطعه‌ها و آزمایش | تمام اجزا در composition root |

برای کوچکی مثال، interface و DTO و use case در یک فایل کوتاه‌اند.
وقتی هرکدام بزرگ شد، فایل جدا با نام همان type بساز؛ هر declaration الزاماً یک فایل مستقل نمی‌خواهد.
فایل بزرگ با چند مسئولیت را هم صرفاً برای کوتاه بودن مسیرها حفظ نکن.
در فصل معماری مرز assembly و پروژه‌های جدا را عمیق‌تر اعمال می‌کنیم.

## قرارداد محدود

```csharp
public interface ITicketQueries
{
    Task<TicketSummary[]> ListAsync(CancellationToken ct);
}
```

خروجی materialize شده است؛ IQueryable از مرز بیرون نمی‌رود.
اگر caller خودش Where و Include اضافه کند، جزئیات provider و خطای ترجمه و زمان اجرای query به لایهٔ بالاتر نشت می‌کنند.
CancellationToken جزئی از قرارداد است، چون latency و لغو به use case مربوط‌اند.
projection به DTO داخل implementation و پیش از ToArrayAsync انجام می‌شود.
سقف بیست ردیف در این آزمایش صریح است؛ برای محصول، قرارداد page/filter/result مناسب تعریف کن.
این متد ادعای گزارش تمام دیتابیس را ندارد.

## آیا همه‌جا Repository لازم است؟

DbContext خودش بسیاری از رفتارهای Unit of Work و DbSet بسیاری از رفتارهای Repository را دارد.
یک GenericRepository<T> که همان Add/Update/GetAll را با نام دیگری تکرار کند الزاماً ارزش ایجاد نمی‌کند.
اگر تمام queryها به Includeهای دلخواه و expression provider وابسته باشند، wrapper فقط پیچیدگی اضافه می‌کند.
استفادهٔ مستقیم از context در handlerهای کوچک با مرز روشن هم انتخاب قابل دفاعی است.
انتخاب باید با نیاز به queryهای مشترک، تست، پیچیدگی دامنه و مالکیت transaction توضیح داده شود.

## کجا port مفید است؟

وقتی یک use case قرارداد مشخصی مانند «صفحهٔ خلاصهٔ تیکت‌ها» می‌خواهد، interface کوچک هدف روشنی دارد.
می‌توان query را بدون تغییر use case از EF به SQL هدفمند منتقل کرد.
این به معنی یکسان بودن همهٔ semantics دو implementation نیست؛ ترتیب، null، collation و consistency باید قرارداد مشترک داشته باشند.
fake برای بررسی orchestration مفید است؛ query translation و FK را اثبات نمی‌کند.
آزمایش فعلی همین implementation واقعی را به PostgreSQL وصل می‌کند.

## مالک SaveChanges کیست؟

برای خواندن نیازی به SaveChanges نیست.
برای نوشتن، مرز commit باید با use case هماهنگ باشد.
Repository که در هر Add فوراً Save می‌کند، ساخت چند تغییر اتمی را دشوار می‌کند.
از طرف دیگر، سرویس عمومی Commit بدون قرارداد طول عمر می‌تواند state چند قابلیت را ناخواسته ذخیره کند.
در نمونهٔ نهایی port مربوط به Ticket یک aggregate را load و همان تغییر را اتمی commit می‌کند.
اگر دو aggregate باید با هم commit شوند، قرارداد واحد کار یا transaction صریح متناسب با آن use case لازم است.

## دامنه از persistence جداست

Domain نباید DbContext یا Controller یا HttpContext را بشناسد.
entityهای persistence برای materialization و FK و token طراحی شده‌اند.
DTO برای پاسخ مشتری طراحی شده و نباید graph tracked را با چرخهٔ navigation serialize کند.
Mapper محل تبدیل این شکل‌هاست؛ انتقال قواعد کسب‌وکار به mapper درست نیست.
در API نهایی، بازسازی دامنه دادهٔ ناسازگار را هم رد می‌کند و تغییر از متد دامنه عبور می‌کند.

## از نام پوشه دفاع کن

Services برای همهٔ کلاس‌ها معنا ندارد؛ TicketReport use case است و EfTicketQueries یک adapter دیتابیس.
Helperهای عمومی که context و request و domain را با هم می‌گیرند معمولاً مرزها را خراب می‌کنند.
ابتدا نقش را پیدا کن، سپس فایل را نام‌گذاری کن.
نام Repository به‌تنهایی وابستگی معکوس یا قابلیت تست ایجاد نمی‌کند.

## اجرای مرجع

از ریشهٔ مخزن و پس از تنظیم SUPPORTDESK_PG اجرا کن:

```powershell
dotnet run --project samples/45-data-access/SupportDesk.csproj
```

Lab یک schema تصادفی تازه می‌سازد و در پایان همان schema را پاک می‌کند.
با --keep در انتهای فرمان می‌توانی آن را برای بازرسی نگه داری؛ نام schema در خروجی چاپ می‌شود.
این گزینه ابزار مشاهده است، نه ذخیره‌سازی پایدار برنامه.
کد خروج صفر و PASS: lesson scenario یعنی assertionهای همین سناریو عبور کرده‌اند.
برای بررسی همهٔ این درس‌ها، python scripts/ef_smoke.py را با دیتابیس آماده اجرا کن.

## تمرین و معیار عبور

1. درخت فایل‌های این نمونه را با فلش وابستگی روی کاغذ توضیح بده؛ Application نباید به EF وابسته شود.
2. قرارداد را برای فیلتر Status گسترش بده و SQL واقعی را بررسی کن.
3. یک fake کوچک برای TicketReport بنویس و دقیقاً مشخص کن چه چیزی را ثابت نمی‌کند.
4. پیشنهاد GenericRepository را با همین query بررسی کن؛ کدام عملیات اضافه و کدام نیاز پنهان می‌شود؟

معیار عبور: محل interface، implementation، DTO و commit را بر اساس دلیل توضیح بدهی، نه تقلید یک نام معماری.

## منبع رسمی

[مستندات مرتبط](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

[Npgsql EF provider](https://www.npgsql.org/efcore/)

[بخش چهارم](../README.md) · [نقشهٔ دوره](../../../ROADMAP.md) · [درس بعد](../047-dapper/README.md)
