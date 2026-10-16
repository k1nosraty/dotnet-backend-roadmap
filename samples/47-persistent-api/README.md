# نمونهٔ درس 048

[آموزش کامل](../../lessons/04-data/048-data-delivery/README.md)

.NET SDK 10 و PostgreSQL لازم است. SUPPORTDESK_PG را برای دیتابیس اختصاصی تنظیم کن.

```powershell
dotnet tool restore
dotnet run --project samples/47-persistent-api/SupportDesk.csproj -- --migrate
dotnet run --project samples/47-persistent-api/SupportDesk.csproj -- --urls http://127.0.0.1:5080
```

startup معمول migration نمی‌زند. اتصال و schema را بین اجراها ثابت نگه دار.
schema برخلاف آزمایش‌های قبلی خودکار حذف نمی‌شود. مدل EF این snapshot با schema خام درس ۴۰ متفاوت است.
مستندات endpointها، concurrency و خرابی در درس بالا قرار دارد.

## اجرای integration runner

python scripts/ef_smoke.py علاوه بر SUPPORTDESK_PG به psql نیاز دارد.
runner برای ساخت/پاک‌سازی schema خصوصیِ تست از متغیرهای SUPPORTDESK_TEST_PGHOST، SUPPORTDESK_TEST_PGPORT، SUPPORTDESK_TEST_PGDATABASE، SUPPORTDESK_TEST_PGUSER و SUPPORTDESK_TEST_PGPASSWORD استفاده می‌کند.
پیش‌فرض‌ها به‌ترتیب 127.0.0.1، 5432، supportdesk_lab، course و course-ci-only هستند؛ برای محیط محلی باید با اتصال آزمایش خودت هماهنگشان کنی.
این مقدارهای پیش‌فرض فقط آزمایش CI هستند و credential محصول نیستند.
خطای runner نباید باعث شود آن را به دیتابیس شخصی/محصول متصل کنی؛ یک دیتابیس تمرین اختصاص بده.
runner هرگز schema موجود را پاک نمی‌کند؛ نام خصوصی تصادفی می‌سازد و فقط همان را در finally پاک می‌کند.
