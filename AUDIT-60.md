# ممیزی آموزشی و فنی تا درس 060

تاریخ: 2026-10-17. محدوده: پنج بخش اول، ۶۰ درس نوشته‌شده و ۵۹ پروژهٔ ورودی اجرایی مستقل.
این گزارش مکمل [ممیزی تاریخی درس‌های ۱ تا ۳۴](AUDIT.md) است؛ آن گزارش به شاهد و محدودهٔ زمان خودش تعلق دارد.

## نتیجه و معنای تأیید

برای محدودهٔ آموزشی پنج بخش اول، مسیر محتوا و نمونه‌ها قابل تأیید است.
در مرور حاضر خطای مسدودکنندهٔ تازه‌ای در منطق جاری پیدا نشد؛ دو نقطهٔ ضعف در پوشش بررسی‌های خودکار تقویت شدند.
این نتیجه، ادعای نبود هر خطای ممکن، حل تمام تمرین‌های آزاد فراگیر یا آماده بودن محصول production نیست.
درس‌های ۶۱ تا ۹۰ همچنان برنامه‌ریزی‌شده‌اند و در شمار درس‌های کامل حساب نمی‌شوند.

## روش و شواهد

- نسخهٔ محلیِ مبنای مرور با SHA فایل‌های آخرین head در GitHub تطبیق داده شد؛ اختلافی وجود نداشت.
- فهرست ۹۰ درس، موجود بودن فقط ۶۰ درس نوشته‌شده، لینک‌های داخلی و تطابق فرمان پروژهٔ هر درس بررسی شدند.
- ممیزی قبلی بخش‌های ۱ تا ۳ مبنا قرار گرفت؛ قراردادهای نهایی CLI و API با نمونه‌های جدید مقایسه شدند.
- در بخش داده، load و hydrate، token هم‌زمانی، یک‌واحدکار بودن mutation، transaction خواندن، migration و failure بررسی شدند.
- در بخش معماری، immutable model، port و adapter، mapping، counter فایل، validation، policy وابستگی و ADRها مرور شدند.
- تست‌های اجرایی موجود روی همهٔ نمونه‌ها، با ماتریس Windows و Linux و PostgreSQL 18 اجرا می‌شوند؛ اجرای محلی ساختاری جای اجرای C# معرفی نشده است.

شاهد مبنای کد پیش از این تقویت‌ها: [اجرای موفق Check course شمارهٔ ۳۰](https://github.com/k1nosraty/dotnet-backend-roadmap/actions/runs/39030972511).
نتیجهٔ اجرای آخرین commit و سابقهٔ ادغام از [Pull Request شمارهٔ ۱](https://github.com/k1nosraty/dotnet-backend-roadmap/pull/1) و [Actions](https://github.com/k1nosraty/dotnet-backend-roadmap/actions) قابل پیگیری است.
شرط ادغام این مرور، موفق شدن هر پنج job آخرین commit است: دو regression، دو architecture delivery و PostgreSQL integration.

## یافته‌ها و تغییرهای این مرور

| یافته | تغییر | شاهد |
| --- | --- | --- |
| تست فایل خراب، بعضی خطاهای schema و Domain را می‌پوشاند ولی counter معتبر از نظر JSON را به‌طور صریح پوشش نمی‌داد | نسخهٔ ناشناختهٔ سند، NextId صفر/منفی، برخورد با Id موجود و مقدار خارج از ظرفیت به fixtureهای تحویل اضافه شد | AdapterChecks درس ۶۰: رد constructor و save و ثابت ماندن bytes |
| parser گزارش NuGet از get با پیش‌فرض مجموعهٔ خالی استفاده می‌کرد؛ تغییر شکل یا نبود داده می‌توانست بررسی را بی‌اثر کند | فهرست کامل dependency و نسخهٔ FluentValidation جدا بررسی می‌شوند؛ projectهای آن با گزارش آسیب‌پذیری تطبیق داده می‌شوند و خطای ابزار رد می‌شود. حذف framework از گزارش فیلترشدهٔ بدون هشدار مجاز است | architecture_final_smoke و اجرای audit واقعی dependency |
| گزارش ریز ممیزی قبلی فقط تا درس ۳۴ بود | محدودهٔ جاری، ماتریس ۶۰ درس، تفاوت‌های عمدی و محدودیت‌های تأیید ثبت شد | همین گزارش و لینک صفحهٔ اصلی |

این دو مورد، شکاف شاهد تست بودند؛ به‌عنوان خرابی اثبات‌شدهٔ منطق برنامه معرفی نشده‌اند.

## تفاوت‌های عمدی مراحل که خطا محسوب نمی‌شوند

| موضوع | توضیح |
| --- | --- |
| قواعد اولیه و قواعد کامل تیکت | درس‌های مبانی مدل ساده‌تری دارند؛ قواعد note و Closed در طراحی و تحویل‌های بعد اضافه می‌شوند. snapshotهای اولیه بازنویسیِ بی‌مقدمهٔ مراحل آینده نیستند. |
| جست‌وجو | حافظه از OrdinalIgnoreCase و API PostgreSQL درس ۴۸ از Contains حساس به حروف استفاده می‌کند؛ تفاوت در درس ۴۸ صریح است. |
| محدودیت طول DB و Domain | طول .NET و PostgreSQL معیار یکسان ندارند؛ constraintهای DB حداقل‌های schema هستند و جای همهٔ invariantها نیستند. |
| persistence بخش معماری | adapter JSON تک‌process برای اجرای آموزشی انتخاب شده است؛ API PostgreSQL درس ۴۸ مستقل باقی مانده و با portهای جدید یکپارچه نشده است. |
| فایل نسخهٔ ۱ و ۲ | تحویل ۶۰ به NextId نیاز دارد؛ سند آن نسخهٔ ۲ است و برای اجرای دستی فایل تازه لازم است. migration خودکار ادعا نشده است. |
| Close و Resolve | Close تکراری تغییر معنایی ندارد؛ Resolve شامل افزودن note است و پس از Closed به Conflict می‌رسد. |
| validation دوباره | HTTP و use case از همان validator pure استفاده می‌کنند؛ هزینهٔ اجرای دوباره در ADR ثبت شده و به rule دارای I/O تعمیم داده نشده است. |
| هویت و record | Ticket با Id شناخته می‌شود؛ equality تولیدشدهٔ record معیار عمومی هویت aggregate یا برابری محتوایی collection معرفی نشده است. |

## ماتریس مرور درس‌ها

این جدول محدودهٔ مرور هر درس را ثبت می‌کند. خواندن یا اجرای نمونهٔ مرجع، معادل انجام مستقل تمرین فراگیر نیست.

| درس | موضوع | تمرکز مرور |
| --- | --- | --- |
| 001 | [محیط، SDK، CLI و اولین اجرا](lessons/01-foundations/001-environment/README.md) | نصب SDK، فرق runtime و SDK، فرمان اجرا و معیار محیط |
| 002 | [نوع‌ها، متغیرها و اولین قابلیت](lessons/01-foundations/002-first-feature/README.md) | نوع‌ها، decimal، تقسیم صحیح و حالت صفر |
| 003 | [ورودی، شرط و اعتبارسنجی](lessons/01-foundations/003-input-validation/README.md) | TryParse، null، normalization و مرز ورودی |
| 004 | [حلقه‌ها و منوی کنسول](lessons/01-foundations/004-loops/README.md) | منو، حلقه، EOF و مسیر خروج |
| 005 | [متدها و جداسازی محاسبه از ورودی](lessons/01-foundations/005-methods/README.md) | تفکیک محاسبه و ورودی و refactor بدون تغییر رفتار |
| 006 | [List و Dictionary و جست‌وجو](lessons/01-foundations/006-collections/README.md) | List و Dictionary، جست‌وجو و شناسهٔ ناموجود |
| 007 | [کلاس، سازنده و property](lessons/01-foundations/007-classes/README.md) | سازنده، property و حفظ مدل معتبر |
| 008 | [nullability و قرارداد داده](lessons/01-foundations/008-nullability/README.md) | قرارداد nullability و دادهٔ ناموجود |
| 009 | [exception و مدیریت منابع](lessons/01-foundations/009-exceptions/README.md) | exception، ownership و dispose منابع |
| 010 | [LINQ و اجرای deferred](lessons/01-foundations/010-linq/README.md) | deferred LINQ، materialization و projection |
| 011 | [فایل و JSON و بازیابی داده](lessons/01-foundations/011-files-json/README.md) | JSON معتبر و نامعتبر و حفظ فایل قبلی |
| 012 | [Task و async/await و لغو عملیات](lessons/01-foundations/012-async/README.md) | await، لغو cooperative و مرز commit فایل |
| 013 | [encapsulation و invariant](lessons/02-design/013-encapsulation/README.md) | encapsulation و رفتار مدل |
| 014 | [composition و inheritance](lessons/02-design/014-composition/README.md) | composition و inheritance و علت انتخاب |
| 015 | [interface و قرارداد](lessons/02-design/015-interfaces/README.md) | قرارداد interface و جایگزینی implementation |
| 016 | [تزریق وابستگی و طول عمر](lessons/02-design/016-dependency-injection/README.md) | lifetime، scope، disposal و captive dependency |
| 017 | [SOLID با بازآرایی یک قابلیت](lessons/02-design/017-solid/README.md) | SOLID در یک تغییر واقعی و مرز گزارش |
| 018 | [record و value object](lessons/02-design/018-value-objects/README.md) | record، value object و معنای برابری |
| 019 | [جداسازی presentation و application و persistence](lessons/02-design/019-separation/README.md) | جداسازی presentation، application و ذخیره |
| 020 | [نتیجهٔ عملیات و exception](lessons/02-design/020-errors/README.md) | result معتبر و تفکیک failure و exception |
| 021 | [بازآرایی و کنترل پیچیدگی](lessons/02-design/021-refactoring/README.md) | refactor و کنترل پیچیدگی با حفظ قرارداد |
| 022 | [تحویل و بازبینی SupportDesk CLI](lessons/02-design/022-cli-delivery/README.md) | تحویل CLI، counter شناسه، فایل خراب و publish |
| 023 | [HTTP و status code و idempotency](lessons/03-web/023-http/README.md) | HTTP، Location، status و محدودیت idempotency |
| 024 | [host و pipeline و تنظیمات اجرا](lessons/03-web/024-web-host/README.md) | host، middleware، ترتیب اجرا و تنظیمات |
| 025 | [routing و binding](lessons/03-web/025-routing/README.md) | route، query، header، JSON و فرق 400/404 |
| 026 | [controller و action و قرارداد API](lessons/03-web/026-controllers/README.md) | Controller، Application و مسیر Location |
| 027 | [DTO و serialization و mapping دستی](lessons/03-web/027-dto/README.md) | DTO و projection و عدم انتشار entity ذخیره |
| 028 | [validation و ProblemDetails](lessons/03-web/028-validation/README.md) | validation ورودی و ProblemDetails |
| 029 | [middleware و مدیریت خطا](lessons/03-web/029-middleware/README.md) | 500 عمومی و request ID و عدم انتشار جزئیات |
| 030 | [configuration و options و secrets](lessons/03-web/030-configuration/README.md) | Options و validation هنگام startup |
| 031 | [DI scope و lifetime در درخواست](lessons/03-web/031-di-scopes/README.md) | scope درخواست و طول عمر state مشترک |
| 032 | [طراحی REST و pagination](lessons/03-web/032-rest/README.md) | pagination مرتب، total و overflow |
| 033 | [OpenAPI و مصرف API](lessons/03-web/033-openapi/README.md) | OpenAPI و تطابق schema و status پاسخ |
| 034 | [تحویل SupportDesk API در حافظه](lessons/03-web/034-api-delivery/README.md) | تحویل API حافظه‌ای، قواعد Domain، concurrency و publish |
| 035 | [مدل رابطه‌ای و کلید و constraint](lessons/04-data/035-relational/README.md) | مدل رابطه‌ای، کلید، NULL و constraint |
| 036 | [SQL و CRUD پارامتری](lessons/04-data/036-sql-basics/README.md) | CRUD پارامتری، متن فارسی و SQL injection fixture |
| 037 | [JOIN و aggregation](lessons/04-data/037-joins/README.md) | JOIN، aggregate و parent بدون child |
| 038 | [index و EXPLAIN و طرح query](lessons/04-data/038-indexes/README.md) | EXPLAIN و buffers بدون فرض plan ثابت |
| 039 | [transaction و isolation](lessons/04-data/039-transactions/README.md) | دو session، isolation، rollback و مشاهدهٔ انتظار lock |
| 040 | [طراحی schema برای درخواست و پیام](lessons/04-data/040-schema/README.md) | schema درخواست و note، FK و unique و policy حذف |
| 041 | [EF Core و DbContext و provider](lessons/04-data/041-ef-core/README.md) | چرخهٔ context و tracker و provider مستقل |
| 042 | [migration و تکامل schema](lessons/04-data/042-migrations/README.md) | migration، snapshot و history و upgrade با داده |
| 043 | [relationship و delete behavior](lessons/04-data/043-relationships/README.md) | relationship و رفتار حذف واقعی |
| 044 | [tracking و projection و N+1](lessons/04-data/044-tracking/README.md) | tracking، projection سمت server و شمارش N+1 |
| 045 | [هم‌زمانی و optimistic concurrency](lessons/04-data/045-concurrency/README.md) | نسخهٔ هم‌زمانی و rollback child در تعارض parent |
| 046 | [مرز data access و بحث repository](lessons/04-data/046-data-access/README.md) | قرارداد data access و محل commit |
| 047 | [Dapper و SQL برای query هدفمند](lessons/04-data/047-dapper/README.md) | گزارش پارامتری Dapper و materialization |
| 048 | [تحویل API پایدار و بررسی خرابی دیتابیس](lessons/04-data/048-data-delivery/README.md) | API PostgreSQL، restart، rule و قطعی دیتابیس |
| 049 | [تشخیص مسئولیت کلاس‌ها](lessons/05-architecture/049-responsibilities/README.md) | دلیل تغییر و مسئولیت کلاس‌ها |
| 050 | [ساختار پوشه‌ای بر اساس لایه یا قابلیت](lessons/05-architecture/050-folders/README.md) | مقایسهٔ layout با حفظ سورس رفتاری |
| 051 | [جداسازی assembly و جهت وابستگی](lessons/05-architecture/051-projects/README.md) | مرز assembly، internal و تخلف compile عمدی |
| 052 | [application service و orchestration](lessons/05-architecture/052-application-services/README.md) | orchestration چند رفتار و یک commit |
| 053 | [قواعد domain و aggregate](lessons/05-architecture/053-domain/README.md) | aggregate و rehydration معتبر و دفاع از collection |
| 054 | [port و adapter و مرز infrastructure](lessons/05-architecture/054-ports/README.md) | قرارداد مشترک adapterها و حفظ فایل خراب |
| 055 | [mapping و هزینهٔ abstraction](lessons/05-architecture/055-mapping/README.md) | mapping صریح و شکل محدود request/response |
| 056 | [vertical slice و مقایسهٔ عملی](lessons/05-architecture/056-vertical-slices/README.md) | feature layout، HTTP واقعی و resolve هم‌زمان |
| 057 | [تفکیک command و query بدون اجبار ابزار](lessons/05-architecture/057-cqrs/README.md) | command/query و pagination و query بدون write |
| 058 | [FluentValidation پس از بررسی سازگاری](lessons/05-architecture/058-validation-library/README.md) | FluentValidation ثابت، errors و حفظ invariant |
| 059 | [تست وابستگی‌های معماری و ADR](lessons/05-architecture/059-architecture-tests/README.md) | policy پروژه و assembly، آزمایش منفی و ADR |
| 060 | [بازآرایی TeamDesk و دفاع از معماری](lessons/05-architecture/060-teamdesk/README.md) | TeamDesk یکپارچه، lifecycle، NextId، restart و publish |

## اجرای بررسی‌ها

از ریشهٔ مخزن:

```powershell
python scripts/check_content.py
python scripts/smoke.py
python scripts/web_smoke.py
python scripts/controller_smoke.py
python scripts/domain_smoke.py
python scripts/architecture_smoke.py
python scripts/architecture_next_smoke.py
python scripts/architecture_final_smoke.py
# با PostgreSQL اختصاصی و تنظیمات راهنمای آزمایش:
python scripts/postgres_smoke.py
python scripts/ef_smoke.py
```

Python 3 و برای اجرای C#، SDK 10 لازم است.
اسکریپت‌های PostgreSQL به اتصال اختصاصی نیاز دارند؛ ef_smoke برای مدیریت schema آزمایش از psql هم استفاده می‌کند.
نام، اتصال، مجوز و پاک‌سازی schema موقت در راهنمای آزمایش شرح داده شده‌اند؛ این فرمان‌ها را به دیتابیس دادهٔ واقعی وصل نکن.
build-only ویندوز شاهد اجرای SQL نیست؛ job PostgreSQL بررسی زنده را جدا اجرا می‌کند.

## کارهای باقی‌ماندهٔ دوره

- امنیت: مدل تهدید، سازمان و حساب، authentication، authorization، مالکیت و کنترل سوءاستفاده؛ درس‌های ۶۱ تا ۷۰.
- آموزش عمیق ابزارهای تست، integration و مشاهده‌پذیری؛ درس‌های ۷۱ تا ۷۸.
- استقرار و عملیات، backup/restore، cache، صف و performance؛ درس‌های ۷۹ تا ۸۶.
- پروژهٔ نهایی مستقل؛ درس‌های ۸۷ تا ۹۰.

تست نفوذ، coverage صددرصد، crash/power-loss durability، هماهنگی چند process برای JSON و برابری رفتار تمام IDEها در این تأیید ادعا نشده‌اند.
برای عبور از هر بخش، تمرین و ارزیابی مستقل همان بخش و دفاع از محل کلاس‌ها و قراردادها همچنان لازم است.
