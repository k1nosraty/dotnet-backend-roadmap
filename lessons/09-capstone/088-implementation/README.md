# درس 88: پیاده‌سازی مستقل و review

یک برش کوچک را مستقل پیاده کن. پیش‌نیاز: [درس قبل](../087-requirements/README.md). زمان پیشنهادی: دو تا چهار جلسه با تمرین مستقل.

[صورت مسئله](../../../capstone/BRIEF.md) | [پذیرش و rubric](../../../capstone/ACCEPTANCE.md) | [راهنمای اجرا و تحویل](../../../capstone/DELIVERY.md)

## از یک برش شروع کن

قبل از مرجع، create/read/claim را با قرارداد درس قبل بساز. ابتدا یک درخواست ثبت و خوانده شود؛ سپس Idempotency-Key و در پایان version و رقابت را اضافه کن. کل پروژهٔ نهایی را یک‌باره کپی نکن.

این snapshot عمداً حافظه‌ای و موقت است. Hosting/ClosedGate.cs در اجرای عادی هیچ کاربری را تأیید نمی‌کند؛ بنابراین مسیرهای محافظت‌شده 401 هستند و health/live تنها برای بررسی host است. این درس سرویس حساب واقعی نیست. هویت آزمایشگاهی فقط داخل test assembly تزریق می‌شود؛ افزودن اعتماد به X-Test-Actor در Program راه‌حل login نیست.

Application/MemoryDesk.cs داخل lock، تشخیص تکرار و تغییر version را اتمیک می‌کند. قفل همین process است و در چند instance یا restart دوام ندارد. DTO خروجی entity قابل تغییر نیست. تست رقابت، یک claim موفق می‌خواهد؛ اینکه همه 200 بگیرند نشانهٔ خطاست.

فایل‌ها را به ترتیب Domain، MemoryDesk، Program و سپس تست HTTP بخوان. در review بررسی کن tenant از actor بیاید، key برای هر owner جدا باشد و پاسخ مشترک به object قابل تغییر داخلی اشاره نکند. پس از فهم این مرحله به snapshot مستقل ۸۹ برو؛ آن نسخه دارای login واقعی و PostgreSQL است.

تمرین مستقل: فهرست owner را با ترتیب پایدار و size محدود اضافه کن؛ peer و tenant دیگر را تست کن. معیار عبور: دو تست موجود پاس و تست مستقل جدید بدون واردکردن test header به محصول.

## فرمان از ریشهٔ مخزن

```powershell
dotnet build samples/87-servicedesk-prototype/SupportDesk.csproj
dotnet test tests/088-prototype/SupportDesk.Tests.csproj
```

[فهرست بخش](../README.md) | [درس بعد](../089-hardening/README.md)
