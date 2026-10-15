# آزمایش PostgreSQL — درس ۳۵

[درس کامل](../../lessons/04-data/035-relational/README.md) · [آماده‌سازی server](../../handbook/postgresql-lab.md)

## هدف

مدل رابطه‌ای و کلید و constraint روی PostgreSQL واقعی.
این نمونه مستقل است و جدول‌های خود را در schema تازه می‌سازد.
Npgsql 10.0.3 و SDK 10 لازم است؛ connection string از SUPPORTDESK_PG می‌آید.

## اجرا از ریشهٔ دوره

```powershell
dotnet run --project samples/34-relational/SupportDesk.csproj
```

در حالت معمول schema تازهٔ همین اجرا در پایان پاک می‌شود.
با `-- --keep` پس از فرمان run می‌توان schema را برای بازرسی نگه داشت؛ نامش در خروجی است.
connection string، password و دادهٔ واقعی را در source یا README تمرین عمومی نگذار.

## فایل‌ها

- `Infrastructure/Lab.cs`
- `Program.cs`
- `Sql/01-schema.sql`
- `Sql/02-seed.sql`
- `Sql/03-queries.sql`
- `Sql/04-checks.sql`
- `SupportDesk.csproj`

## مرز نمونه

Infrastructure/Lab.cs ابزار راه‌اندازی و نمایش آزمایش است، نه service محصول.
فایل‌های SQL، برنامهٔ هر سناریو و assertionها کنار همان project هستند.
قواعدی که DDL enforce می‌کند در متن درس مشخص‌اند؛ constraint ساده جای تمام invariantهای Domain نیست.
این پروژه API بخش قبل را به PostgreSQL وصل نکرده است؛ آن تحویل در ادامهٔ بخش data انجام می‌شود.

## بررسی

```powershell
python scripts/postgres_smoke.py
```

این دستور تمام شش آزمایش را build و به server واقعی وصل می‌کند.
کد خروج صفر و PASS وابسته به موفق شدن سناریو و cleanup هستند.
