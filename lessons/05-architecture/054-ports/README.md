# درس 054 — port، adapter و مرز Infrastructure

## خروجی قابل مشاهده

ResolveTicket بدون تغییر کد با دو adapter حافظه‌ای و فایل JSON اجرا می‌شود.
قرارداد مشترک، فقط نام متدها نیست: نبودن داده، تعارض، لغو و commit کامل هم بررسی می‌شوند.
دادهٔ خراب ذخیره‌شده هنگام load رد می‌شود و فایل با seed تازه جایگزین نمی‌شود.

پیش‌نیاز: [درس 053](../053-domain/README.md)، interface، async و concurrency.
پیاده‌سازی PostgreSQL درس ۴۸ همچنان مرجع ذخیرهٔ پایدار وب است.
این فایل‌ذخیره‌ساز یک آزمایش محدود برای مقایسهٔ adapterهاست.

## از نیاز برنامه به قرارداد برس

ResolveTicket باید یک تیکت را بخواند و تغییر نهایی را با نسخهٔ قبلی ذخیره کند.
برای این نیاز، Application یک port دارد:

```csharp
public interface ITicketStore
{
    Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct);
    Task<SaveOutcome> SaveAsync(
        TicketSnapshot expected, Ticket changed, CancellationToken ct);
}
```

مالک قرارداد Application است؛ جزئیات JSON و dictionary در امضای آن نیست.
Infrastructure این قرارداد را پیاده می‌کند.
Program implementation را انتخاب می‌کند و handler همان ITicketStore را دریافت می‌کند.
port ابزار پنهان کردن تمام تفاوت‌های جهان واقعی نیست؛ تضمین‌هایش باید نوشته و بررسی شوند.

## قرارداد رفتاری

| عملیات یا حالت | تعهد همین port |
| --- | --- |
| load ناموجود | null |
| save ناموجود | Missing؛ درج ضمنی انجام نمی‌شود |
| نسخهٔ قدیمی | Conflict؛ تغییر این caller ثبت نمی‌شود |
| save موفق | کل Ticket و افزایش یک نسخه |
| تغییر Id | ArgumentException |
| لغو پیش از commit | exception لغو، بدون commit |
| خرابی storage | خطای فنی؛ به Missing تبدیل نمی‌شود |

نسخه از snapshot داخلی می‌آید؛ request مشتری حق تنظیم آن را ندارد.
این port قرارداد ETag یا idempotency شبکه نیست.
تضمین save باید یک aggregate را پوشش دهد؛ save جداگانه برای هر note این تعهد را عوض می‌کند.

## دو adapter

| فایل در src/Infrastructure | نقش |
| --- | --- |
| InMemoryTicketStore.cs | dictionary و lock داخلی |
| JsonTicketStore.cs | خواندن فایل، بررسی نسخه و جایگزینی سند |
| StoreDocument.cs | شکل نسخه‌دار فایل و row ذخیره |
| StorageMapping.cs | تبدیل row معتبر به Domain و برعکس |
| StoreFactory.cs | ساخت implementation و بازگرداندن interface |

هر دو کلاس ذخیره داخلی‌اند؛ caller با factory، port دریافت می‌کند.
Application هیچ ProjectReference به Infrastructure ندارد.
JSON با کتابخانهٔ استاندارد System.Text.Json انجام می‌شود؛ package تازه‌ای اضافه نشده است.

## مسیر خواندن JSON

1. سند فایل را Deserialize کن.
2. FormatVersion باید ۱ و مجموعهٔ Tickets موجود باشد.
3. rowهای null، شناسهٔ تکراری و نسخهٔ غیرمثبت را رد کن.
4. StorageMapping از Ticket.Restore استفاده کند.
5. فقط snapshot معتبر به Application برسد.

JsonException به InvalidDataException تبدیل می‌شود.
خطای Domain هنگام بازسازی هم به خطای دادهٔ ذخیره‌شده تبدیل می‌شود.
فایل خراب با مجموعهٔ خالی جایگزین نمی‌شود؛ نبود فایل و خراب بودن فایل دو وضعیت متفاوت‌اند.
seed تنها برای اولین ساخت فایلِ ناموجود است.
Load بعدی فایل مفقود، خطای فنی می‌دهد و آن را خودکار بازسازی نمی‌کند.

## مسیر ذخیره

adapter زیر lock، فایل تازه را می‌خواند و Version را مقایسه می‌کند.
اگر نسخه مطابق بود، snapshot جدید را در dictionary محلی می‌گذارد.
سپس سند کامل در فایل موقت همان پوشه نوشته می‌شود و پس از بررسی لغو، به مسیر نهایی منتقل می‌شود.
فایل موقت در finally پاک می‌شود.
نوشتن مستقیم با File.WriteAllText روی فایل اصلی، در میانهٔ failure می‌تواند آن را ناقص کند.

```csharp
File.WriteAllText(temporary, JsonSerializer.Serialize(document));
ct.ThrowIfCancellationRequested();
File.Move(temporary, path, overwrite: true);
```

این الگو به‌تنهایی تضمین عمومیِ دوام در قطع برق، خرابی دیسک یا هر filesystem نیست.
همچنین commit موفق و دریافت پاسخ توسط caller دو اتفاق جدا هستند.
پس از نقطهٔ جایگزینی فایل، cancellation وعدهٔ rollback ندارد.

## مرز هم‌زمانی فایل

نمونه برای path کامل و یکسان، یک gate مشترک در همان process نگه می‌دارد.
دو instance در همان process نمی‌توانند هم‌زمان یک نسخه را ذخیره کنند.
این gate میان دو process مستقل یا دو server مشترک نیست.
مسیرهای alias و symlink هم قرارداد هماهنگی این آزمایش نیستند.
قفل داخل حافظه، جای قفل سیستم‌عامل یا concurrency دیتابیس را نمی‌گیرد.
برای محصول چندکاربره، تضمین‌های provider مناسب را جدا انتخاب و آزمایش کن.
تست reopen با process تازه، دوام معمولی را نشان می‌دهد؛ هماهنگی دو process هم‌زمان را ثابت نمی‌کند.

## اجرای مرجع

```powershell
dotnet run --project samples/53-ports-adapters/SupportDesk.csproj
dotnet run --project samples/53-ports-adapters/SupportDesk.csproj -- --check
```

خروجی عادی `Success: 1 Closed notes=1` است.
بررسی‌ها در پایان `PASS: port contracts, persisted adapter and corrupt-file preservation` می‌دهند.
برای مشاهدهٔ فایل واقعی، مسیر یک فایل آزمایشی را بده؛ پوشهٔ والد باید موجود باشد:

```powershell
dotnet run --project samples/53-ports-adapters/SupportDesk.csproj -- --file "$env:TEMP/ports-lab.json"
dotnet run --project samples/53-ports-adapters/SupportDesk.csproj -- --file "$env:TEMP/ports-lab.json"
```

اجرای اول در فایل تازه Success و `Stored: Closed notes=1 version=2` می‌دهد.
اجرای دوم Conflict می‌دهد چون تیکت بسته note تازه نمی‌پذیرد؛ فایل و نسخه ثابت می‌مانند.
این‌ها دو اجرای مستقل process هستند؛ فایل قبلی با seed بازنویسی نمی‌شود.
برای شروع آزمایش تازه، از نام فایل تازه استفاده کن.

```powershell
python scripts/architecture_next_smoke.py
```

این اسکریپت هر دو adapter را با سناریوهای یکسان و restart واقعی بررسی می‌کند.
آزمایش‌های دیتابیس بخش چهارم جدا اجرا می‌شوند.

## fake با adapter واقعی فرق دارد

RejectingStore قبلی برای ساخت Conflict یا IOException کنترل‌شده مفید بود.
اما serializing، نسخهٔ سند، خواندن فایل و نرمال‌سازی Domain را تست نمی‌کرد.
برای همین این درس هم contract test مشترک دارد و هم آزمون خاص فایل خراب.
برابریِ interface تضمین نمی‌کند هر adapter semantics درست دارد.
سرعت و رفتار failure حافظه، فایل و دیتابیس متفاوت‌اند.

## تمرین مستقل

یک adapter فقط‌خواندنی برای گزارش فایل طراحی کن.
اول تعیین کن آیا باید ITicketStore کامل را پیاده کند یا port خواندن کوچک‌تری لازم است.
متدی که هر write را NotSupported می‌کند الزاماً جایگزین درستی برای port نوشتن نیست.
برای دادهٔ duplicate و schema version ناشناخته سناریو بنویس.

### سرنخ

ابتدا caller و تعهدهای مورد نیازش را بنویس.
اگر متدی در interface هرگز نباید اجرا شود، مرز interface را دوباره بررسی کن.
به‌جای پنهان کردن تمام exceptionها، خطای فنی و نبودن منبع را جدا نگه دار.

## معیار عبور

- ResolveTicket در دو adapter بدون تغییر اجرا شود.
- snapshot قدیمی بازنویسی نشود.
- JSON خراب یا دادهٔ Domain نامعتبر حفظ و رد شود.
- اجرای دوبارهٔ process داده را حفظ کند.
- بتوانی محدودیت gate درون process و نبود تضمین قطع برق را توضیح بدهی.
- قرارداد port از JsonSerializer، path و dictionary مستقل بماند.

## منابع رسمی

تطبیق ابزارها در 2026-10-17، .NET 10:

- [مرز Application و Infrastructure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [File.Move و رفتار overwrite](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0)
- [اعتبار مدل بازسازی‌شده](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations)

[قبلی](../053-domain/README.md) · [بعدی](../055-mapping/README.md)
