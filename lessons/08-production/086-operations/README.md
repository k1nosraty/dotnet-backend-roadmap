# درس ۰۸۶ — backup را با restore اثبات کن؛ worker را قابل برگشت تحویل بده

داشتن فایل backup کافی نیست. باید بتوانیم آن را در دیتابیس تازه برگردانیم، jobهای منتظر را ادامه بدهیم و اگر نسخهٔ تازه آماده نبود، همان artifact قبلی را اجرا کنیم.
این درس، تمرین پایان بخش عملیات است.

## ۱. worker پایدار را دستی ببین

[آزمایشگاه عملیات](../../../handbook/operations-lab.md) را برای DB یک‌بارمصرف اجرا و با CLI درس ۸۴ init و seed کن.
از ریشهٔ مخزن:

```powershell
dotnet run --project samples/85-worker-delivery/SupportDesk.csproj --urls http://127.0.0.1:5086
```

در ترمینال دیگر:

```powershell
curl.exe http://127.0.0.1:5086/health/live
curl.exe http://127.0.0.1:5086/health/ready
curl.exe http://127.0.0.1:5086/version
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- status
```

وقتی schema و DB در دسترس باشند، دو health پاسخ 200 دارند و version برابر 1.0.0 است. job منتظر، با یک effect دیتابیسی done می‌شود.
بدون SUPPORTDESK_JOBS برنامه با پیام ثابت و exit code 2 تمام می‌شود؛ connection string چاپ نمی‌شود.
با Ctrl+C Host متوقف و token worker لغو می‌شود؛ کار claimشدهٔ ناتمام پس از پایان lease دوباره قابل بررسی است، نه اینکه حتماً در shutdown تمام شود.

## ۲. مرز فایل‌ها و عمرها

| محل | مسئولیت |
| --- | --- |
| Program.cs | DI، version و liveness/readiness |
| JobWorker.cs | loop، scope هر نوبت، cancellation و backoff هنگام نبود DB/job |
| Persistence/QueueStore.cs | همان قرارداد claim و fencing و commit اتمیک درس ۸۴ |
| scripts/operations_recovery.py | محیط دورریختنی، dump/restore و آزمون تعویض نسخه |

DataSource مشترک با عمر Host است و scope نوبت پردازش آزاد می‌شود. worker هیچ task بدون await رها نمی‌کند.
readiness به availability جدول صف و حالت Maintenance نگاه می‌کند و timeout دارد؛ سلامت کامل همهٔ permissionها، effectها و سرویس خارجی را اثبات نمی‌کند.
liveness عمداً DB را بررسی نمی‌کند. وقتی Maintenance=true است، readiness برابر 503 می‌شود و worker job تازه claim نمی‌کند.
این flag کنترل مدیریتی configuration است؛ route عمومی برای روشن/خاموش کردنش نداریم.

## ۳. آزمایش بازیابیِ واقعی و مستقل

Docker با Linux containers، SDK 10 و Python 3 لازم است:

```powershell
python scripts/operations_recovery.py
```

اسکریپت به SUPPORTDESK_JOBS شخصی تو متصل نمی‌شود. یک container با نام تصادفی، password خودش و پورت loopback پویا می‌سازد و در پایان فقط همان را حذف می‌کند.
مراحلش:

1. یک job را done و یک job دیگر را pending می‌گذارد.
2. با pg_dump -Fc یک archive سازگار از jobs و effects می‌گیرد.
3. در دیتابیس تازهٔ restored با pg_restore --exit-on-error برمی‌گرداند.
4. شناسه‌ها، stateها، attempts و effect ثبت‌شده را با snapshot مقایسه می‌کند.
5. یک archive خراب را در مرحلهٔ خواندن رد می‌کند؛ آن را روی دیتابیس موجود restore نمی‌کند.

زمان dump و restore همان اجرای کوچک چاپ می‌شود. فایل backup در این آزمایش داخل container موقت است و نگه‌داری off-site/رمزنگاری/retention محصول را جایگزین نمی‌کند.
pg_dump نقش‌ها، secretهای محیط و همهٔ منابع بیرون DB را به‌عنوان یک بستهٔ کامل عملیات ذخیره نمی‌کند؛ آن‌ها برنامهٔ جدا می‌خواهند.

## ۴. تعویض نسخه و rollback

دو publish جدا از همین کد ساخته می‌شود: CourseVersion=1.0.0 و 1.1.0.
تغییر شمارهٔ metadata، migration نمی‌سازد؛ schema این دو نسخه عمداً یکسان است.

| مرحله | شاهد آزمایش |
| --- | --- |
| نسخهٔ ۱ روی DB بازیابی‌شده | job pending به done می‌رود و version درست است |
| توقف و job تازه | pending در DB باقی می‌ماند |
| نسخهٔ ۱.۱ با Maintenance=true | live=200، ready=503، job تازه مصرف نمی‌شود |
| بازگشت به همان پوشهٔ نسخهٔ ۱ | hash فایل‌ها ثابت و version=1.0.0 است؛ job تازه ادامه پیدا می‌کند |
| پایان | برای هر سه job فقط یک effect محلی وجود دارد؛ DB مبدأ snapshot عوض نشده است |

این آزمایش، health gate و rollback artifact روی runner است؛ reverse proxy را خودکار switch نمی‌کند و deployment عمومی انجام نمی‌دهد.
برای تعویض واقعی ترافیک باید readiness، drain، timeout و rollback مقصد را در runbook همان زیرساخت تعریف کنی.

## ۵. rollback برنامه، rollback داده نیست

اگر نسخهٔ جدید ستونی را حذف کرده باشد، اجرای binary قدیمی ممکن است دیگر ممکن نباشد.
برای rollout واقعی از migration سازگار مثل expand/contract، backup معتبر و rehearsal روی کپی استفاده کن؛ restore روی DB زنده هم‌زمان با write انجام نده.
پس از restore، صف ممکن است jobی را دوباره ببیند که اثر خارجی‌اش قبلاً انجام شده است؛ idempotency و reconciliation همچنان لازم‌اند.
cache را هم فراموش نکن: namespace/epoch قدیمی ممکن است با revision بازیابی‌شده تداخل کند؛ قبل از گرفتن ترافیک سیاست invalidation را اجرا کن.

## ۶. RPO و RTO را روی کاغذ تعریف کن

| واژه | سؤال |
| --- | --- |
| RPO | حداکثر چه مقدار داده بر حسب زمان می‌تواند از دست برود؟ |
| RTO | حداکثر چه مدت تا بازگشت سرویس فرصت داریم؟ |

موفقیت این نمونهٔ کوچک، RPO صفر یا RTO محصول را اثبات نمی‌کند. حجم داده، انتقال backup، ساخت مقصد، دسترسی کلید، restore، validation و برگشت ترافیک هم زمان می‌برند.
برای PITR و بازیابی نزدیک‌تر به حادثه، WAL و برنامهٔ backup مناسب لازم است؛ SQL dump این درس به‌تنهایی PITR نیست.

## ۷. چک‌لیست تحویل worker

- قرارداد ورودی و idempotency، سقف retry و رفتار dead job مستند باشد.
- مسیر secret و مجوز محدود DB، TTL/حافظهٔ cache و نام‌های telemetry مشخص باشد.
- live و ready و توقف و lease expiry شاهد تست داشته باشند.
- backup در مقصد تازه restore و نتیجهٔ داده و job بررسی شده باشد.
- artifact قبلی، سازگاری schema و روش rollback و مسئول تصمیم ثبت شوند.

برای بررسی خودکار کل این مرحله:

```powershell
python scripts/operations_smoke.py
python scripts/operations_smoke.py --with-docker
```

حالت عادی تست‌های کانتینری Redis و PostgreSQL را فقط build می‌کند و unit/load را اجرا می‌کند؛ حالت کامل علاوه بر آن، هشت تست کانتینری و آزمایش بازیابی بالا را واقعاً اجرا می‌کند. نبود Docker در حالت کامل شکست است.

## تمرین مستقل و معیار عبور

یک runbook بنویس که از not-ready شدن نسخهٔ تازه شروع شود و تا تصمیم rollback، بررسی schema، اجرای artifact قبلی و تأیید jobها پیش برود.
برای هر گام، «شاهد موفقیت» و «شرط توقف» بنویس. هیچ دستور حذف DB یا volume واقعی را از این lab به runbook محصول کپی نکن.
برای عبور، گزارش restore و ادامهٔ job و رد نسخهٔ not-ready را تحویل بده و محدودیت اثر خارجی و schema را توضیح بده.

## منابع رسمی

- [PostgreSQL SQL dump و restore](https://www.postgresql.org/docs/18/backup-dump.html)
- [pg_dump](https://www.postgresql.org/docs/18/app-pgdump.html)
- [pg_restore](https://www.postgresql.org/docs/18/app-pgrestore.html)
- [Continuous archiving و PITR](https://www.postgresql.org/docs/18/continuous-archiving.html)

[قبلی](../085-performance/README.md) · [فهرست بخش](../README.md) · [پروژهٔ نهایی در نقشه](../../../ROADMAP.md)
درس‌های ۸۷ تا ۹۰ هنوز برنامه‌ریزی‌شده‌اند.

[ادامه: درس ۸۷ و پروژهٔ نهایی](../../09-capstone/087-requirements/README.md)
