# بخش چهارم — PostgreSQL و دسترسی به داده

تمام ۱۴ درس ۳۵ تا ۴۸ نوشته شده‌اند؛ خروجی بخش، API با ذخیره‌سازی PostgreSQL و migration و کنترل تعارض است.
پیش‌نیاز این گروه، سه بخش اول و آماده‌سازی PostgreSQL است.

| درس | موضوع | وضعیت |
| --- | --- | --- |
| 035 | [مدل رابطه‌ای و کلید و constraint](035-relational/README.md) | نوشته‌شده |
| 036 | [SQL و CRUD پارامتری](036-sql-basics/README.md) | نوشته‌شده |
| 037 | [JOIN و aggregation](037-joins/README.md) | نوشته‌شده |
| 038 | [index و EXPLAIN و طرح query](038-indexes/README.md) | نوشته‌شده |
| 039 | [transaction و isolation](039-transactions/README.md) | نوشته‌شده |
| 040 | [طراحی schema برای درخواست و پیام](040-schema/README.md) | نوشته‌شده |
| 041 | [EF Core و DbContext و provider](041-ef-core/README.md) | نوشته‌شده |
| 042 | [migration و تکامل schema](042-migrations/README.md) | نوشته‌شده |
| 043 | [relationship و delete behavior](043-relationships/README.md) | نوشته‌شده |
| 044 | [tracking و projection و N+1](044-tracking/README.md) | نوشته‌شده |
| 045 | [هم‌زمانی و optimistic concurrency](045-concurrency/README.md) | نوشته‌شده |
| 046 | [مرز data access و بحث repository](046-data-access/README.md) | نوشته‌شده |
| 047 | [Dapper و SQL برای query هدفمند](047-dapper/README.md) | نوشته‌شده |
| 048 | [تحویل API پایدار و بررسی خرابی دیتابیس](048-data-delivery/README.md) | نوشته‌شده |

## مسیر اجرا

[محیط PostgreSQL](../../handbook/postgresql-lab.md) را با نصب محلی یا Docker آماده کن.
هر درس یک project مستقل net10.0 دارد؛ Npgsql 10.0.3 و EF Core 10.0.13 و provider 10.0.3 در نمونه‌های مرتبط pin شده‌اند.
نمونه‌های آزمایشی ۳۵ تا ۴۷ schema تازه می‌سازند و فقط همان را پاک می‌کنند؛ --keep برای بازرسی دستی است.
API درس ۴۸ با schema ثابت کار می‌کند و آن را خودکار حذف نمی‌کند.
فایل‌های SQL و سناریوهای C# در پوشهٔ نمونهٔ همان درس قرار دارند.

```powershell
python scripts/check_content.py
python scripts/postgres_smoke.py
python scripts/ef_smoke.py
```

بررسی زنده روی PostgreSQL 18 در job جدا اجرا می‌شود؛ build نمونه‌ها روی Windows نیز بررسی می‌شود.
--build-only فقط کامپایل است و هیچ تضمینی دربارهٔ اجرای SQL نمی‌دهد.
API درس ۳۴ یک snapshot حافظه‌ای باقی می‌ماند؛ نسخهٔ پایدار مستقل در درس ۴۸ قرار دارد.
اجرای ef_smoke به SDK و PostgreSQL و برای آزمایش API به psql نیاز دارد؛ تنظیمات آن در README نمونه آمده است.

## مرزهای مهم

constraint ساده جای تمام قواعد Domain نیست.
SQLSTATE، row count و state پس از failure باید کنار هم بررسی شوند.
query با index لزوماً بهترین plan یا سریع‌تر از scan نیست.
transaction بدون isolation و پروتکل هماهنگ، همهٔ raceها را حل نمی‌کند.

[نقشهٔ کل دوره](../../ROADMAP.md)
