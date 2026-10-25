# درس ۰۳۹ — transaction و isolation

[قبلی](../038-indexes/README.md) · [خانه](../../../README.md) · [بعدی](../040-schema/README.md)

## خروجی و پیش‌نیاز

چند statement را با transaction به یک واحد موفقیت وصل می‌کنی.
با دو connection مستقل تفاوت snapshot در Read Committed و Repeatable Read را مشاهده می‌کنی.
rollback، خطای serialization و انتظار واقعی row lock را بررسی می‌کنی.
پیش‌نیاز: Npgsql، command، constraint و مفهوم state مشترک.

## ۱. چرا چند statement مشکل می‌سازند؟

فرض کن ثبت درخواست و ثبت note اولیه دو statement باشند.
اگر اولی موفق و دومی شکست بخورد، وضعیت نیمه‌کاره می‌تواند باقی بماند.
transaction مشخص می‌کند کدام مجموعه تغییر باید با هم commit یا rollback شوند.

statement یعنی یک دستور SQL مانند INSERT یا UPDATE. transaction گروهی از این دستورها با یک مرز موفقیت است: commit یعنی تغییرهای transaction را تأیید کنیم؛ rollback یعنی تغییرهای تأییدنشدهٔ آن را برگردانیم. rollback دکمهٔ Undo برای هر commit قبلی نیست.

برای ثبت درخواست همراه note، این مسیر را تصور کن:

| گام | نتیجه در transaction باز |
| --- | --- |
| INSERT درخواست موفق می‌شود | هنوز موفقیت کل کار اعلام نشده |
| INSERT note به دلیل FK خراب شکست می‌خورد | مسیر commit عادی را ادامه نمی‌دهیم |
| rollback انجام می‌شود | درخواستِ همین تلاش هم باقی نمی‌ماند |

connection کانال ارتباط برنامه با DB و session وضعیت همان ارتباط در سمت server است. در آزمایش دو connection می‌خواهیم تا دو مصرف‌کنندهٔ مستقل واقعاً بتوانند دادهٔ مشترک را تغییر دهند.
اما transaction به تنهایی تمام invariantهای چند writer را بدون isolation و locking مناسب حفظ نمی‌کند.

## ۲. چهار مفهوم ACID

| مفهوم | برداشت عملی |
| --- | --- |
| Atomicity | تغییرهای همان transaction با هم موفق یا rollback شوند |
| Consistency | constraint و قواعد انتخاب‌شده حفظ شوند؛ دیتابیس business policy را حدس نمی‌زند |
| Isolation | مشاهده و تداخل transactionها طبق سطح و رفتار server کنترل شود |
| Durability | commit با سیاست و پیکربندی server ماندگار شود |

ACID را تضمین نبودن bug، وجود backup یا مجوز درست کاربر ندان.
قطع اتصال بعد از ارسال COMMIT می‌تواند نتیجه را برای client نامعلوم کند.
rollback بعدی client لزوماً commit انجام‌شده را برنمی‌گرداند.

## ۳. مرز connection

```csharp
await using var connection = await db.OpenConnectionAsync(token);
await using var transaction = await connection.BeginTransactionAsync(token);
```

statementهای همان transaction باید از connection مربوط استفاده کنند.
CreateCommand روی DataSource ممکن است connection دیگری بگیرد و بیرون transaction تو اجرا شود.
در نمونه NpgsqlCommand با connection و transaction مشخص ساخته می‌شود.
connection مشترک را هم‌زمان برای دو command مستقل فعال مصرف نمی‌کنیم.
دو session تست این درس دو connection واقعی‌اند.

## ۴. commit پس از آخرین موفقیت

```csharp
await using var command = new NpgsqlCommand(sql, connection, transaction);
await command.ExecuteNonQueryAsync(token);
await transaction.CommitAsync(token);
```

commit را پس از موفق شدن همهٔ مراحل انجام بده.
Success را قبل از commit به client اعلام نکن.
exception باید مسیر rollback و disposal را روشن کند.
Dispose transaction باز معمولاً rollback می‌کند، اما در کد آزمایشی شکست مورد انتظار را صریح RollbackAsync می‌کنیم.

## ۵. شکست constraint و وضعیت aborted

در PostgreSQL خطای یک statement می‌تواند transaction جاری را aborted کند.
ادامه دادن query عادی در همان transaction بدون rollback مشکل را رفع نمی‌کند.
در مرجع، insert Ticket انجام می‌شود و سپس note با parent ناموجود FK را می‌شکند.
transaction rollback می‌شود و query مستقل ثابت می‌کند Ticket نیمه‌کاره باقی نمانده است.
exception عمومی را به Success تبدیل نمی‌کنیم.

## ۶. Id پس از rollback

sequence معمولاً مقدار مصرف‌شده را به خاطر rollback پس نمی‌دهد.
پس ممکن است یک Id دیگر در داده نباشد ولی شمارهٔ بعدی جلو رفته باشد.
این با atomicity ردیف‌های business تضاد ندارد؛ قرارداد sequence بدون فاصله نیست.
برای شماره‌گذاری حقوقی بدون فاصله، نیاز و هزینهٔ طراحی دیگری داریم.

## ۷. Read Committed

در PostgreSQL سطح پیش‌فرض متداول Read Committed است.
هر statement معمولاً snapshot دادهٔ commitشده در شروع همان statement را می‌بیند.
در مرجع، connection اول عنوان را می‌خواند؛ connection دوم مقدار تازه commit می‌کند.
خواندن بعدی connection اول در همان transaction، مقدار تازه را می‌بیند.
پس transaction باز به تنهایی تضمین نمی‌کند دو SELECT یک جواب بدهند.

isolation یعنی وقتی دیگری هم‌زمان کار می‌کند، این transaction چه چیزی را می‌بیند. snapshot در این بحث نمای قابل مشاهدهٔ دادهٔ commitشده است، نه فایل backup.

| ترتیب آزمایش | Read Committed | Repeatable Read در بخش بعد |
| --- | --- | --- |
| خواننده اولین SELECT را می‌زند | مقدار A را می‌بیند | مقدار A را می‌بیند |
| نویسندهٔ دیگر B را commit می‌کند | تغییر نویسنده انجام شده | تغییر نویسنده انجام شده |
| خواننده در همان transaction دوباره SELECT می‌زند | B را می‌بیند | همچنان A را می‌بیند |

A و B در این جدول فقط نام دو مقدار هستند؛ متن دقیق هر سناریو را در Program ببین. ثابت‌ماندن مشاهده در Repeatable Read به معنی توقف تمام نویسنده‌ها نیست. برای فهم جدول، هر بار مشخص کن کدام connection می‌خواند و کدام commit می‌کند.

## ۸. Repeatable Read

در transaction دوم، snapshot با نخستین query داده‌ای تثبیت می‌شود.
پس از commit connection دیگر، SELECT بعدی هنوز مقدار snapshot قبلی را می‌بیند.
این سطح همهٔ تغییرهای جهان را قفل نمی‌کند و همهٔ anomalyها را هم حذف نمی‌کند.
در PostgreSQL می‌تواند رفتار snapshot isolation و failure serialization مرتبط داشته باشد.
رفتار نام isolation را برای تمام DBMSها یکسان فرض نکن.

## ۹. SQLSTATE 40001

نمونه بعد از تغییر row توسط connection دوم، از snapshot Repeatable Read قدیمی تلاش به update همان row می‌کند.
PostgreSQL خطای serialization با کد 40001 می‌دهد.
این failure در تست انتظار می‌رود و rollback می‌شود.
در use case واقعی، retry باید تمام transaction را از ابتدا و با تعداد تلاش محدود تکرار کند.
تکرار فقط آخرین UPDATE در transaction aborted یا snapshot قبلی راه‌حل نیست.

aborted یعنی transaction به وضعیتی رسیده که ادامهٔ عادی آن ممکن نیست. خطای 40001 هم اینجا خطای تبدیل JSON نیست؛ server تغییرِ مورد نظر را با وضعیت رقابتی این transaction قابل قبول ندانسته است. retryِ کل transaction یعنی دوباره از خواندن دادهٔ معتبر شروع کنیم، تصمیم را دوباره بگیریم و بعد برای ذخیره تلاش کنیم؛ با سقف تلاش مشخص.
ارسال ایمیل و side effect بیرونی را هم نباید با هر retry بی‌حساب تکرار کنیم.

## ۱۰. Serializable و محدودیت نام‌گذاری

Serializable هدف معادل شدن اجرا با یک ترتیب serial معتبر را دنبال می‌کند.
ممکن است برای حفظ این قرارداد بعضی transactionها را abort کند؛ برنامه باید failure مناسب را مدیریت کند.
این سطح «هرگز خطا نمی‌دهد» یا «همه چیز را به ترتیب قفل می‌کند» نیست.
در این نمونه Serializable اجرا نشده و تمرکز تست روی Read Committed و Repeatable Read است.
در PostgreSQL، Read Uncommitted نیز مانند Read Committed رفتار می‌کند؛ این نکتهٔ اختصاصی server است.

## ۱۱. row lock با FOR UPDATE

```sql
SELECT id FROM tickets WHERE id=1 FOR UPDATE;
```

داخل transaction، row انتخاب‌شده برای عملیات ناسازگار writer دیگر قفل می‌شود.
خواندن معمول MVCC الزاماً مانند UPDATE منتظر همان قفل نمی‌ماند.
قفل تا پایان transaction نگه داشته می‌شود؛ transaction را هنگام انتظار کاربر یا تماس HTTP بیرونی باز نگذار.
ترتیب ثابت گرفتن قفل چند row، احتمال deadlock را کم می‌کند ولی همهٔ علت‌ها را حذف نمی‌کند.
کد 40P01 را با 40001 یک خطای متنی یکسان parse نکن.

## ۱۲. مشاهدهٔ واقعی انتظار

LockDemo با connection اول row را قفل می‌کند.
UPDATE connection دوم به صورت asynchronous شروع می‌شود.
یک query سوم با pg_blocking_pids بررسی می‌کند که backend دوم واقعاً blocker دارد.
پس از مشاهده، transaction اول rollback و آزاد می‌شود و UPDATE دوم commit می‌شود.
تست فقط بر اساس یک Sleep ثابت ادعای قفل نمی‌کند؛ timeout هم مانع انتظار نامحدود است.
این helper ابزار آزمایش است، نه منطق repository محصول.

## ۱۳. نمونهٔ خطر check سپس write

اگر دو writer هر دو count note را چهار بخوانند، ممکن است هر دو note اضافه و تعداد شش شود.
transaction بدون هماهنگی parent لزوماً این race را رفع نمی‌کند.
یک پروتکل ممکن این است که همهٔ writerهای note ابتدا همان Ticket parent را FOR UPDATE بگیرند، سپس وضعیت و count را بررسی و insert کنند.
این پروتکل فقط وقتی درست است که همهٔ writerهای مربوط آن را رعایت کنند و سطح isolation و retry درست تحلیل شده باشد.
قفل داخل process C# برای چند instance سرویس جای قفل دیتابیس را نمی‌گیرد.
در این sample چنین note service کاملی پیاده نشده؛ تمرین طراحی آن است.

## ۱۴. فایل‌های نمونه

| فایل | نقش |
| --- | --- |
| Program.cs | آزمایش دو snapshot و rollback |
| Infrastructure/LockDemo.cs | مشاهدهٔ row lock واقعی |
| Infrastructure/Lab.cs | schema موقت و timeout |
| Sql/01-schema.sql | tableهای محدود آزمایش |

هیچ کدام هنوز unit of work عمومی برای همهٔ entityها نیستند.
ساخت یک transaction کوتاه برای یک use case از پخش کردن begin/commit در controllerهای مختلف روشن‌تر است.

## اجرای نمونه و بازرسی

ابتدا [محیط PostgreSQL](../../../handbook/postgresql-lab.md) و متغیر SUPPORTDESK_PG را آماده کن.
از ریشهٔ دوره:

```powershell
dotnet run --project samples/38-transactions/SupportDesk.csproj
```

باید نتیجهٔ سناریو و PASS را ببینی؛ نبود connection string کد خروج ۲ و failure دیتابیس یا سناریو کد ۱ می‌دهد.
برای نگه داشتن schema همین اجرا:

```powershell
dotnet run --project samples/38-transactions/SupportDesk.csproj -- --keep
```

نام schema در خروجی چاپ می‌شود و می‌توانی آن را در psql بررسی کنی.
بدون --keep فقط schema تولیدشدهٔ همان اجرا پس از آزمایش پاک می‌شود.
[فهرست فایل‌ها و محدودهٔ نمونه](../../../samples/38-transactions/README.md) کنار همین درس است.
فرمان `python scripts/postgres_smoke.py` کل شش نمونه را روی server واقعی بررسی می‌کند.
موفقیت `--build-only` را اجرای SQL حساب نکن.

## تمرین مستقل

برنامه را اجرا و سه مشاهده را ثبت کن: Read Committed تغییر را دید، Repeatable Read مقدار قبلی را دید و UPDATE قدیمی 40001 گرفت.
با query مستقل ثابت کن INSERT پیش از FK failure باقی نمانده است.
خروجی lock را با sessionهای SQL دستی مقایسه کن.
در README توضیح بده چرا دو query متوالی بدون writer دوم هیچ چیزی دربارهٔ isolation ثابت نمی‌کنند.

## تمرین تکمیلی

برای AddNote یک use case با parent row lock و حداکثر پنج note طراحی کن.
قبل از insert، Closed و duplicate و ظرفیت را بررسی کن.
دو connection را با seed چهار note آزمایش کن؛ مجموع موفقیت‌ها فقط یک باید باشد.
تمام writerها باید پروتکل یکسان داشته باشند؛ SQL دستی بدون آن را موفقیت امنیتی سیستم حساب نکن.

## معیار عبور

- [ ] commandهای transaction روی connection درست‌اند.
- [ ] failure میانی دادهٔ نیمه‌کاره نگه نمی‌دارد.
- [ ] rollback را بازگشت sequence به عدد قبلی نمی‌دانم.
- [ ] snapshot در دو isolation آزمایش‌شده متفاوت است.
- [ ] 40001 را با retry تمام transaction تحلیل می‌کنم.
- [ ] انتظار واقعی writer دوم و آزاد شدن row lock مشاهده شده است.

## منابع رسمی

- [Transactions](https://www.postgresql.org/docs/18/tutorial-transactions.html)
- [Transaction isolation](https://www.postgresql.org/docs/18/transaction-iso.html)
- [Explicit locking](https://www.postgresql.org/docs/18/explicit-locking.html)
- [Npgsql transactions](https://www.npgsql.org/doc/basic-usage.html)
