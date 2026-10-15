# ممیزی آموزشی و فنی درس‌های ۰۰۱ تا ۰۳۴

## نتیجهٔ ارزیابی

این ۳۴ درس پس از اصلاحات این ممیزی، برای محدودهٔ سه بخش اول قابل تأیید هستند:
مبانی C#، طراحی و تحویل CLI، و ساخت API حافظه‌ای با ASP.NET Core.
ترتیب مفاهیم، نمونهٔ قابل اجرا، تمرین مستقل، failure case و معیار عبور برای این محدوده وجود دارد.
این تأیید به معنی کامل شدن مسیر بک‌اند حرفه‌ای، بی‌نقص بودن تمام حالت‌های ممکن یا تسلط فراگیر بدون تمرین نیست.

## دامنه و روش بررسی

- متن ۳۴ درس، فهرست‌ها، راهنماهای ساختار و کتابخانه و قرارداد تحویل‌ها مرور شد.
- ۳۳ پروژهٔ مستقل و ۲۱۴ فایل C# بررسی شدند؛ کدهای یکسان در snapshotها با مقایسهٔ محتوا تشخیص داده شدند.
- توضیح هر مرحله با مدل و مسیر اجرای نمونه، ورودی نامعتبر و حفظ state مقایسه شد.
- پیوندهای داخلی و مسیر csprojهای نوشته‌شده در فرمان‌ها به صورت خودکار بررسی می‌شوند.
- catalog نمونه‌ها هر درس نوشته‌شدهٔ بعد از راه‌اندازی را به پروژهٔ اجرایی همان درس وصل می‌کند.
- ساخت و رفتار کدها با اسکریپت‌های موجود و تست‌های مستقل جدید بررسی می‌شود؛ نتیجهٔ واقعی CI پایین ثبت شده است.

## یافته‌ها و اصلاحات

| یافته | اصلاح | شاهد بررسی |
| --- | --- | --- |
| API نهایی اجازهٔ ویرایش Closed و noteهای خارج از قواعد CLI را می‌داد | طول ۳ تا ۵۰۰، سقف پنج، مقایسهٔ Ordinal و منع rename/note پس از Closed در Domain حفظ شد | consumer مستقل و HTTP؛ رد شدن باید state را ثابت نگه دارد |
| factory مدل‌های controller می‌توانست Id نامعتبر بسازد و null فقط به dereference می‌رسید | guard Id مثبت و ArgumentNullException در مدل‌های درس ۲۶ تا ۳۴ | domain_smoke بدون controller |
| MutationResult آزاد، Success بدون value یا kind نامعتبر را قابل ساخت می‌کرد | constructor خصوصی، factoryهای کنترل‌شده و Value محدود به success | رد Success(null) و Value در failure |
| callback ذخیره‌سازی می‌توانست مدل با Id متفاوت را جای کلید موجود بنشاند | بررسی هویت پیش از جایگزینی در store | failure بدون تغییر Ticket قبلی |
| فقط ساخته شدن روی Linux بررسی شده بود، در حالی که آموزش Windows-first است | matrix ویندوز و لینوکس با Python UTF-8 | workflow Check course |
| بعضی syntaxهای delegate، primary constructor و extension خیلی فشرده معرفی می‌شدند | مرجع syntax با نمونهٔ بازشده، event، ValueTask و lock و پیوند از درس‌ها | handbook/csharp-syntax.md |
| دریافت مرجع و ثبت نسخهٔ تمرین مسیر روشن مستقلی نداشت | راهنمای Git محلی، stage/diff/commit/branch و جداسازی تمرین از مرجع | handbook/git-practice.md |
| لینک معتبر به تنهایی صحیح بودن فرمان نمونه را ثابت نمی‌کرد | catalog درس/پروژه و بررسی مسیرهای csproj داخل Markdown | check_content.py |
| محدودهٔ API نسبت به CLI می‌توانست مبهم خوانده شود | ماتریس قابلیت‌ها؛ اولویت و گزارش CLI هنوز منتقل نشده‌اند و persistence API برای بخش چهارم است | فهرست بخش سوم و درس ۰۳۴ |

## نتیجهٔ بررسی هر درس

این جدول نتیجهٔ مرور محتوای هر درس است؛ «خوانده شد» را با اجرای همهٔ تمرین‌های آزاد فراگیر یکی نمی‌کند.

| درس | موضوع | موارد بررسی‌شده |
| --- | --- | --- |
| 001 | [محیط، SDK، CLI و اولین اجرا](lessons/01-foundations/001-environment/README.md) | نصب، SDK/runtime، build/run، خطای کامپایل و debugger؛ راهنمای Git متصل شد. |
| 002 | [نوع‌ها، متغیرها و اولین قابلیت](lessons/01-foundations/002-first-feature/README.md) | نوع، تقسیم صحیح، decimal، culture و حالت صفر؛ نمونه و تمرین هزینه. |
| 003 | [ورودی، شرط و اعتبارسنجی](lessons/01-foundations/003-input-validation/README.md) | null/empty، TryParse، فرهنگ عددی، normalization و مرز طول. |
| 004 | [حلقه‌ها و منوی کنسول](lessons/01-foundations/004-loops/README.md) | حلقه، switch، break/continue، retry و EOF بدون حلقهٔ سریع. |
| 005 | [متدها و جداسازی محاسبه از ورودی](lessons/01-foundations/005-methods/README.md) | ورودی/قاعده/نمایش جدا؛ قرارداد متد و refactoring با حفظ رفتار. |
| 006 | [List و Dictionary و جست‌وجو](lessons/01-foundations/006-collections/README.md) | List/Dictionary، شناسه مستقل از index، حذف و ثبت مجدد و lookup ناموجود. |
| 007 | [کلاس، سازنده و property](lessons/01-foundations/007-classes/README.md) | کلاس، property، constructor، enum، namespace و alias/reference. |
| 008 | [nullability و قرارداد داده](lessons/01-foundations/008-nullability/README.md) | nullable reference/value، فهرست خالی و قرارداد نتیجهٔ ناموجود. |
| 009 | [exception و مدیریت منابع](lessons/01-foundations/009-exceptions/README.md) | خطای دقیق، throw، using/Dispose و ownership منابع. |
| 010 | [LINQ و اجرای deferred](lessons/01-foundations/010-linq/README.md) | Where/Select/OrderBy، deferred execution، shallow snapshot و گزارش صفرها. |
| 011 | [فایل و JSON و بازیابی داده](lessons/01-foundations/011-files-json/README.md) | JSON خارجی، schema، duplicate Id، فایل خراب و نوشتن مرحله‌ای. |
| 012 | [Task و async/await و لغو عملیات](lessons/01-foundations/012-async/README.md) | زنجیرهٔ async، token، pre-cancel و مرز commit؛ محدودیت ReadLine روشن است. |
| 013 | [encapsulation و invariant](lessons/02-design/013-encapsulation/README.md) | invariant، wrapper collection، ظرفیت، duplicate و state پس از شکست. |
| 014 | [composition و inheritance](lessons/02-design/014-composition/README.md) | composition در برابر inheritance؛ formatter بدون mutation و policy مستقل. |
| 015 | [interface و قرارداد](lessons/02-design/015-interfaces/README.md) | interface کوچک، جایگزینی implementation و failure جدا از null. |
| 016 | [تزریق وابستگی و طول عمر](lessons/02-design/016-dependency-injection/README.md) | DI، scope، disposal، captive dependency و محدودیت validation container. |
| 017 | [SOLID با بازآرایی یک قابلیت](lessons/02-design/017-solid/README.md) | SOLID با تغییر واقعی، جهت وابستگی و حفظ رفتار refactor. |
| 018 | [record و value object](lessons/02-design/018-value-objects/README.md) | value object، equality، with، shallow copy و جلوگیری از تغییر value نامعتبر. |
| 019 | [جداسازی presentation و application و persistence](lessons/02-design/019-separation/README.md) | مرزهای presentation/application/domain/persistence و Saved پس از موفقیت I/O. |
| 020 | [نتیجهٔ عملیات و exception](lessons/02-design/020-errors/README.md) | Outcome محافظت‌شده، TryCreate، error code و exception/cancellation مستقل. |
| 021 | [بازآرایی و کنترل پیچیدگی](lessons/02-design/021-refactoring/README.md) | parse/dispatch/render، type pattern و قرارداد helper mutation. |
| 022 | [تحویل و بازبینی SupportDesk CLI](lessons/02-design/022-cli-delivery/README.md) | CLI یکپارچه، schema الزامی، NextId پایدار، restart، failure و publish. |
| 023 | [HTTP و status code و idempotency](lessons/03-web/023-http/README.md) | پیام HTTP، status/header/body، safe/idempotent و retry نامطمئن POST. |
| 024 | [host و pipeline و تنظیمات اجرا](lessons/03-web/024-web-host/README.md) | host، middleware، configuration precedence و environment/lifetime. |
| 025 | [routing و binding](lessons/03-web/025-routing/README.md) | route/query/header/body/service binding و تفاوت ۴۰۰/۴۰۴/۴۰۵/۴۱۵. |
| 026 | [controller و action و قرارداد API](lessons/03-web/026-controllers/README.md) | Controller و service/store جدا؛ factory Domain برای Id و null تقویت شد. |
| 027 | [DTO و serialization و mapping دستی](lessons/03-web/027-dto/README.md) | DTO/entity جدا، mapping دستی، extension و PUT زیرمنبع title. |
| 028 | [validation و ProblemDetails](lessons/03-web/028-validation/README.md) | DataAnnotations روی class، raw در برابر Trim و ValidationProblemDetails. |
| 029 | [middleware و مدیریت خطا](lessons/03-web/029-middleware/README.md) | handler ۵۰۰، عدم نشت جزئیات، OnStarting و تطبیق requestId. |
| 030 | [configuration و options و secrets](lessons/03-web/030-configuration/README.md) | Options، ValidateOnStart، env override، failure startup و secrets محلی. |
| 031 | [DI scope و lifetime در درخواست](lessons/03-web/031-di-scopes/README.md) | scope واقعی request، instance probe، store singleton و thread safety. |
| 032 | [طراحی REST و pagination](lessons/03-web/032-rest/README.md) | filter/count/page با ترتیب قطعی، offset بزرگ و محدودیت snapshot بین requestها. |
| 033 | [OpenAPI و مصرف API](lessons/03-web/033-openapi/README.md) | OpenAPI، schema/status metadata، diagnostics exclusion و dependency audit. |
| 034 | [تحویل SupportDesk API در حافظه](lessons/03-web/034-api-delivery/README.md) | API یکپارچه؛ قواعد note/Closed با CLI یکسان و MutationResult محافظت شد. |

## شاهد اجرای بررسی‌ها

کد اصلاح‌شده در [commit 4c4e70c](https://github.com/k1nosraty/dotnet-backend-roadmap/commit/4c4e70c6a90ae9fc97ce45234d8956b726e19720) بررسی شد.
[اجرای Check course](https://github.com/k1nosraty/dotnet-backend-roadmap/actions/runs/38803781292) روی هر دو runner با موفقیت تمام شد.

| بررسی | Windows | Linux |
| --- | --- | --- |
| metadata، لینک داخلی و catalog نمونه‌ها | موفق | موفق |
| ساخت و رفتار نمونه‌های CLI بخش اول و دوم | موفق | موفق |
| نمونه‌های HTTP پایه | موفق | موفق |
| snapshotهای controller، pagination و OpenAPI | موفق | موفق |
| note و status، duplicate، ظرفیت و درخواست هم‌زمان | موفق | موفق |
| consumer مستقل Domain و محافظت نتیجه | موفق | موفق |
| publish و اجرای CLI و API خارج از source | موفق | موفق |

این گزارش و توضیح‌های تکمیلی پس از ثبت نتیجه به repository اضافه شده‌اند؛ گزارش، جای log اجرای لینک‌شده را نمی‌گیرد.

فرمان‌های تکرارپذیر از ریشهٔ دوره:

```powershell
python scripts/check_content.py
python scripts/smoke.py
python scripts/web_smoke.py
python scripts/controller_smoke.py
python scripts/domain_smoke.py
```

چک ساختاری به Python 3 نیاز دارد؛ اسکریپت‌های smoke به SDK 10 هم نیاز دارند.
همهٔ داده‌های تست در پوشه‌های موقت‌اند.
نمونه‌ها روی HTTP loopback اجرا و processها پس از بررسی بسته می‌شوند.
خروجی‌های framework-dependent مربوط به CLI و API publish و از پوشه‌ای جدا از source اجرا می‌شوند.

## پوشش تا این مرحله و کارهای باقی‌مانده

| مهارت | وضعیت تا درس ۳۴ |
| --- | --- |
| پایهٔ زبان لازم برای خواندن و توسعهٔ نمونه‌ها | پوشش داده شده؛ مرجع syntax نقاط فشرده را باز می‌کند |
| منطق مدل، مرز کلاس‌ها، interface و DI | پوشش داده شده و با چند تحویل تمرین دارد |
| HTTP، Controller، DTO، validation و failure | پوشش داده شده برای API محلی این بخش |
| data access رابطه‌ای، SQL، PostgreSQL و EF Core | هنوز نوشته نشده؛ درس‌های ۳۵ تا ۴۸ |
| چند assembly، معماری بزرگ‌تر و دفاع از tradeoffها | مقدمات دارد؛ تکمیل در درس‌های ۴۹ تا ۶۰ |
| authentication، authorization و امنیت محصول | هنوز نوشته نشده؛ درس‌های ۶۱ تا ۷۰ |
| xUnit، WebApplicationFactory، Testcontainers و یکپارچه‌سازی | تست‌های مرجع موجودند؛ آموزش ابزارها درس‌های ۷۱ تا ۷۸ |
| استقرار، Docker، عملیات، backup، cache و performance | هنوز نوشته نشده؛ درس‌های ۷۹ تا ۸۶ |
| تحویل مستقل بدون کپی مرجع | ارزیابی‌های بخشی دارد؛ پروژهٔ نهایی درس‌های ۸۷ تا ۹۰ |

## حدود این تأیید

تست‌های خودکار جای حل تمرین‌های آزاد، review پروژهٔ مستقل، تجربهٔ کار تیمی یا ارزیابی production را نمی‌گیرند.
پوشش شاخه‌ای صددرصد، تست نفوذ، benchmark و تحمل crash/قطع برق در این ممیزی ادعا نشده است.
همهٔ شکل‌های Accept و تمام clientها و IDEها به صورت دستی بررسی نشده‌اند.
لینک‌های داخلی و فرمان‌های پروژه بررسی شده‌اند؛ همهٔ صفحات خارجی و تغییرات آیندهٔ منابع تحت کنترل دوره نیستند.
JSON تک‌نویسندهٔ CLI و store حافظه‌ای API عمداً جای پایگاه دادهٔ چندکاربره نیستند.
برای عبور، هر تحویل را مستقل بساز، جدول پذیرش را اجرا و انتخاب محل کلاس‌ها را با نام فایل‌های خودت توضیح بده.
