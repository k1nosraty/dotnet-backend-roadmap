# بازبینی سراسری نگارش فارسی درس‌ها: ۲۰۲۶/۱۱/۰۳

تمام نثر ۹۰ درس اصلی و هر دو درس تکمیلی امنیت، شامل ۰۶۴a و ۰۶۴b، خوانده و بازبینی شد. هر ۹۲ فایل اصلاح شده است؛ جدول پایین، موضوع اصلاح هر درس را ثبت می‌کند. این گزارش جای گزارش اولیهٔ محدود به سه درس را می‌گیرد. مبنای مقایسهٔ این دور، کامیت `4a9fbb4` است.

## معیار نگارش و اصطلاح‌ها

مخاطب، هنرجویی است که پیش‌نیازها را انجام داده ولی مفهوم تازه را هنوز نمی‌شناسد. توضیح باید مسئله را نشان دهد، رفتار ابزار را تعریف کند و ورودی مشخص را تا نتیجه دنبال کند. جمله‌های فشرده‌ای که چند اصطلاح را بدون رابطهٔ روشن کنار هم می‌گذاشتند باز شدند؛ صرفاً تعداد جمله‌ها یا طول متن معیار نبود.

طبق ترجیح کاربر، اصطلاح رایج برنامه‌نویسی به انگلیسی می‌ماند. برای نمونه، constructor، property، interface، namespace، reference، thread، middleware، dependency injection و transaction با توضیح فارسی همراه‌اند. بازنویسی‌های قبلی درس‌های ۷، ۱۲ و ۸۳ نیز با این قاعده هماهنگ شدند. واژه‌های عمومی و جاافتاده مثل کلاس، متد و متغیر حفظ شده‌اند.

اسکیل [persian-writing](https://github.com/ali2000hos/persian-writing)، نسخهٔ 1.4.0، پیش‌تر از کامیت `75fcdf313fab65d6d205500ca50f9cb52db25783` در مسیر مهارت‌های محلی نصب شده است و در این دور نیز راهنمای نگارش بود. ترجیح کاربر دربارهٔ نام‌های انگلیسی بر پیشنهاد ترجمهٔ اصطلاح‌ها مقدم است. قاعدهٔ نگارش در [راهنمای فارسی مخزن](handbook/persian-writing.md)، [قالب درس](templates/lesson.md) و [راهنمای عامل ویرایش](AGENTS.md) هم ثبت شد. عنوان درس‌های ۷ و ۱۶ در فهرست دوره و نقشه هماهنگ شد.

## چرا جمله‌ها سخت فهمیده می‌شدند؟

بعضی تعریف‌ها خودشان به اصطلاح تازه‌ای وابسته بودند؛ مثلاً تعریف property با accessor، پیش از توضیح خواندن و تغییر مقدار. بعضی بخش‌ها نتیجه را می‌گفتند، ولی مسیر رخداد را نشان نمی‌دادند؛ مثلاً چرا تغییر فایل پس از commit با cancellation برنمی‌گردد. در چند بخش نیز نام ابزار، توصیه و هشدار پشت سر هم آمده بودند، بدون اینکه ارتباطشان با مثال روشن باشد.

بازبینی با مثال همان درس انجام شد: دو reference به یک Ticket، تغییر عنوان پیش از خواندن cache، دو UPDATE با نسخهٔ یکسان یا پایان lease پیش از acknowledgment. تعریف‌ها، مراحل و محدودیت‌ها به این مثال‌ها وصل شدند. مخفف‌هایی مثل CQRS، PKCE، RPO و RTO نیز در محل استفاده توضیح داده شدند.

## اصلاح توضیح‌های نادرست یا مبهم

- در درس ۴۳ روشن شد که Ticket خودِ parent است و noteها child هستند؛ جملهٔ قبلی این رابطه را درست بیان نمی‌کرد.
- در درس ۷۴ تفاوت affected rows و commit روشن شد؛ اجرای UPDATE به‌تنهایی معادل تأیید نهایی transaction نیست. محدودیت lookup جدا پس از UPDATE نیز توضیح داده شد.
- در درس ۶۰ مشخص شد دوام پس از restart به adapter فایل مربوط است؛ adapter پیش‌فرض حافظه‌ای داده را پس از پایان process از دست می‌دهد.
- در درس ۵۳ روشن شد public بودن constructor به‌تنهایی مشکل نیست؛ مسئله، مسیر ساخت یا تغییری است که invariant را بررسی نکند.
- در درس ۶۷ متن نمونهٔ اسکریپت در قالب inline code قرار گرفت تا به‌عنوان متن مثال نمایش داده شود.
- در درس‌های تست، مرز درخواست داخل WebApplicationFactory با شبکه و TLS واقعی صریح‌تر شد.

این دور، بازبینی و اصلاح محتوای آموزشی است. تغییری در کد اجرایی نمونه‌ها یا تست‌ها داده نشد. شواهد اجرای فنیِ دور پیشین در [گزارش ممیزی فنی](AUDIT-2026-11.md) باقی است.

## پوشش درس‌به‌درس

در همهٔ ردیف‌ها، علاوه بر نکتهٔ نوشته‌شده، پیوستگی توضیح و کاربرد اصطلاح‌های انگلیسی بررسی شد. پاراگراف‌هایی که در مرورگر به دلیل آغاز لاتین چپ‌به‌راست بودند نیز با جمله‌بندی فارسی اصلاح شدند؛ نام واقعی جزء حفظ شد.

| درس | صفحه | موضوع اصلی بازبینی و اصلاح |
| --- | --- | --- |
| ۰۰۱ | [محیط، SDK، CLI و اولین اجرا](lessons/01-foundations/001-environment/README.md) | تفاوت SDK/runtime/project و مسیر restore، build و run با مثال |
| ۰۰۲ | [نوع‌ها، متغیرها و اولین قابلیت](lessons/01-foundations/002-first-feature/README.md) | توضیح نوع عدد، cast، operand، interpolation و فرهنگ نمایش عدد |
| ۰۰۳ | [ورودی، شرط و اعتبارسنجی](lessons/01-foundations/003-input-validation/README.md) | تفکیک تبدیل ورودی از validation؛ توضیح null و عملگرها با مقدار مشخص |
| ۰۰۴ | [حلقه‌ها و منوی کنسول](lessons/01-foundations/004-loops/README.md) | دنبال‌کردن حلقه، break، پایان ورودی و محل افزایش شمارنده |
| ۰۰۵ | [متدها و جداسازی محاسبه از ورودی](lessons/01-foundations/005-methods/README.md) | تفاوت parameter/argument، local function، scope و مسئولیت caller |
| ۰۰۶ | [List و Dictionary و جست‌وجو](lessons/01-foundations/006-collections/README.md) | تفاوت index و key، generic و iteration؛ تعریف lookup و محدودیت مقدار متنی |
| ۰۰۷ | [کلاس، constructor و property](lessons/01-foundations/007-classes/README.md) | حفظ constructor/property/namespace/reference/enum و دنبال‌کردن دو متغیر تا یک شیء |
| ۰۰۸ | [nullability و قرارداد داده](lessons/01-foundations/008-nullability/README.md) | تفاوت هشدار nullable با خطای runtime و نمایش دقیق عملگرهای null |
| ۰۰۹ | [exception و مدیریت منابع](lessons/01-foundations/009-exceptions/README.md) | توضیح exception، stack trace، cleanup، IDisposable، buffer و ownership |
| ۰۱۰ | [LINQ و اجرای deferred](lessons/01-foundations/010-linq/README.md) | توضیح query، deferred execution، enumeration و snapshot با تغییر دادهٔ منبع |
| ۰۱۱ | [فایل و JSON و بازیابی داده](lessons/01-foundations/011-files-json/README.md) | تفکیک Deserialize از اعتبارسنجی داده و توضیح mapper و فایل موقت |
| ۰۱۲ | [Task و async/await و لغو عملیات](lessons/01-foundations/012-async/README.md) | حفظ thread و Task؛ توضیح await، ترتیب اجرا و تفاوت commit فایل با transaction |
| ۰۱۳ | [encapsulation و invariant](lessons/02-design/013-encapsulation/README.md) | توضیح invariant و encapsulation؛ تفکیک محافظت collection از عنصرهایش |
| ۰۱۴ | [composition و inheritance](lessons/02-design/014-composition/README.md) | توضیح inheritance/composition و رفتار polymorphism در گزارش |
| ۰۱۵ | [interface و قرارداد](lessons/02-design/015-interfaces/README.md) | تعریف interface از نیاز مصرف‌کننده و رفتار مشترک implementationها |
| ۰۱۶ | [Dependency Injection و lifetime](lessons/02-design/016-dependency-injection/README.md) | تعریف DI، dependency graph، scope، singleton و captive dependency |
| ۰۱۷ | [SOLID با بازآرایی یک قابلیت](lessons/02-design/017-solid/README.md) | بازکردن نام‌های SOLID و توضیح coupling با مسئولیت مشخص کلاس‌ها |
| ۰۱۸ | [record و value object](lessons/02-design/018-value-objects/README.md) | تفاوت entity/value object، record equality، init و mutable collection |
| ۰۱۹ | [جداسازی presentation و application و persistence](lessons/02-design/019-separation/README.md) | تفکیک لایه‌ها، وابستگی کد و ترتیب اجرای application/persistence |
| ۰۲۰ | [نتیجهٔ عملیات و exception](lessons/02-design/020-errors/README.md) | معنای Outcome و تفکیک failure قابل انتظار از exception زیرساخت |
| ۰۲۱ | [بازآرایی و کنترل پیچیدگی](lessons/02-design/021-refactoring/README.md) | شرح parse، dispatch و render و مرز parser با اثر جانبی |
| ۰۲۲ | [تحویل و بازبینی SupportDesk CLI](lessons/02-design/022-cli-delivery/README.md) | توضیح snapshot، lost update، commit فایل و محدودیت تحویل CLI |
| ۰۲۳ | [HTTP و status code و idempotency](lessons/03-web/023-http/README.md) | توضیح request/response، representation و safe/idempotent بودن عملیات |
| ۰۲۴ | [host و pipeline و تنظیمات اجرا](lessons/03-web/024-web-host/README.md) | نقش host و pipeline و معنای short-circuit در middleware |
| ۰۲۵ | [routing و binding](lessons/03-web/025-routing/README.md) | تفکیک routing، binding و validation و نقش FromServices |
| ۰۲۶ | [controller و action و قرارداد API](lessons/03-web/026-controllers/README.md) | نقش controller/action، constructor injection و lifetime دادهٔ حافظه‌ای |
| ۰۲۷ | [DTO و serialization و mapping دستی](lessons/03-web/027-dto/README.md) | نقش DTO در محدودکردن ورودی و خروجی و رفتار serialization |
| ۰۲۸ | [validation و ProblemDetails](lessons/03-web/028-validation/README.md) | تفکیک raw و normalized، ModelState و metadata روی constructor در record |
| ۰۲۹ | [middleware و مدیریت خطا](lessons/03-web/029-middleware/README.md) | ترتیب callback، OnStarting و پاسخ خطا در pipeline |
| ۰۳۰ | [configuration و options و secrets](lessons/03-web/030-configuration/README.md) | توضیح typed options، configuration و شکست زودهنگام startup |
| ۰۳۱ | [DI scope و lifetime در درخواست](lessons/03-web/031-di-scopes/README.md) | تفاوت scope درخواست با lifetime کاربر و معنای captive dependency |
| ۰۳۲ | [طراحی REST و pagination](lessons/03-web/032-rest/README.md) | توضیح offset/cursor، صفحه، شمار کل و ترتیب پایدار |
| ۰۳۳ | [OpenAPI و مصرف API](lessons/03-web/033-openapi/README.md) | تفکیک OpenAPI/schema/metadata و تغییر ناسازگار قرارداد |
| ۰۳۴ | [تحویل SupportDesk API در حافظه](lessons/03-web/034-api-delivery/README.md) | توضیح انتقال وضعیت، تغییر بی‌اثر و محدودهٔ lock |
| ۰۳۵ | [مدل رابطه‌ای و کلید و constraint](lessons/04-data/035-relational/README.md) | تفاوت database/schema/table و نقش constraint، identity و sequence |
| ۰۳۶ | [SQL و CRUD پارامتری](lessons/04-data/036-sql-basics/README.md) | تفاوت driver/ORM، connection pool، reader و parameter SQL |
| ۰۳۷ | [JOIN و aggregation](lessons/04-data/037-joins/README.md) | توضیح JOIN و aggregation و اثر جای شرط روی parent/child |
| ۰۳۸ | [index و EXPLAIN و طرح query](lessons/04-data/038-indexes/README.md) | توضیح index، plan و هزینهٔ خواندن/نوشتن با دادهٔ مشخص |
| ۰۳۹ | [transaction و isolation](lessons/04-data/039-transactions/README.md) | تعریف statement/transaction، ACID، isolation و snapshot با دو connection |
| ۰۴۰ | [طراحی schema برای درخواست و پیام](lessons/04-data/040-schema/README.md) | توضیح normalization، کلید ترکیبی، collation و مرز schema با migration |
| ۰۴۱ | [EF Core و DbContext و provider](lessons/04-data/041-ef-core/README.md) | نقش DbContext/DbSet، tracking و materialize در برابر ذخیرهٔ واقعی |
| ۰۴۲ | [migration و تکامل schema](lessons/04-data/042-migrations/README.md) | تفاوت migration و snapshot و مراحل expand/backfill/enforce |
| ۰۴۳ | [relationship و delete behavior](lessons/04-data/043-relationships/README.md) | اصلاح توضیح parent/child و تفکیک navigation از foreign key |
| ۰۴۴ | [tracking و projection و N+1](lessons/04-data/044-tracking/README.md) | توضیح tracking، projection و مسئلهٔ N+1 |
| ۰۴۵ | [هم‌زمانی و optimistic concurrency](lessons/04-data/045-concurrency/README.md) | توضیح optimistic concurrency، Revision و تصمیم caller پس از conflict |
| ۰۴۶ | [مرز data access و بحث repository](lessons/04-data/046-data-access/README.md) | تعریف port خواندن و مرز fake با رفتار واقعی دیتابیس |
| ۰۴۷ | [Dapper و SQL برای query هدفمند](lessons/04-data/047-dapper/README.md) | توضیح buffered query، transaction و عمر connection در Dapper |
| ۰۴۸ | [تحویل API پایدار و بررسی خرابی دیتابیس](lessons/04-data/048-data-delivery/README.md) | توضیح readiness، DDL/DML، reconciliation و محدودیت lookup جدا |
| ۰۴۹ | [تشخیص مسئولیت کلاس‌ها](lessons/05-architecture/049-responsibilities/README.md) | توضیح cohesion/coupling، SRP و compare-and-swap در مرز ذخیره |
| ۰۵۰ | [ساختار پوشه‌ای بر اساس لایه یا قابلیت](lessons/05-architecture/050-folders/README.md) | تفاوت چیدمان feature/layer و دلیل ثبت تصمیم در ADR |
| ۰۵۱ | [جداسازی assembly و جهت وابستگی](lessons/05-architecture/051-projects/README.md) | تفاوت folder/namespace/project/assembly و reference مستقیم و transitive |
| ۰۵۲ | [application service و orchestration](lessons/05-architecture/052-application-services/README.md) | توضیح orchestration و مراحل use case؛ محدودهٔ test double |
| ۰۵۳ | [قواعد domain و aggregate](lessons/05-architecture/053-domain/README.md) | توضیح rehydration و invariant؛ دقیق‌کردن شرط نامعتبرشدن مدل با constructor |
| ۰۵۴ | [port و adapter و مرز infrastructure](lessons/05-architecture/054-ports/README.md) | تعریف port/adapter و قرارداد مشترک دادهٔ غایب، conflict، لغو و commit |
| ۰۵۵ | [mapping و هزینهٔ abstraction](lessons/05-architecture/055-mapping/README.md) | تفکیک mapping ورودی، projection خروجی و mapping ذخیره؛ مرز validation |
| ۰۵۶ | [vertical slice و مقایسهٔ عملی](lessons/05-architecture/056-vertical-slices/README.md) | توضیح vertical slice و محدودهٔ handler و تغییر اتمیک نسخه |
| ۰۵۷ | [تفکیک command و query بدون اجبار ابزار](lessons/05-architecture/057-cqrs/README.md) | بازکردن CQRS و تفکیک مدل خواندن/نوشتن و تأخیر احتمالی خواندن |
| ۰۵۸ | [FluentValidation پس از بررسی سازگاری](lessons/05-architecture/058-validation-library/README.md) | تعریف validator و RuleFor و علت حفظ قواعد داخل Domain |
| ۰۵۹ | [تست وابستگی‌های معماری و ADR](lessons/05-architecture/059-architecture-tests/README.md) | توضیح architecture test، exported type و اجزای ADR |
| ۰۶۰ | [بازآرایی TeamDesk و دفاع از معماری](lessons/05-architecture/060-teamdesk/README.md) | تفکیک adapter پیش‌فرض حافظه‌ای از فایل و دوام پس از restart |
| ۰۶۱ | [مدل تهدید و مرز اعتماد](lessons/06-security/061-threat-model/README.md) | توضیح مرز اعتماد، credential و رفتار fail-closed |
| ۰۶۲ | [Identity و مدیریت امن حساب](lessons/06-security/062-identity/README.md) | نقش Identity، lockout و تفاوت بررسی password با ورود کامل |
| ۰۶۳ | [hash رمز و سیاست بازیابی](lessons/06-security/063-passwords/README.md) | تفکیک hash، encryption و encoding و نقش salt و token |
| ۰۶۴ | [cookie و bearer و انتخاب روش](lessons/06-security/064-authentication/README.md) | تفکیک scheme، token ورود و cookie و توضیح refresh/revocation |
| ۰۶۴a | [JWT و اعتبارسنجی API](lessons/06-security/064a-jwt/README.md) | توضیح JWS، alg، clock skew و scope و مرز JWT با مجوز منبع |
| ۰۶۴b | [OAuth/OIDC و Authorization Code با PKCE](lessons/06-security/064b-oidc/README.md) | شرح PKCE، code verifier/challenge، PAR و محدودیت redirect پس از ورود |
| ۰۶۵ | [policy و نقش و مجوز](lessons/06-security/065-authorization/README.md) | تعریف principal/policy/requirement و رابطهٔ AND و OR شرط‌ها |
| ۰۶۶ | [کنترل مالکیت و جلوگیری از IDOR](lessons/06-security/066-ownership/README.md) | شرح ownership/tenant و resource-based authorization و خطر IDOR |
| ۰۶۷ | [CORS و CSRF و injection](lessons/06-security/067-web-security/README.md) | توضیح preflight/CORS و نمایش literal اسکریپت به‌صورت inline code |
| ۰۶۸ | [rate limiting و کنترل سوءاستفاده](lessons/06-security/068-rate-limits/README.md) | توضیح partition، fixed window، backoff و محدودهٔ budget حافظه‌ای |
| ۰۶۹ | [secrets و audit و دادهٔ حساس](lessons/06-security/069-secrets/README.md) | تفکیک log و audit پایدار؛ توضیح canary و WORM و حفاظت secret |
| ۰۷۰ | [بازبینی امنیت و سناریوهای منفی](lessons/06-security/070-security-review/README.md) | توضیح ترتیب review و آزمون تغییر مجوز با credential موجود |
| ۰۷۱ | [unit test و xUnit](lessons/07-testing/071-unit/README.md) | توضیح unit test، SUT، TRX و تفاوت scalar با string.Length |
| ۰۷۲ | [boundary و negative case و test double](lessons/07-testing/072-test-design/README.md) | تفکیک نتیجهٔ use case، state و side effect از قرارداد HTTP |
| ۰۷۳ | [integration و WebApplicationFactory](lessons/07-testing/073-integration/README.md) | توضیح integration test، test host، DI override و مرز هویت تست |
| ۰۷۴ | [تست دیتابیس و Testcontainers](lessons/07-testing/074-database-tests/README.md) | تفکیک affected rows از commit؛ نقش fixture، Testcontainers و WebApplicationFactory |
| ۰۷۵ | [HttpClientFactory و timeout و resilience](lessons/07-testing/075-http-resilience/README.md) | تفاوت تلاش مجدد و تلاش اول، transport ساختگی و circuit breaker |
| ۰۷۶ | [BackgroundService و scope و shutdown](lessons/07-testing/076-background-work/README.md) | توضیح job/worker/Channel، توقف پذیرش و drain صف |
| ۰۷۷ | [ILogger و structured logging و Serilog](lessons/07-testing/077-logging/README.md) | نقش sink و logging scope و جلوگیری از ثبت payload |
| ۰۷۸ | [debugger و trace و بازتولید خطا](lessons/07-testing/078-debugging/README.md) | توضیح unchecked overflow، conditional breakpoint و tagهای span |
| ۰۷۹ | [Docker و image و محیط اجرا](lessons/08-production/079-docker/README.md) | شرح Docker layer/cache، build context، read-only و tmpfs |
| ۰۸۰ | [publish و reverse proxy و TLS](lessons/08-production/080-deployment/README.md) | تعریف TLS termination، CA، peer، SNI و محدودهٔ تست HTTP و TLS |
| ۰۸۱ | [CI/CD و quality gate](lessons/08-production/081-ci/README.md) | تعریف CI/runner/job/artifact/manifest، hash و framework-dependent publish |
| ۰۸۲ | [OpenTelemetry و health check](lessons/08-production/082-observability/README.md) | تفاوت log/trace/metric/span، health Predicate و cardinality |
| ۰۸۳ | [cache و invalidation و Redis](lessons/08-production/083-caching/README.md) | حفظ cache/async؛ توضیح cache hit/miss، نسخه، invalidation و TTL |
| ۰۸۴ | [صف و پردازش job و ارزیابی Hangfire یا Quartz](lessons/08-production/084-jobs/README.md) | توضیح claim/lease/polling و تفکیک outbox/inbox/reconciliation |
| ۰۸۵ | [load test و profiling و بودجهٔ latency](lessons/08-production/085-performance/README.md) | تعریف latency/throughput/warmup/profiler و شرح coordinated omission |
| ۰۸۶ | [backup و restore و rollback و تحویل worker](lessons/08-production/086-operations/README.md) | تفاوت backup/restore/rollback و شرح expand/contract، epoch، RPO/RTO/PITR/WAL |
| ۰۸۷ | [تحلیل ServiceDesk و معیار پذیرش](lessons/09-capstone/087-requirements/README.md) | بازکردن سناریوی درخواست؛ تعریف Actor، مرز اعتماد و ماتریس مجوز |
| ۰۸۸ | [پیاده‌سازی مستقل و review](lessons/09-capstone/088-implementation/README.md) | تعریف snapshot و vertical slice و مرز DTO/entity و tenant/owner/key |
| ۰۸۹ | [تست و امنیت و عملکرد](lessons/09-capstone/089-hardening/README.md) | تفکیک concurrency token از token ورود و توضیح grant revocation |
| ۰۹۰ | [استقرار و مستندات و دفاع فنی](lessons/09-capstone/090-delivery/README.md) | توضیح proxy/loopback/CA، key ring و اجزای قابل بررسی تحویل |

## بررسی نگارش، کد و نمایش

ابزارهای `persian_cleanup.py --edit` و `fa_lint.py --check` روی هر ۹۲ درس اجرا شدند. پاک‌سازی فقط روی نسخه‌های موقت و با محافظت از مقصد پیوندها بود؛ پیشنهادها خودکار وارد منبع نشدند. ابزار در چند مورد شمارهٔ نسخه، literal یا عملگر را اشتباه تغییر می‌داد و برای واژه‌های درست «آگاهانه» و «فراخواننده» هشدار نادرست داشت. نتیجهٔ linter به‌عنوان قبولی کامل گزارش نمی‌شود؛ هشدارها با توجه به متن بررسی شدند. محدودیت پاک‌سازی در راهنمای نگارش ثبت است.

مقایسهٔ مستقیم با مبنا، ثابت‌ماندن هر ۵۰۸ بلوک fenced شامل کد، فرمان، خروجی و نمودار و مقصد هر ۶۶۷ پیوند Markdown در درس‌ها را تأیید کرد. ۴۹ نمودار موجود و پرسش‌های بازشدنی کنارشان حفظ شدند.

بررسی محتوای مخزن با `python3 scripts/check_content.py` و بررسی فاصله‌ها با `git diff --check` انجام شد. این بررسی شامل ۹۰ ورودی دوره، ۸۹ پروژهٔ نمونه، ۱۶ پروژهٔ تست و هر دو درس تکمیلی است؛ تطابق فرمان‌ها به معنی اجرای دوبارهٔ پروژه‌ها نیست.

هر ۹۲ درس در README Viewer محلی نسخهٔ 1.5.1 با Chrome روی Linux، بدون دسترسی شبکه، نمایش داده شد. بررسی‌ها شامل حالت روشن با عرض ۱۱۰۰، حالت تیره با عرض ۳۹۰ پیکسل، جهت پاراگراف‌های فارسی، نبود سرریز افقی صفحه، بارگذاری هر ۴۹ نمودار و بازوبسته‌شدن پاسخ‌ها بود. تصاویر چند درس نیز برای خوانایی مرور شدند.

تست‌های اجرایی .NET، Docker و PostgreSQL در این دور دوباره اجرا نشدند، چون کد، فرمان‌ها و خروجی‌های نمونه ثابت ماندند. اجرای برنامهٔ بومی Windows و آزمون یادگیری با هنرجو انجام نشده است. بررسی نمایش و صحت فنی، جای بازخورد خواننده و حل مستقل تمرین را نمی‌گیرد.
