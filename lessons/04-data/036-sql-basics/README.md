# درس ۰۳۶ — SQL و CRUD پارامتری

[قبلی](../035-relational/README.md) · [خانه](../../../README.md) · [بعدی](../037-joins/README.md)

## خروجی و پیش‌نیاز

چهار عملیات Create، Read، Update و Delete را با SQL پارامتری از C# اجرا می‌کنی.
NpgsqlDataSource، command، reader، cancellation و مالکیت اتصال را می‌شناسی.
پیش‌نیاز: درس ۳۵، await، value object و resource ownership.
این repository کوچک، adapter آموزشی است و هنوز به API متصل نشده است.

## ۱. کتابخانه و مسئولیت آن

Npgsql driver ارتباط .NET با PostgreSQL است؛ ORM یا EF Core نیست.
نمونه نسخهٔ ثابت 10.0.3 دارد و target آن net10.0 است.
نسخه و سازگاری از [NuGet رسمی](https://www.nuget.org/packages/Npgsql/10.0.3) بررسی شده‌اند.
PackageReference در csproj آماده وجود دارد؛ اجرای نخست restore می‌خواهد.
نصب driver قوانین domain، migration و مجوز کاربر را ایجاد نمی‌کند.

## ۲. DataSource و اتصال

```csharp
await using NpgsqlDataSource db = NpgsqlDataSource.Create(connectionString);
```

DataSource معمولاً برای پیکربندی و pooling با عمر بلند ساخته می‌شود.
در وب آینده آن را برای هر request دوباره نمی‌سازیم.
اتصال یک session با server است و نباید آن را هم‌زمان بین چند عملیات مستقل به اشتراک گذاشت.
وقتی transaction لازم باشد connection مشخص می‌گیریم؛ درس ۳۹ این موضوع را عملی می‌کند.
connection string از SUPPORTDESK_PG session می‌آید و در source یا log چاپ نمی‌شود.

## ۳. INSERT پارامتری

```csharp
await using var command = db.CreateCommand(
    "INSERT INTO tickets(title) VALUES ($1) RETURNING id,title,status");
command.Parameters.Add(new NpgsqlParameter {
    NpgsqlDbType = NpgsqlDbType.Text, Value = title.Value
});
```

$1 placeholder پارامتر اول است و مقدار از SQL جدا ارسال می‌شود.
RETURNING دادهٔ ساخته‌شده از جمله Id تولیدشده را در همان statement برمی‌گرداند.
برای گرفتن Id از MAX(id) بعد از INSERT استفاده نمی‌کنیم؛ writer دیگری می‌تواند بین دو عملیات داده بسازد.
نوع Text صریح است و به inference مقدار null وابسته نیستیم.

## ۴. parameter برای value است

نام table، column یا جهت ORDER BY را معمولاً با parameter مقدار نمی‌توان جایگزین کرد.
اگر sort دلخواه لازم است، mapping ثابت از نام‌های مجاز به SQL ثابت بساز.
SQL را با عنوان یا شناسهٔ متنی کاربر concatenate نکن.
حذف quote از ورودی راه‌حل injection نیست و می‌تواند متن درست را خراب کند.
درج متن دارای کوتیشن باید بدون تغییر معنی یا حذف جدول انجام شود.

## ۵. خواندن reader

```csharp
await using var reader = await command.ExecuteReaderAsync(token);
if (!await reader.ReadAsync(token))
    throw new InvalidOperationException("INSERT returned no row.");
return new TicketRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
```

reader ابتدا پیش از اولین row قرار دارد و ReadAsync به row بعدی می‌رود.
ستون‌های ۰، ۱ و ۲ مطابق SELECT یا RETURNING صریح ما هستند.
GetString روی NULL مجاز نیست؛ برای column اختیاری ابتدا IsDBNull را بررسی کن.
TicketRow قرارداد خروجی این adapter است و خود Ticket domain با تمام رفتارهایش نیست.

## ۶. lookup و مقدار ناموجود

```sql
SELECT id, title, status FROM tickets WHERE id=$1;
```

WHERE ردیف‌ها را محدود می‌کند و parameter اول Integer است.
Id ناموجود نتیجهٔ صفر row می‌دهد و repository null برمی‌گرداند.
خطای قطع اتصال null نیست و نباید با catch عمومی به Ticket ناموجود تبدیل شود.
کلید یکتا انتظار حداکثر یک row را توجیه می‌کند.

## شرط‌های ترکیبی و محدود کردن خروجی

WHERE می‌تواند شرط‌های AND و OR داشته باشد؛ برای معنی روشن از پرانتز استفاده کن.
query فهرست می‌تواند status، حداقل Id، ترتیب صعودی و limit داشته باشد:

```sql
SELECT id,title,status FROM tickets
WHERE status=$1 AND id>$2
ORDER BY id
LIMIT $3;
```

هر سه value باید با نوع مناسب parameter شوند و limit پیش از اجرا محدود شود.
LIKE و ILIKE برای الگوی متن‌اند؛ parameter شدن مقدار wildcardهای درصد و underscore را به خودی خود بی‌اثر نمی‌کند.
سیاست جست‌وجوی literal یا pattern را مشخص کن؛ آن را با جلوگیری از SQL injection یک موضوع واحد ندان.
برای NULL در query از IS NULL استفاده کن؛ برابری معمول با NULL همان معنی را ندارد.

## ۷. UPDATE و row count

```sql
UPDATE tickets SET title=$1 WHERE id=$2;
```

ExecuteNonQueryAsync تعداد rowهای متاثر را می‌دهد.
برای Id موجود یک و برای ناموجود صفر انتظار داریم.
حذف WHERE می‌تواند همهٔ ردیف‌ها را تغییر دهد؛ query را قبل از اجرا با همان شرط مرور کن.
این adapter فقط SQL را نشان می‌دهد؛ بررسی Closed و مجوز ویرایش باید در use case نهایی حفظ شود.
در این نمونه status تمام رکوردهای ساخته‌شده Open است و workflow API به آن منتقل نشده است.

## ۸. DELETE صریح

```sql
DELETE FROM tickets WHERE id=$1;
```

اولین حذف موجود true و حذف تکراری false می‌دهد.
شناسهٔ پارامتری مانع اثر یک متن مخرب به عنوان syntax می‌شود، ولی authorization را انجام نمی‌دهد.
در درس‌های رابطه و امنیت، FK و مجوز و سیاست حذف نیز باید اعمال شوند.
Id تولیدشده را بعد از DELETE به Count+1 برنمی‌گردانیم.

## ۹. شکل‌های اجرای command

| API | کاربرد نمونه |
| --- | --- |
| ExecuteReaderAsync | چند column یا چند row |
| ExecuteScalarAsync | یک مقدار از اولین row/column، مثل count |
| ExecuteNonQueryAsync | درج/تغییر/حذف بدون row خروجی و DDL |

اگر INSERT دارای RETURNING چند column است، reader انتخاب مناسب این نمونه است.
اسکریپت‌های ثابت schema بدون ورودی کاربر با command آماده اجرا می‌شوند.
برای تعداد زیاد command مستقل می‌توان batching را جدا بررسی کرد؛ هر بار string طولانی ساختن ORM نیست.

## ۱۰. await using و token

command و reader با await using تا پایان scope dispose می‌شوند.
connection قرض‌گرفته‌شده از DataSource به شکل مناسب آزاد یا به pool برمی‌گردد.
CancellationToken به اجرا و ReadAsync منتقل شده است.
pre-cancel این درس تکرارپذیر است؛ لغو وسط نوشتن الزاماً ثابت نمی‌کند server هیچ تغییری نداده است.
retry نوشتن باید با قرارداد عملیات و transaction بررسی شود.

## ۱۱. چیدمان واقعی

| مسیر | مسئولیت |
| --- | --- |
| Contracts/TicketRow.cs | دادهٔ خروجی query |
| Domain/TicketTitle.cs | عنوان معتبر از درس ارزش |
| Data/TicketRepository.cs | SQL و mapping reader |
| Infrastructure/Lab.cs | اتصال و schema آزمایشی |
| Sql/01-schema.sql | DDL همین snapshot |
| Program.cs | سناریو و assertionها |

دلیل نام repository محدود بودن به یک capability است، نه اجبار generic repository برای هر جدول.
مدل domain connection string و Npgsql نمی‌شناسد.
Program محل اتصال اجزا و آزمایش است و هنوز Controller نداریم.

## ۱۲. رفتار مرجع

عنوان فارسی Trim‌شده ساخته و دقیق خوانده می‌شود.
متنی شبیه SQL مخرب به عنوان داده ذخیره و بدون تغییر برمی‌گردد؛ table باقی می‌ماند.
rename موجود موفق و rename ناموجود false است.
delete تکراری false است و رکورد دیگر حفظ می‌شود.
خواندن با token از قبل لغوشده باید لغو را گزارش کند.
خروجی PASS به موفق شدن همهٔ این سناریوها وابسته است.

## اجرای نمونه و بازرسی

ابتدا [محیط PostgreSQL](../../../handbook/postgresql-lab.md) و متغیر SUPPORTDESK_PG را آماده کن.
از ریشهٔ دوره:

```powershell
dotnet run --project samples/35-sql-crud/SupportDesk.csproj
```

باید نتیجهٔ سناریو و PASS را ببینی؛ نبود connection string کد خروج ۲ و failure دیتابیس یا سناریو کد ۱ می‌دهد.
برای نگه داشتن schema همین اجرا:

```powershell
dotnet run --project samples/35-sql-crud/SupportDesk.csproj -- --keep
```

نام schema در خروجی چاپ می‌شود و می‌توانی آن را در psql بررسی کنی.
بدون --keep فقط schema تولیدشدهٔ همان اجرا پس از آزمایش پاک می‌شود.
[فهرست فایل‌ها و محدودهٔ نمونه](../../../samples/35-sql-crud/README.md) کنار همین درس است.
فرمان `python scripts/postgres_smoke.py` کل شش نمونه را روی server واقعی بررسی می‌کند.
موفقیت `--build-only` را اجرای SQL حساب نکن.

## تمرین مستقل

ListAsync را با limit مثبت و مرتب‌سازی Id اضافه کن.
limit را محدود کن و query پارامتری بنویس.
فهرست خالی باید collection خالی بدهد، نه null برای خطای اتصال.
حداقل عنوان دارای کوتیشن، متن فارسی و Id ناموجود را بررسی کن.

## تمرین تکمیلی

description اختیاری را در SQL و قرارداد query اضافه کن.
برای ارسال NULL از parameter دارای نوع مشخص و DBNull.Value استفاده کن.
در خواندن، null را به رشتهٔ ساختگی تبدیل نکن؛ قرارداد nullable را حفظ کن.
قبل از افزودن dependency جدید، نشان بده driver فعلی دقیقاً کدام نیاز را حل نکرده است.

## معیار عبور

- [ ] SQL ثابت و value پارامتری جدا هستند.
- [ ] Id ساخته‌شده از RETURNING می‌آید، نه MAX.
- [ ] reader و command عمر محدود و روشن دارند.
- [ ] ناموجود و خطای اتصال را یکی نمی‌کنم.
- [ ] DataSource را اتصال مشترکِ قابل مصرف parallel فرض نمی‌کنم.
- [ ] مسیر Data هنوز قانون workflow و authorization را جایگزین نکرده است.

## منابع رسمی

- [Npgsql basic usage](https://www.npgsql.org/doc/basic-usage.html)
- [Npgsql package 10.0.3](https://www.nuget.org/packages/Npgsql/10.0.3)
- [PostgreSQL INSERT RETURNING](https://www.postgresql.org/docs/18/dml-returning.html)
