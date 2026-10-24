# درس 89: تست و امنیت و عملکرد

دوام و امنیت را با شکست واقعی اثبات کن. پیش‌نیاز: [درس قبل](../088-implementation/README.md). زمان پیشنهادی: دو تا چهار جلسه با تمرین مستقل.

[صورت مسئله](../../../capstone/BRIEF.md) | [پذیرش و rubric](../../../capstone/ACCEPTANCE.md) | [راهنمای اجرا و تحویل](../../../capstone/DELIVERY.md)

## حافظه را با دوام جایگزین کن

snapshot این درس مستقل و کامل‌تر است: Identity، PostgreSQL، claim، close، reopen و audit دارد. ترتیب مطالعه: Persistence/DeskContext.cs برای جدول و unique index؛ Hosting/Bootstrap.cs برای حساب‌های lab؛ Endpoints/DeskEndpoints.cs برای مرز HTTP؛ Application/DeskService.cs برای transaction؛ Domain برای قانون.

در create ابتدا ورودی معتبر می‌شود؛ request ذخیره می‌شود، audit ذخیره می‌شود و سپس transaction commit می‌شود. اگر audit ذخیره نشود، ذخیرهٔ اول هم برمی‌گردد. در رقابت، Version یک concurrency token است: UPDATE نسخهٔ قبلی را شرط می‌کند؛ اگر کسی زودتر تغییر داده باشد، نتیجه 409 است. unique index از create تکراری بین دو process جلوگیری می‌کند؛ lock حافظه این کار را نمی‌کند.

Tenant و IsAgent از رکورد فعلی کاربر می‌آیند. ارسال tenant در body به خاطر property ناشناخته 400 است؛ ساختن claim روی header هم auth نیست. test مجوز باید با token قدیمی پس از حذف نقش شکست بخورد. کوتاه بودن عمر token جایگزین بررسی مجوز فعلی نشده است.

تست PostgreSQL را با Docker واقعی اجرا کن. WebApplicationFactory مسیر HTTP را اجرا می‌کند، ولی TLS واقعی نیست؛ آن شاهد در درس بعد است. برای اجرای دستی حساب‌ها و تنظیمات از راهنمای تحویل استفاده کن؛ connection و key directory اجباری‌اند و startup با config ناقص عمداً شکست می‌خورد.

تمرین مستقل: note را با transaction و audit اضافه کن، سپس fault در ذخیرهٔ audit و race دو تغییر را بازتولید کن. معیار عبور: نه تست واقعی پاس، بدون skip؛ توضیح اینکه هر assertion کدام قانون را حفاظت می‌کند. هشدار امنیتی یا محدودیت باقی‌مانده را در گزارش خودت ثبت کن، نه با حذف تست.

## فرمان از ریشهٔ مخزن

```powershell
dotnet build samples/88-servicedesk-hardening/SupportDesk.csproj
dotnet test tests/089-capstone/SupportDesk.Tests.csproj
```

فرمان test بالا Docker فعال می‌خواهد؛ PostgreSQL واقعی با Testcontainers ساخته می‌شود.

[فهرست بخش](../README.md) | [درس بعد](../090-delivery/README.md)
