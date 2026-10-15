# درس ۰۳۸ — index و EXPLAIN و طرح query

[قبلی](../037-joins/README.md) · [خانه](../../../README.md) · [بعدی](../039-transactions/README.md)

## خروجی و پیش‌نیاز

برای یک query پرتکرار index متناسب طراحی و plan واقعی آن را می‌خوانی.
فرق هزینهٔ تخمینی، زمان واقعی و تعداد row را می‌فهمی.
پیش‌نیاز: WHERE، ORDER BY، LIMIT و اجرای PostgreSQL درس‌های قبل.
این درس benchmark محصول یا اثبات سریع‌تر بودن همیشگی index نیست.

## ۱. مسئلهٔ query

```sql
SELECT id, title FROM tickets
WHERE status='Open'
ORDER BY id
LIMIT 20;
```

روی table کوچک احتمالاً همه‌چیز سریع به نظر می‌رسد.
با زیاد شدن داده، planner باید بین راه‌های دسترسی انتخاب کند.
می‌خواهیم filter روی status و ترتیب Id با نیاز query هماهنگ باشد.
حفظ همان نتیجه پیش از بهینه‌سازی شرط اصلی است.

## ۲. fixture مصنوعی

نمونه صد هزار Ticket می‌سازد و فقط هر هزارمین row را Open می‌گذارد.
پس صد row یعنی حدود یک‌دهم درصد Open داریم.
بقیه Closed هستند؛ note و workflow API در این dataset ساختگی مدل نشده‌اند.
این داده برای selectivity و plan است، نه import معتبر به مدل کامل Domain.
هر اجرا در schema آزمایشی خودش انجام می‌شود.

## ۳. EXPLAIN بدون اجرای query

EXPLAIN plan تخمینی planner را نشان می‌دهد.
rows تعداد تخمینی و cost واحد داخلی برآورد است؛ cost را میلی‌ثانیه نخوان.
width برآورد عرض row خروجی است.
این plan با statistics، نسخهٔ server و تنظیمات planner تغییر می‌کند.
یک نام node را به عنوان نتیجهٔ ثابت تمام محیط‌ها حفظ نکن.

## ۴. ANALYZE یعنی واقعاً اجرا کن

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id, title FROM tickets WHERE status='Open' ORDER BY id LIMIT 20;
```

این ANALYZE بخشی از EXPLAIN است و query را واقعاً اجرا می‌کند.
برای SELECT، خواندن و هزینهٔ واقعی داریم.
برای UPDATE یا DELETE آزمایشی ممکن است داده تغییر کند؛ آن‌ها را بدون درک اثر روی دادهٔ واقعی اجرا نکن.
statement جداگانهٔ ANALYZE tickets آمار table را برای planner به‌روز می‌کند و کاربرد دیگری دارد.

## ۵. index مرکب

```sql
CREATE INDEX ix_tickets_status_id ON tickets(status,id);
ANALYZE tickets;
```

B-tree پیش‌فرض برای این الگوی برابری status و ترتیب Id قابل بررسی است.
ترتیب columnهای index مهم است؛ این تعریف را با index فقط روی id یکی ندان.
Id از قبل primary key و دارای index است، ولی نیاز filter status جداست.
نیاز به هر دو index را با queryها و هزینهٔ write تحلیل می‌کنیم.

## ۶. خواندن nodeها

| node احتمالی | برداشت اولیه |
| --- | --- |
| Seq Scan | پیمایش table |
| Index Scan | استفاده از index و در صورت نیاز خواندن heap |
| Index Only Scan | دادهٔ لازم از index، با محدودیت visibility |
| Bitmap Heap Scan | جمع‌آوری محل rowها و خواندن heap |
| Sort | مرتب‌سازی rowهای ورودی |
| Limit | محدود کردن نتیجهٔ بالادست |

وجود index تضمین نمی‌کند query از آن استفاده کند.
SELECT شامل title است و index این نمونه title را نگه نمی‌دارد؛ Index Only Scan را قطعی فرض نکن.
پایین‌ترین nodeها داده را برای بالادست آماده می‌کنند؛ plan را فقط از آخرین خط نخوان.

## ۷. actual rows و loops

actual rows و زمان nodeها را کنار loops بخوان.
در node دارای چند loop، شمارش و زمان گزارش‌شده را با تعداد تکرار تفسیر کن.
اختلاف بزرگ rows تخمینی و واقعی می‌تواند علامت آمار نامناسب یا هم‌بستگی داده باشد.
صرف بالا بودن زمان یک اجرا علت قطعی index نامناسب را ثابت نمی‌کند.

## ۸. BUFFERS و cache

Buffers تعداد دسترسی‌های مرتبط با blockها را نشان می‌دهد.
shared hit به معنی پیدا شدن در buffer cache است و shared read خواندن به shared buffers را نشان می‌دهد.
این مقدار را بدون توجه به cache سیستم‌عامل مساوی تعداد I/O فیزیکی دیسک ندان.
اجرای دوم ممکن است به علت cache سریع‌تر باشد، حتی اگر index تغییر نکند.
برای ادعای performance باید شرایط و چند اجرا را ثبت کرد.

## ۹. چرا query کم‌انتخاب‌تر ممکن است Seq Scan شود؟

query روی Closed تقریباً تمام صد هزار row را می‌خواهد.
planner ممکن است هزینهٔ استفاده از index و heap را بیش از scan table برآورد کند.
این لزوماً failure optimizer نیست.
خاموش کردن enable_seqscan برای تولید plan دلخواه راه‌حل production نیست.
اول نیاز query، حجم خروجی و آمار را بررسی کن.

## ۱۰. هزینهٔ index

index فضای دیسک و حافظه و هزینهٔ INSERT/UPDATE/DELETE دارد.
تغییر columnهای index نیاز به نگهداری ساختار اضافه ایجاد می‌کند.
برای هر column مستقل index نساختن و همچنین حذف همهٔ indexها هر دو تصمیم ناقص‌اند.
unique و primary key هم معمولاً index پشتیبان دارند؛ duplication را پیش از افزودن بررسی کن.

## ۱۱. partial و covering به عنوان گزینه

اگر queryهای Open بخش کوچکی از نیاز باشند، partial index می‌تواند گزینه باشد.
اگر projection کوچک ثابت داریم، INCLUDE ممکن است بعضی خواندن‌ها را پوشش دهد.
در این نمونه فقط index مرکب کامل پیاده شده است.
قبل از افزودن گزینه‌های بیشتر، query و plan و هزینهٔ write را مقایسه کن.

## ۱۲. سناریوی اجرا

seed، ANALYZE و plan اولیه چاپ می‌شوند.
سپس index ساخته و ANALYZE و plan همان query دوباره چاپ می‌شوند.
در پایان query Closed نیز plan دارد تا selectivity متفاوت را ببینی.
assertionها وجود index، تعداد fixture و نتیجهٔ Idهای نخست را بررسی می‌کنند.
هیچ تستی نام Index Scan یا زمان کمتر از یک عدد دلخواه را الزام نمی‌کند؛ این شرطها می‌توانند flaky و وابسته به محیط باشند.

## اجرای نمونه و بازرسی

ابتدا [محیط PostgreSQL](../../../handbook/postgresql-lab.md) و متغیر SUPPORTDESK_PG را آماده کن.
از ریشهٔ دوره:

```powershell
dotnet run --project samples/37-index-plans/SupportDesk.csproj
```

باید نتیجهٔ سناریو و PASS را ببینی؛ نبود connection string کد خروج ۲ و failure دیتابیس یا سناریو کد ۱ می‌دهد.
برای نگه داشتن schema همین اجرا:

```powershell
dotnet run --project samples/37-index-plans/SupportDesk.csproj -- --keep
```

نام schema در خروجی چاپ می‌شود و می‌توانی آن را در psql بررسی کنی.
بدون --keep فقط schema تولیدشدهٔ همان اجرا پس از آزمایش پاک می‌شود.
[فهرست فایل‌ها و محدودهٔ نمونه](../../../samples/37-index-plans/README.md) کنار همین درس است.
فرمان `python scripts/postgres_smoke.py` کل شش نمونه را روی server واقعی بررسی می‌کند.
موفقیت `--build-only` را اجرای SQL حساب نکن.

## تمرین مستقل

دو plan Open پیش و پس از index را ذخیره کن.
filter، sort، row estimate، actual rows و buffers را مقایسه کن.
اگر plan فرق نکرد، آمار و تعداد داده و محدودیت query را بررسی و دلیل احتمالی را بنویس.
ادعای «ده برابر سریع‌تر» را بدون شرایط تکرارپذیر گزارش نکن.

## تمرین تکمیلی

یک schema آزمایشی تازه با نسبت Open بیشتر بساز.
انتخاب planner را با dataset اولیه مقایسه کن.
یک query بدون ORDER BY هم اجرا کن و تفاوت sort و دسترسی را ببین؛ تغییر نیازمندی را به عنوان بهینه‌سازی همان query پنهان نکن.

## معیار عبور

- [ ] EXPLAIN و EXPLAIN ANALYZE اثر یکسان ندارند.
- [ ] cost را زمان واقعی نمی‌خوانم.
- [ ] ترتیب columnهای index را به query ربط می‌دهم.
- [ ] Seq Scan را همیشه بد فرض نمی‌کنم.
- [ ] cache و هزینهٔ write در قضاوت من حضور دارند.
- [ ] تست صحت نتیجه را به plan خاصِ یک محیط گره نمی‌زنم.

## منابع رسمی

- [Using EXPLAIN](https://www.postgresql.org/docs/18/using-explain.html)
- [Multicolumn indexes](https://www.postgresql.org/docs/18/indexes-multicolumn.html)
- [Planner statistics](https://www.postgresql.org/docs/18/planner-stats.html)
