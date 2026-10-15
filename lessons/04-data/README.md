# بخش چهارم — PostgreSQL و دسترسی به داده

شش درس ۳۵ تا ۴۰ آماده‌اند؛ شش درس بعدی ۴۱ تا ۴۶ و دو درس تحویل/ابزار ۴۷ و ۴۸ هنوز برنامه‌ریزی‌شده‌اند.
این بخش در مجموع ۱۴ درس دارد و هنوز کامل نشده است.
پیش‌نیاز این گروه، سه بخش اول و آماده‌سازی PostgreSQL است.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 035 | [مدل رابطه‌ای و کلید و constraint](035-relational/README.md) | نوشته‌شده |
| 036 | [SQL و CRUD پارامتری](036-sql-basics/README.md) | نوشته‌شده |
| 037 | [JOIN و aggregation](037-joins/README.md) | نوشته‌شده |
| 038 | [index و EXPLAIN و طرح query](038-indexes/README.md) | نوشته‌شده |
| 039 | [transaction و isolation](039-transactions/README.md) | نوشته‌شده |
| 040 | [طراحی schema برای درخواست و پیام](040-schema/README.md) | نوشته‌شده |
| 041 | EF Core و DbContext و provider | برنامه‌ریزی‌شده |
| 042 | migration و تکامل schema | برنامه‌ریزی‌شده |
| 043 | relationship و delete behavior | برنامه‌ریزی‌شده |
| 044 | tracking و projection و N+1 | برنامه‌ریزی‌شده |
| 045 | هم‌زمانی و optimistic concurrency | برنامه‌ریزی‌شده |
| 046 | مرز data access و بحث repository | برنامه‌ریزی‌شده |
| 047 | Dapper و SQL برای query هدفمند | برنامه‌ریزی‌شده |
| 048 | تحویل API پایدار و بررسی خرابی دیتابیس | برنامه‌ریزی‌شده |

## مسیر اجرا

[محیط PostgreSQL](../../handbook/postgresql-lab.md) را با نصب محلی یا Docker آماده کن.
هر درس یک project مستقل net10.0 با Npgsql 10.0.3 دارد.
runner در هر اجرا schema تازه می‌سازد و در پایان فقط همان schema را پاک می‌کند؛ --keep برای بازرسی دستی است.
فایل‌های SQL و سناریوهای C# در پوشهٔ نمونهٔ همان درس قرار دارند.

```powershell
python scripts/check_content.py
python scripts/postgres_smoke.py
```

بررسی زنده روی PostgreSQL 18 در job جدا اجرا می‌شود؛ build نمونه‌ها روی Windows نیز بررسی می‌شود.
--build-only فقط کامپایل است و هیچ تضمینی دربارهٔ اجرای SQL نمی‌دهد.
API درس ۳۴ هنوز حافظه‌ای است؛ اتصال API، EF Core و migration در ادامهٔ بخش انجام می‌شوند.

## مرزهای مهم

constraint ساده جای تمام قواعد Domain نیست.
SQLSTATE، row count و state پس از failure باید کنار هم بررسی شوند.
query با index لزوماً بهترین plan یا سریع‌تر از scan نیست.
transaction بدون isolation و پروتکل هماهنگ، همهٔ raceها را حل نمی‌کند.

[نقشهٔ کل دوره](../../ROADMAP.md)
