# درس 047 — Dapper و SQL برای query هدفمند

پیش‌نیاز: درس 046 و [محیط PostgreSQL](../../../handbook/postgresql-lab.md).
خروجی این درس یک مثال قابل اجرا و یک تمرین مستقل است؛ صرفاً خواندن کد معیار عبور نیست.

گاهی یک گزارش SQL مشخص داریم و کنترل مستقیم شکل query ارزشمند است.
Dapper متدهای extension برای اجرای فرمان و mapping ردیف به object فراهم می‌کند.
این کتابخانه schema و migration و ChangeTracker و قواعد Domain را مدیریت نمی‌کند.
در این درس EF دادهٔ آزمایشی را آماده می‌کند و Dapper فقط یک گزارش مستقل را می‌خواند.

## وابستگی‌ها و انتخاب

نسخهٔ مرجع این نمونه Dapper 2.1.66 است و در فایل پروژه pin شده؛ ادعای آخرین نسخه بودن ندارد.
اتصال PostgreSQL همچنان از Npgsql می‌آید.
برای آشنایی با کتابخانه، یک query روشن کافی است؛ لازم نیست همهٔ data access پروژه را بازنویسی کنی.
برای queryهای پیچیده، SQL صریح می‌تواند review و EXPLAIN را ساده کند.
برای graph و تغییرهای tracked، EF می‌تواند کد دستی را کم کند.
مقایسهٔ سرعت بدون workload و اندازه‌گیری، دلیل معماری نیست.

## query گزارش

```sql
SELECT t."Id" AS "Id", t."Title" AS "Title", count(n."Id")::int AS "NoteCount"
FROM tickets t
LEFT JOIN notes n ON n."TicketId" = t."Id"
WHERE t."Title" = @Title
GROUP BY t."Id", t."Title"
ORDER BY t."Id"
LIMIT @Limit;
```

LEFT JOIN تیکت بدون note را حفظ می‌کند.
count(n.Id) برای آن تیکت صفر است؛ count(*) روی join می‌توانست یک ردیف مصنوعی را بشمارد.
cast به int با سازندهٔ TicketSummary(int, string, int) هماهنگ است.
این انتخاب برای سقف پنج یادداشت امن است؛ برای شمارش بزرگ عمومی long را ارزیابی کن.
aliasها را با نام property/پارامتر DTO هماهنگ نگه دار.
نام‌های quoted این snapshot را با schema کوچک‌نویسی‌شدهٔ درس ۴۰ مخلوط نکن.

## parameter واقعی

```csharp
var command = new CommandDefinition(sql,
    new { Title = inputTitle, Limit = 10 },
    commandTimeout: 20, cancellationToken: ct);
var rows = (await connection.QueryAsync<TicketSummary>(command)).ToArray();
```

این قطعه با using Dapper نوشته شده؛ کد کامل نمونه معادل static همان API را صدا می‌زند.
Title و Limit مقدارند و parameter می‌شوند.
نام جدول، نام ستون و جهت ORDER BY را نمی‌توان مثل مقدار parameter کرد.
برای انتخاب ستون مرتب‌سازی، ورودی محدود را به چند عبارت ثابت مجاز map کن.
رشتهٔ ساخته‌شده از ورودی را به SQL نچسبان، حتی اگر از Dapper استفاده می‌کنی.

## آزمایش تزریق

عنوان آزمایشی شامل متن Robert'); DROP TABLE tickets;-- است.
query با مقایسهٔ دقیق همان عنوان، فقط یک تیکت را برمی‌گرداند.
سپس تعداد تیکت‌ها با EF بررسی می‌شود و باید همچنان دو باشد.
این آزمون نشان می‌دهد همان مسیر parameterized امن است؛ مجوز اتصال بیش‌ازحد یا queryهای دیگر را بررسی نمی‌کند.
خطا یا خاموش بودن exceptionها را نشانهٔ ایمن بودن SQL حساب نکن.

## اتصال و ownership

NpgsqlDataSource طول عمر مدیریت pool را دارد؛ اتصال بازشده واحد کار کوتاه است.
await using اتصال را برمی‌گرداند و از نشت connection جلوگیری می‌کند.
CommandDefinition token و timeout را به اجرای command می‌رساند.
نتیجهٔ کوچک این درس buffered/materialize می‌شود؛ برای خروجی بسیار بزرگ، streaming و طول عمر اتصال را جداگانه طراحی کن.
query بدون سقف می‌تواند حافظه و pool را تحت فشار بگذارد.

## اشتراک transaction با EF

این مثال دو کار مستقل دارد: seed commit شده و سپس گزارش خواندنی.
اتصال Dapper آن، خودکار در transaction context EF شرکت نمی‌کند.
اگر نوشتن با EF و Dapper باید یک واحد اتمی باشد، باید همان DbConnection باز و همان DbTransaction را آگاهانه به هر دو بدهی.
دو connection جدا حتی با connection string یکسان، یک transaction نیستند.
مثال این درس چنین transaction مشترکی ادعا نمی‌کند.

## خطاها و logging

نام پارامتر و نوع آن را برای عیب‌یابی بشناس؛ مقدار حساس را به log عمومی نریز.
حذف ChangeTracker به معنی حذف constraint یا خطاهای سرور نیست.
SQLSTATE و timeout و cancellation همچنان باید سیاست پاسخ مشخص داشته باشند.
Dapper برای query هدفمند ابزار است، جایگزین معماری سرویس نیست.

## اجرای مرجع

از ریشهٔ مخزن و پس از تنظیم SUPPORTDESK_PG اجرا کن:

```powershell
dotnet run --project samples/46-dapper/SupportDesk.csproj
```

Lab یک schema تصادفی تازه می‌سازد و در پایان همان schema را پاک می‌کند.
با --keep در انتهای فرمان می‌توانی آن را برای بازرسی نگه داری؛ نام schema در خروجی چاپ می‌شود.
این گزینه ابزار مشاهده است، نه ذخیره‌سازی پایدار برنامه.
کد خروج صفر و PASS: lesson scenario یعنی assertionهای همین سناریو عبور کرده‌اند.
برای بررسی همهٔ این درس‌ها، python scripts/ef_smoke.py را با دیتابیس آماده اجرا کن.

## تمرین و معیار عبور

1. تیکت بدون note اضافه کن و ثابت کن count صفر است؛ count(*) را مقایسه کن.
2. فیلتر Status را با parameter اضافه کن و متن SQL و مقدارها را جدا نگه دار.
3. sort مجاز با دو گزینهٔ Id و Title بساز؛ ورودی ناشناخته را رد کن.
4. گزارش EF درس قبل و نسخهٔ Dapper را با ترتیب و دادهٔ برابر مقایسه کن، سپس EXPLAIN بگیر.

معیار عبور: query پارامتری، mapping صحیح DTO و ownership اتصال را نشان بدهی و تفاوت آن با EF را دقیق توضیح بدهی.

## منبع رسمی

[مستندات مرتبط](https://github.com/DapperLib/Dapper)

[Npgsql EF provider](https://www.npgsql.org/efcore/)

[بخش چهارم](../README.md) · [نقشهٔ دوره](../../../ROADMAP.md) · [درس بعد](../048-data-delivery/README.md)
