# آماده‌سازی PostgreSQL برای درس‌های ۳۵ تا ۴۰

این بخش با PostgreSQL 18 و Npgsql 10.0.3 روی net10.0 بررسی می‌شود.
SDK 10 و دسترسی به یک PostgreSQL واقعی لازم است؛ SQLite یا fake جای این آزمایش‌ها نیست.
برای راه‌اندازی یکی از دو مسیر زیر کافی است.

## مسیر اول: PostgreSQL نصب‌شده

از [دانلود رسمی Windows](https://www.postgresql.org/download/windows/) نصب سازگار با PostgreSQL 18 را تهیه کن.
password حساب مدیریتی را خودت انتخاب کن و در فایل repository ننویس.
SQL Shell یا psql را با حساب مدیریتی باز کن و یک محیط تمرین جدا بساز:

```sql
CREATE ROLE course LOGIN PASSWORD 'course-local-only';
CREATE DATABASE supportdesk_lab OWNER course;
```

این نام و password فقط نمونهٔ محلی‌اند، نه credential مناسب سرویس عمومی.
دستورها یک بار اجرا می‌شوند؛ اگر role یا database از قبل هست، خطا را بخوان و آن را بی‌دلیل حذف نکن.
در این مسیر فرض پورت محلی ۵۴۳۲ است؛ نصب تو ممکن است پورت دیگری داشته باشد.

در PowerShell همان ترمینالی که dotnet را اجرا می‌کنی:

```powershell
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Port=5432;Database=supportdesk_lab;Username=course;Password=course-local-only;Timeout=10;Command Timeout=20"
```

اگر password یا پورت دیگری انتخاب کرده‌ای مقدار واقعی session را متناسب بده.
فقط در database تمرینی که مالک آن هستی اجرا کن؛ runner برای ساخت schema نیاز به دسترسی دارد.

## مسیر دوم: Docker محلی

اگر Docker Desktop آماده و Linux containers فعال است:

```powershell
docker run --name supportdesk-pg -e POSTGRES_USER=course -e POSTGRES_PASSWORD=course-local-only -e POSTGRES_DB=supportdesk_lab -p 127.0.0.1:55432:5432 -d postgres:18
```

پورت فقط روی loopback میزبان bind شده است.
۵۵۴۳۲ پورت host و ۵۴۳۲ پورت داخل container است؛ با مسیر نصب محلی اشتباه نکن.
نقش ساخته‌شده با POSTGRES_USER در image رسمی مدیریتی است؛ این shortcut محیط محلی الگوی مجوزدهی production نیست.
اگر container همنام از قبل هست، ابتدا با docker ps -a وضعیتش را ببین؛ run دوباره همان نام را ایجاد نمی‌کند.

```powershell
docker logs supportdesk-pg
docker exec supportdesk-pg pg_isready -h 127.0.0.1 -U course -d supportdesk_lab
```

باید server آمادهٔ اتصال TCP باشد؛ صرف برگشتن شناسهٔ container کافی نیست. گزینهٔ -h را حذف نکن: بررسی socket محلی ممکن است server موقت مرحلهٔ initialization را ببیند. pg_isready فقط وضعیت پذیرش اتصال را می‌سنجد؛ درستی password و مجوز query را با بررسی اتصال پایین ثابت کن.
سپس:

```powershell
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Port=55432;Database=supportdesk_lab;Username=course;Password=course-local-only;Timeout=10;Command Timeout=20"
```

در این فرمان volume برای دادهٔ ماندگار تعریف نشده است.
stop/start همان container و حذف/recreate آن را از نظر داده یکی فرض نکن.
برای این آزمایش دادهٔ مرجع را scriptها می‌سازند؛ این راهنما backup یا استقرار پایدار دیتابیس نیست.

## بررسی اتصال

اگر psql روی host داری:

```powershell
psql -h 127.0.0.1 -p 55432 -U course -d supportdesk_lab -c "SELECT version(), current_database(), current_user;"
```

در مسیر نصب محلی پورت را ۵۴۳۲ یا پورت واقعی خودت بگذار.
psql در صورت نیاز password را می‌پرسد؛ password واقعی را در command history قرار نده.
اگر psql میزبان نصب نیست، برای container:

```powershell
docker exec -it supportdesk-pg psql -U course -d supportdesk_lab
```

در psql، `\q` خروج و `\dt` فهرست جدول‌های قابل مشاهده است.
دستورهای SQL با سمی‌کالن تمام می‌شوند؛ prompt ناتمام ممکن است یعنی statement هنوز کامل نشده است.

## اجرای اولین آزمایش از ریشهٔ دوره

```powershell
dotnet run --project samples/34-relational/SupportDesk.csproj
```

برنامه یک schema با نام تولیدشده مثل lab35_... می‌سازد، SQLها را اجرا و نتیجه را بررسی می‌کند.
در حالت عادی فقط همان schema ساخته‌شدهٔ همین اجرا را در پایان حذف می‌کند.
schema دیگر و دادهٔ عمومی database هدف پاک نمی‌شوند.
این اجراها مخزن پایدار API نیستند؛ هر بار آزمایش مستقل تازه داریم.

برای نگه داشتن دادهٔ آزمایش و بررسی دستی:

```powershell
dotnet run --project samples/34-relational/SupportDesk.csproj -- --keep
```

نام دقیق schema در خروجی چاپ می‌شود.
در session psql با همان نام چاپ‌شده، search_path را تنظیم کن و query بزن؛ نام مثال زیر را جایگزین کن:

```sql
SET search_path TO lab35_name_printed_by_runner;
SELECT id, title, status FROM tickets ORDER BY id;
```

search_path در اتصال runner و session psql مستقل است.
--keep داده را برای بازرسی نگه می‌دارد؛ اجرای بعدی خودکار همان schema را مصرف نمی‌کند.
schemaهای نگه‌داشته‌شده را خودت پس از بررسی و با اطمینان از نامشان مدیریت کن.

## خطاهای متداول

| علامت | بررسی |
| --- | --- |
| Set SUPPORTDESK_PG | متغیر باید در همان session ترمینال تنظیم شود |
| connection failed | server، پورت host، role و password را بررسی کن |
| SQLSTATE 42501 | مجوز ساخت schema یا دسترسی database کافی نیست |
| relation does not exist در psql | database و search_path همان session را بررسی کن |
| duplicate table هنگام اجرای دستی SQL | فایل schema روی schema تازه اجرا شود |
| timeout یا انتظار lock | transaction باز sessionهای خودت را بررسی کن |

runner connection string یا password را در log چاپ نمی‌کند.
برای خطای نحوی SQL، فایل و SQLSTATE را بررسی کن و فقط روی دادهٔ آزمایشی debug انجام بده.

## بررسی کل این گروه

```powershell
python scripts/postgres_smoke.py
```

این فرمان هر شش نمونه را build و روی دیتابیس واقعی اجرا می‌کند.
`--build-only` فقط کامپایل را بررسی می‌کند و موفقیت آن شاهد درستی SQL یا isolation نیست.

پس از پایان آزمایش اگر به متغیر session نیاز نداری:

```powershell
Remove-Item Env:SUPPORTDESK_PG
```

## منابع رسمی

- [PostgreSQL Windows downloads](https://www.postgresql.org/download/windows/)
- [PostgreSQL version policy](https://www.postgresql.org/support/versioning/)
- [psql reference](https://www.postgresql.org/docs/18/app-psql.html)
- [Official image source](https://github.com/docker-library/postgres)
- [Npgsql connection strings](https://www.npgsql.org/doc/connection-string-parameters.html)
