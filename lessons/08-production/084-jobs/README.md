# درس ۰۸۴ — job گم نشود، حتی اگر worker قطع شد

صف Channel درس ۷۶ با پایان process از بین می‌رفت. اکنون یک job را در PostgreSQL نگه می‌داریم و به worker برای مدتی محدود حق پردازش می‌دهیم.
مسئلهٔ مهم، فقط برداشتن job نیست: اگر process وسط کار قطع شد یا job دوباره اجرا شد، چه می‌شود؟

## ۱. اجرای قدم‌به‌قدم

[آزمایشگاه عملیات](../../../handbook/operations-lab.md) را برای PostgreSQL یک‌بارمصرف انجام بده و SUPPORTDESK_JOBS را تنظیم کن.
از ریشهٔ مخزن:

```powershell
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- init
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- seed
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- once
dotnet run --project samples/83-durable-jobs/SupportDesk.csproj -- status
```

برای DB خالی، خروجی‌های اصلی:

```text
schema=ready
job=accepted
job=done
pending=0; leased=0; done=1; dead=0; effects=1
```

seed همیشه یک شناسهٔ ثابت می‌فرستد؛ تکرارش job دوم نمی‌سازد. once بعدی در این وضعیت job=none می‌دهد.
init فقط schema آزمایش را می‌سازد؛ روی دیتابیس محصول اجرا نکن و آن را migration حساب نکن.

## ۲. stateها را جدا بفهم

job یعنی شرح یک کار که باید پردازش شود؛ worker برنامه‌ای است که کار آماده را برمی‌دارد و انجام می‌دهد. lease یعنی حق پردازش موقت: این worker تا یک مهلت مشخص مالک معتبر این تلاش است، نه مالک همیشگی job.

acknowledgment یا ack یعنی اعلام پذیرفته‌شدن پایان پردازش طبق قرارداد صف. در این نمونه، فقط داشتن JobId برای ack کافی نیست؛ token تلاش فعلی و اعتبار lease هم باید بررسی شوند. crash یعنی process قبل از طی‌کردن مسیر معمول پایان متوقف شود؛ ممکن است دیگر فرصت ارسال Fail هم نداشته باشد.

| state | معنی |
| --- | --- |
| pending | منتظر زمان مجاز شروع |
| leased | یک worker با token فعلی و مهلت سی ثانیه‌ای مالک کار است |
| done | نتیجهٔ محلی و acknowledgment با هم commit شده‌اند |
| dead | سقف سه تلاش تمام شده؛ نیازمند بررسی عملیاتی |

Lease علاوه بر JobId یک Token تازه دارد. JobId هویت کار است؛ Token هویت یک تلاشِ دارای مالکیت است.
زمان‌های مهلت از clock دیتابیس می‌آیند تا اختلاف ساعت workerها تصمیم مالکیت را تعیین نکند.

## ۳. دو worker چطور یک کار را برندارند؟

در Persistence/QueueStore.cs متد ClaimAsync را ببین.
یک transaction، job آماده را با FOR UPDATE SKIP LOCKED انتخاب و همان ردیف را leased می‌کند؛ attempts افزایش و token عوض می‌شود.
SKIP LOCKED ردیف قفل‌شدهٔ مصرف‌کنندهٔ دیگر را رد می‌کند؛ ابزار عمومی گزارش‌گیری سازگار نیست، اما برای صف کاربرد دارد.
در رقابت هشت claimant روی یک job، تست فقط یک lease معتبر انتظار دارد.
این polling کوچک برای آموزش است؛ ترتیب دقیق اجرای همهٔ jobها یا عدالت مصرف‌کنندگان را تضمین نمی‌کند.

## ۴. crash و مالک قدیمی

اگر worker پس از claim بمیرد، job تا پایان lease منتظر می‌ماند و بعد قابل claim دوباره است.
مالک جدید token تازه می‌گیرد. CompleteAsync پیش از نوشتن نتیجه، token، state و اعتبار زمانی را زیر قفل ردیف بررسی می‌کند.
مالک قدیمی نمی‌تواند با token منقضی acknowledgment بفرستد.

چرا به token تازه نیاز داریم؟ این ترتیب را دنبال کن:

| لحظه | worker اول | worker دوم |
| --- | --- | --- |
| claim اول | JobId ثابت، token=A و مهلت دارد | هنوز مالک نیست |
| پایان مهلت بدون تکمیل | token=A دیگر حق تکمیل معتبر نمی‌دهد | می‌تواند دوباره claim کند |
| claim دوم | ممکن است هنوز محاسبهٔ قدیمی را ادامه بدهد | همان JobId، ولی token=B می‌گیرد |
| Complete قدیمی برسد | باید رد شود | حق تلاش تازه با B باقی است |

fencing یعنی جلوگیری از اثرِ مالک قدیمی با بررسی مرز مالکیت؛ در این پیاده‌سازی، token و state و مهلت زیر قفل بررسی می‌شوند. JobId مشخص می‌کند «کدام کار؟» و token مشخص می‌کند «کدام تلاشِ فعلاً معتبر؟». A و B در جدول فقط نام دو token متفاوت‌اند؛ این tokenها token ورود کاربر نیستند.
تست expiry با SQL کنترل‌شده، clock مهلت را برای همان container آزمایشی عقب می‌برد؛ سی ثانیه Sleep نمی‌کند.

هر claim یک تلاش حساب می‌شود؛ crash هم رایگان نیست. آخرین تلاشِ crashشده بعد از expiry به dead منتقل می‌شود.
FailAsync برای خطای قابل انتظار، کار را با دو ثانیه تأخیر دوباره pending می‌کند یا پس از تلاش سوم dead می‌کند.
در محیط واقعی backoff و jitter و تشخیص خطای دائمی/موقت لازم است؛ تأخیر ثابت این lab سیاست محصول نیست.

## ۵. «دوباره اجرا» با «اثر دوباره» یکی نیست

جدول effects برای هر JobId کلید یکتا دارد. نوشتن اثر محلی و done شدن job در **یک transaction PostgreSQL** انجام می‌شود.
Complete دوباره، اثر دوم نمی‌سازد. اینجا اثر فقط درج مقدار مصنوعی processed است.
این تضمین شامل ایمیل یا درخواست HTTP خارجی نیست؛ سرویس خارجی در transaction دیتابیس ما شرکت ندارد.

at-least-once یعنی در قرارداد تحویلِ دارای retry، باید احتمال دوباره تحویل/اجراشدن کار را بپذیریم؛ به معنی موفقیت قطعی هر job نیست. این lab سقف تلاش و state dead دارد. idempotencyِ اثر یعنی تکرار همان کار، اثر business اضافی نسازد؛ جدول effects با کلید JobId در همین محدوده کمک می‌کند.

مثلاً اگر worker ایمیل را واقعاً بفرستد و قبل از ثبت done قطع شود، تلاش بعدی ممکن است دوباره ایمیل بفرستد. rollback دیتابیس نمی‌تواند ایمیل ارسال‌شده را پس بگیرد. outbox/inbox الگوهای ثبت و مصرف پیام و reconciliation یعنی تطبیق وضعیت‌های نامعلوم و رفع اختلاف‌اند؛ هیچ‌کدام را صرفاً با نوشتن اسمشان تضمین exactly-once ندان. در تمرین crash، همیشه «آیا دوباره اجرا شد؟» و «آیا اثر دوباره ثبت شد؟» را جدا جواب بده.
برای اثر خارجی باید idempotency key سمت مقصد، outbox/inbox یا جبران و reconciliation متناسب طراحی کنی؛ ادعای exactly-once سراسری نکن.

## ۶. Hangfire یا Quartz را چه وقت بررسی کنم؟

| نیاز | گزینه‌ای که باید ارزیابی شود | چیزی که هنوز باید تصمیم بگیری |
| --- | --- | --- |
| اجرای background method و retry و dashboard عملیاتی | Hangfire | storage provider، مجوز dashboard، serialization، retry و idempotency |
| اجرای تقویمی، trigger و cron و misfire | Quartz.NET | persistent store، timezone، clustering و سیاست اجرای دیررس |
| فهم قرارداد lease و transaction در این lab | صف کوچک PostgreSQL | سقف تلاش، reclaim، سازگاری اثر و مانیتورینگ |

انتخاب این درس پیاده‌سازی کوچک برای دیدن مکانیزم است؛ توصیه به ساخت scheduler سفارشیِ کامل برای محصول نیست.
Hangfire هم با retry پیش‌فرض مسئلهٔ اثر تکراری را خودکار حل نمی‌کند. Quartz با store حافظه‌ای، دوام پس از crash نمی‌دهد؛ persistence باید صریح انتخاب شود.
دو framework را فقط برای پر کردن csproj نصب نکرده‌ایم. راهنمای رسمی نسخهٔ مورد انتخاب را قبل از پیاده‌سازی واقعی بخوان.

## ۷. تست دیتابیس واقعی

```powershell
dotnet test tests/084-jobs/SupportDesk.Tests.csproj
```

Docker با Linux containers لازم است. شش تست، رقابت claim، تکرار enqueue/ack، fencing، retry exhaustion، آخرین crash و باز شدن data source تازه و cancellation را روی PostgreSQL واقعی می‌سنجند.
نبود Docker به skip تبدیل نمی‌شود. جدول‌های container خودش reset می‌شوند؛ دیتابیس کاربر پاک نمی‌شود.

## تمرین و معیار عبور

1. پنجره‌های crash را بنویس: قبل از claim، بعد از claim، قبل از commit و بعد از commit.
2. برای هر پنجره توضیح بده آیا job دوباره دیده می‌شود و چرا effects دوباره ثبت نمی‌شود.
3. برای dead job یک runbook بنویس: علت را بررسی کن، اثر خارجی را reconcile کن و فقط سپس دربارهٔ requeue تصمیم بگیر. این lab endpoint عمومی requeue ندارد.
4. بگو برای کار طولانی‌تر از lease چه heartbeat/renewal و fencing بیشتری لازم است؛ در این نمونه renewal وجود ندارد.

برای عبور، بتوانی at-least-once را با crash توضیح بدهی و مرز تضمین transaction محلی را مشخص کنی.

## منابع رسمی

- [PostgreSQL SKIP LOCKED](https://www.postgresql.org/docs/18/sql-select.html)
- [Hangfire exception و retry](https://docs.hangfire.io/en/latest/background-processing/dealing-with-exceptions.html)
- [Quartz tutorial](https://www.quartz-scheduler.net/documentation/quartz-4.x/tutorial/)
- [Quartz persistent store](https://www.quartz-scheduler.net/documentation/quartz-4.x/configuration/reference)

[قبلی](../083-caching/README.md) · [بعدی](../085-performance/README.md) · [فهرست بخش](../README.md)
