# درس 88: پیاده‌سازی مستقل و review

یک برش کوچک را مستقل پیاده کن. پیش‌نیاز: [درس قبل](../087-requirements/README.md). زمان پیشنهادی: دو تا چهار جلسه با تمرین مستقل.

[صورت مسئله](../../../capstone/BRIEF.md) | [پذیرش و rubric](../../../capstone/ACCEPTANCE.md) | [راهنمای اجرا و تحویل](../../../capstone/DELIVERY.md)

## از یک برش شروع کن

قبل از مرجع، create/read/claim را با قرارداد درس قبل بساز. ابتدا یک درخواست ثبت و خوانده شود؛ سپس Idempotency-Key و در پایان version و رقابت را اضافه کن. کل پروژهٔ نهایی را یک‌باره کپی نکن.

این snapshot عمداً حافظه‌ای و موقت است. Hosting/ClosedGate.cs در اجرای عادی هیچ کاربری را تأیید نمی‌کند؛ بنابراین مسیرهای محافظت‌شده 401 هستند و health/live تنها برای بررسی host است. این درس سرویس حساب واقعی نیست. هویت آزمایشگاهی فقط داخل test assembly تزریق می‌شود؛ افزودن اعتماد به X-Test-Actor در Program راه‌حل login نیست.

Application/MemoryDesk.cs داخل lock، تشخیص تکرار و تغییر version را اتمیک می‌کند. قفل همین process است و در چند instance یا restart دوام ندارد. DTO خروجی entity قابل تغییر نیست. تست رقابت، یک claim موفق می‌خواهد؛ اینکه همه 200 بگیرند نشانهٔ خطاست.

فایل‌ها را به ترتیب Domain، MemoryDesk، Program و سپس تست HTTP بخوان. در review بررسی کن tenant از actor بیاید، key برای هر owner جدا باشد و پاسخ مشترک به object قابل تغییر داخلی اشاره نکند. پس از فهم این مرحله به snapshot مستقل ۸۹ برو؛ آن نسخه دارای login واقعی و PostgreSQL است.

تمرین مستقل: فهرست owner را با ترتیب پایدار و size محدود اضافه کن؛ peer و tenant دیگر را تست کن. معیار عبور: دو تست موجود پاس و تست مستقل جدید بدون واردکردن test header به محصول.

## مرور را با دو شاهد شروع کن

در [تست HTTP](../../../tests/088-prototype/PrototypeTests.cs) این دو مسیر را دنبال کن:

1. `Normal_host_never_trusts_test_identity_header`: host عادی ساخته می‌شود؛ X-Test-Actor فرستاده می‌شود ولی 401 می‌گیریم. پس header، ورودی ورودِ محصول نیست.
2. `Idempotency_and_cross_tenant_and_racing_claim_are_explicit`: فقط TestFactory هویت مصنوعی را ثبت می‌کند. همان key دوباره همان Id می‌دهد؛ actor سازمان دیگر 404 می‌گیرد؛ شش claim با version یکسان فقط یک موفقیت دارند.

قبل از اجرای تست، جدول نتیجهٔ هر درخواست را بنویس. سپس [MemoryDesk](../../../samples/87-servicedesk-prototype/Application/MemoryDesk.cs) را بخوان و مشخص کن چرا بررسی version و تغییر state باید داخل همان lock باشند. بیرون‌بردن یکی از این دو از lock، شرط و تغییر را از هم جدا می‌کند.

اگر فقط dotnet run کنی و 401 بگیری، نمونه خراب نشده است؛ برای تجربهٔ این قابلیت در این مرحله تست را اجرا کن. login واقعی در درس ۸۹ اضافه می‌شود. تمرین فهرست این درس نیز باید هویت ساختگی را فقط در تست دریافت کند.

## فرمان از ریشهٔ مخزن

```powershell
dotnet build samples/87-servicedesk-prototype/SupportDesk.csproj
dotnet test tests/088-prototype/SupportDesk.Tests.csproj
```

[فهرست بخش](../README.md) | [درس بعد](../089-hardening/README.md)
