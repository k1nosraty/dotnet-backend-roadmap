# درس ۰۳۵ — مدل رابطه‌ای و کلید و constraint

[قبلی](../../03-web/034-api-delivery/README.md) · [خانه](../../../README.md) · [بعدی](../036-sql-basics/README.md)

## خروجی و پیش‌نیاز

با table، row، column، key و constraint دادهٔ درخواست را مدل می‌کنی.
تفاوت قاعدهٔ مدل C# و تضمین دیتابیس را می‌بینی.
پیش‌نیاز: سه بخش قبلی و [آماده‌سازی PostgreSQL](../../../handbook/postgresql-lab.md).
این درس شروع persistence API نیست؛ یک آزمایش مستقل روی دیتابیس است.

## ۱. چرا از حافظه به مدل رابطه‌ای می‌رویم؟

store حافظه‌ای با restart پاک می‌شود و بین چند process مشترک نیست.
یک فایل JSON تک‌نویسنده نیز مسئلهٔ چند writer، query و هماهنگی تغییرها را به تنهایی حل نمی‌کند.
پایگاه دادهٔ رابطه‌ای علاوه بر نگهداری داده، زبان query، constraint و transaction می‌دهد.
صرف ذخیره در PostgreSQL به معنی طراحی درست schema یا امنیت کامل نیست.

## ۲. مفاهیم پایه

| مفهوم | مثال |
| --- | --- |
| database | supportdesk_lab، مرز اتصال و مدیریت |
| schema | namespace آزمایش جاری داخل database |
| table | tickets، مجموعهٔ ردیف‌ها با ساختار مشترک |
| row | یک Ticket مشخص |
| column | title یا status |
| primary key | id یکتا و غیر null |

schema با database یکی نیست و schema فایل JSON هم معنی دیگری دارد.
نام column با property C# الزاماً یکسان نیست؛ mapping باید آگاهانه باشد.
وجود table به تنهایی entity domain را با تمام متدهایش ذخیره نمی‌کند.

## ۳. اولین DDL

```sql
CREATE TABLE tickets (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY CHECK (id > 0),
    title text NOT NULL CHECK (title = btrim(title) AND char_length(title) BETWEEN 1 AND 120),
    status text NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'InProgress', 'Closed')),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

DDL شکل داده و محدودیت‌ها را تعریف می‌کند.
فایل واقعی این statement در Sql/01-schema.sql نمونه است.
کلمه‌های کلیدی را می‌توان با حروف کوچک نوشت؛ در متن برای خوانایی بزرگ نوشته‌ایم.
نام‌های بدون quote در PostgreSQL به lowercase تبدیل می‌شوند؛ در این دوره snake_case را بدون quote انتخاب می‌کنیم.

## ۴. نوع column

integer عدد صحیح ۳۲بیتی است و با Id فعلی نمونه سازگار است.
text متن Unicode ذخیره می‌کند؛ اگر database با UTF-8 ساخته شده باشد، عنوان فارسی نیز حفظ می‌شود.
timestamptz لحظهٔ زمانی را نگه می‌دارد و نمایش آن با timezone session فرق می‌کند؛ نام timezone اولیه را ذخیره نمی‌کند.
برای مبلغ از numeric با سیاست precision و scale استفاده می‌کنیم، نه float به صورت خودکار.
در این schema مبلغ نداریم و column اضافهٔ بدون نیاز نمی‌سازیم.

## ۵. هویت و تولید Id

IDENTITY تولید مقدار را به sequence مربوط متصل می‌کند.
PRIMARY KEY یکتایی و غیر null بودن را enforce می‌کند؛ identity به تنهایی جای primary key نیست.
ALWAYS ارسال دستی مقدار را در مسیر معمول محدود می‌کند؛ override مدیریتی همچنان وجود دارد.
CHECK مثبت بودن، عدد منفی overrideشده را هم رد می‌کند.
sequence ممکن است پس از rollback یا INSERT ناموفق فاصله داشته باشد.
Id را شمارهٔ بدون فاصلهٔ فاکتور یا Count+1 فرض نکن.

## ۶. عنوان و دو لایهٔ validation

در SQL فعلاً guard ذخیره‌سازی انتخاب کرده‌ایم: عنوان خالی نباشد، طول code point بیشتر از ۱۲۰ نشود و فاصلهٔ معمول بیرونی نداشته باشد.
این guard تمام invariantهای API را بیان نمی‌کند.
قانون C# عنوان همچنان حداقل سه و حداکثر ۱۲۰ واحد UTF-16 و Trim است.
char_length و string.Length برای emoji لزوماً برابر نیستند و btrim پیش‌فرض فقط space معمول را حذف می‌کند.
به همین دلیل CHECK ساده را «همان validation C#» معرفی نمی‌کنیم.
در درس ۳۶ ابتدا از TicketTitle معتبر عبور می‌کنیم؛ تغییر واحد طول باید در کل قرارداد و دادهٔ قبلی بررسی شود.

## ۷. NULL با رشتهٔ خالی فرق دارد

SQL NULL نبودن مقدار معلوم است؛ با رشتهٔ خالی یا صفر یکی نیست.
شرط title = NULL نتیجهٔ مورد انتظار برای پیدا کردن nullها نمی‌دهد؛ از IS NULL استفاده کن.
SQL منطق سه‌ارزشی دارد و بعضی عبارت‌ها UNKNOWN می‌شوند.
CHECK معمولاً فقط false را رد می‌کند؛ پس CHECK طول به تنهایی جای NOT NULL نیست.
در این نمونه هر دو لازم‌اند.

## ۸. DEFAULT اعتبارسنجی نیست

اگر status را نفرستی DEFAULT مقدار Open می‌دهد.
اگر صریحاً NULL بفرستی NOT NULL آن را رد می‌کند.
اگر Unknown بفرستی CHECK آن را رد می‌کند.
default مقدار نامعتبر ارسال‌شده را خودکار به مقدار درست تبدیل نمی‌کند.
همچنین CHECK مجموعهٔ نام‌ها مسیر مجاز Open به Closed را enforce نمی‌کند.

## ۹. درج و خواندن اولیه

```sql
INSERT INTO tickets(title) VALUES ('Network issue');
SELECT id, title, status FROM tickets ORDER BY id;
```

درج با نام column روشن است و به ترتیب همهٔ columnهای table وابسته نیست.
بدون ORDER BY ترتیب نمایش قرارداد قابل اتکا ندارد.
متن داخل کوتیشن تکی literal SQL است؛ متن کاربر در کد C# باید parameter شود که درس بعد انجام می‌دهیم.
در این آزمایش literalها دادهٔ ثابت آموزشی‌اند.

## ۱۰. database از تمام business policy خبر ندارد

در این table هنوز Notes وجود ندارد و transitionها ذخیره نشده‌اند.
SQL دستی می‌تواند status شناخته‌شده ولی خارج از گردش workflow بنویسد.
این محدودیت صریح نمونه است؛ در بخش تراکنش و طراحی schema آن را تحلیل می‌کنیم.
Domain و application همچنان مسئول invariantها و مجوزهای خود هستند.
اعتبارسنجی UI به تنهایی حتی جای CHECK ساده را هم نمی‌گیرد، چون writerهای دیگر ممکن است وجود داشته باشند.

## ۱۱. اجرای مستقل

runner آماده برای ساخت schema و اجرای SQL از Npgsql استفاده می‌کند.
در این درس تمرکز روی فایل‌های SQL است؛ خطوط driver و connection در درس ۳۶ باز می‌شوند.
هر اجرا schema تازهٔ خودش را دارد و با جدول‌های تمرین دیگر تداخل ندارد.
دو Ticket seed، عنوان فارسی در نمونهٔ CRUD بعدی، و چند constraint failure قابل بررسی‌اند.

## ۱۲. SQLSTATE و بررسی شکست

کدهای PostgreSQL برای تشخیص نوع خطا از متن ترجمه‌شونده مطمئن‌ترند.
23502 برای NOT NULL، 23514 برای CHECK و 23505 برای UNIQUE از نمونه‌های این درس‌اند.
فایل 04-checks.sql خطاهای مورد انتظار را داخل blockهای مستقل می‌گیرد و نبود خطا را failure اعلام می‌کند.
این DO block ابزار بررسی است؛ برای ایجاد table معمول نیازی به نوشتن PL/pgSQL نیست.
صرف موفق شدن build C# اجرای constraint را ثابت نمی‌کند.

## اجرای نمونه و بازرسی

ابتدا [محیط PostgreSQL](../../../handbook/postgresql-lab.md) و متغیر SUPPORTDESK_PG را آماده کن.
از ریشهٔ دوره:

```powershell
dotnet run --project samples/34-relational/SupportDesk.csproj
```

باید نتیجهٔ سناریو و PASS را ببینی؛ نبود connection string کد خروج ۲ و failure دیتابیس یا سناریو کد ۱ می‌دهد.
برای نگه داشتن schema همین اجرا:

```powershell
dotnet run --project samples/34-relational/SupportDesk.csproj -- --keep
```

نام schema در خروجی چاپ می‌شود و می‌توانی آن را در psql بررسی کنی.
بدون --keep فقط schema تولیدشدهٔ همان اجرا پس از آزمایش پاک می‌شود.
[فهرست فایل‌ها و محدودهٔ نمونه](../../../samples/34-relational/README.md) کنار همین درس است.
فرمان `python scripts/postgres_smoke.py` کل شش نمونه را روی server واقعی بررسی می‌کند.
موفقیت `--build-only` را اجرای SQL حساب نکن.

## تمرین مستقل

table کوچکی برای دسته‌بندی درخواست با Id، نام ضروری و نام یکتا طراحی کن.
NULL، نام خالی، نام تکراری و Id تکراری را جدا آزمایش کن.
مشخص کن کدام شرط از نوع داده می‌آید و کدام از constraint.
برای دادهٔ معتبر SELECT با ترتیب مشخص بنویس.

## تمرین تکمیلی

یک description اختیاری اضافه کن و NULL و متن خالی را مقایسه کن.
در README سیاست نبود description را ثبت کن.
برای عنوان emoji بررسی کن string.Length و char_length چه عددی می‌دهند؛ نگوییم دو معیار یکی‌اند.

## معیار عبور

- [ ] database و schema و table را تفکیک می‌کنم.
- [ ] identity و primary key یک وظیفه ندارند.
- [ ] NULL و CHECK و NOT NULL را جدا توضیح می‌دهم.
- [ ] SQL بدون ORDER BY را مرتب فرض نمی‌کنم.
- [ ] CHECK نام status را با قانون transition یکی نمی‌دانم.
- [ ] آزمون منفی constraint واقعاً روی PostgreSQL اجرا شده است.

## منابع رسمی

- [PostgreSQL data definition](https://www.postgresql.org/docs/18/ddl.html)
- [Constraints](https://www.postgresql.org/docs/18/ddl-constraints.html)
- [Identity columns](https://www.postgresql.org/docs/18/ddl-identity-columns.html)
