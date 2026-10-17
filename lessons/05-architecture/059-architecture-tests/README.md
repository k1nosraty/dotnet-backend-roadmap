# درس 059 — تست وابستگی‌های معماری و ADR

## خروجی قابل مشاهده

جهت reference پروژه‌ها، packageهای مجاز و وابستگی assemblyهای ساخته‌شده را بررسی می‌کنی.
یک تخلف عمدی در کپی موقتِ Domain به‌طور خودکار رد می‌شود.
تصمیم‌های مهم را با زمینه و جایگزین و پیامد در ADR ثبت می‌کنی.

پیش‌نیاز: [درس 058](../058-validation-library/README.md)، assembly و ProjectReference.
این درس مقدمهٔ تست معماری است؛ فصل تست بعداً xUnit و تست‌دابل و integration را عمیق‌تر آموزش می‌دهد.
برای این policy کوچک ابزار خارجی تست معماری نصب نمی‌کنیم.

## مسئلهٔ convention شفاهی

اگر فقط بگوییم «Domain نباید وب را بشناسد»، افزودن reference اشتباه ممکن است در review گم شود.
تست معماری بخشی از قراردادهای تیم را executable می‌کند.
اما قانون مبهم یا policy بیش از حد باز، فقط ظاهری از اطمینان می‌سازد.
ابتدا ممنوعیت دقیق را بنویس، سپس شاهد مناسبش را انتخاب کن.

## سه سطح بررسی

| سطح | شاهد این نمونه | چه چیزی را نمی‌فهمد؟ |
| --- | --- | --- |
| project metadata | XML csproj | استفادهٔ واقعی typeها در کد |
| assembly ساخته‌شده | GetReferencedAssemblies | همهٔ reflection و dynamic loadها |
| رفتار برنامه | HTTP و سناریوهای Domain | دلیل انتخاب معماری |

ترکیب این شاهدها مفید است؛ هیچ‌کدام به‌تنهایی «معماری کاملاً درست» را ثابت نمی‌کند.
نام یک type یا مسیر پوشه هم جای dependency graph را نمی‌گیرد.

## policy صریح پروژه

| پروژه | reference مستقیم مجاز | package مجاز |
| --- | --- | --- |
| Domain | هیچ‌کدام | هیچ‌کدام |
| Application | Domain | FluentValidation 12.2.0 |
| Infrastructure | Application و Domain | در این snapshot هیچ‌کدام |
| host | اجزای composition | runtime ASP.NET Core |

اسکریپت XML را با parser می‌خواند؛ جست‌وجوی سادهٔ متنِ using مبنای اصلی نیست.
ProjectReference اضافی حتی اگر در کد استفاده نشده باشد، policy مستقیم را نقض می‌کند.
package مجاز هم باید همان نسخهٔ ثابت باشد؛ تغییر dependency یک تصمیم قابل review است.
این policy مخصوص نمونه است؛ برای adapter PostgreSQL، referenceهای provider باید با نیاز واقعی و ADR جدید اضافه شوند.

## بررسی assembly کامپایل‌شده

ArchitectureChecks.cs از GetReferencedAssemblies استفاده می‌کند.
نام referenceهای SupportDesk.* را با مجموعهٔ مورد انتظار مقایسه می‌کند.
همچنین نشت ASP.NET، EF و Npgsql به assemblyهای این نمونه را رد می‌کند.
برای Domain وابستگی FluentValidation هم مجاز نیست؛ محل آن Application است.
adapterهای InMemoryTicketStore و JsonTicketStore نباید exported type عمومی باشند.
این تست سورس را از محل اجرای برنامه نمی‌خواند؛ در خروجی publish هم قابل اجراست.

## آزمون منفی خود policy

اسکریپت یک کپی موقت از samples/58-architecture-tests می‌سازد.
در Domain یک ProjectReference به Infrastructure اضافه می‌کند.
سپس انتظار دارد policy با AssertionError آن را رد کند.
اگر policy تخلف را بپذیرد، خود تست شکست می‌خورد.
سورس اصلی در این آزمایش تغییر نمی‌کند.
آزمایش‌های منفی compiler درس ۵۱ هم جداگانه در architecture_smoke باقی مانده‌اند.

## ADR چیست؟

Architecture Decision Record یک تصمیم مشخص با دلیل و پیامدش است.
فهرست buzzword یا توصیف پوشه‌ها کافی نیست.
خواننده باید بفهمد چه مسئله‌ای داشتیم، چه انتخاب دیگری ممکن بود و چرا انتخاب فعلی انجام شد.
تصمیم تغییرپذیر است؛ تاریخچه را با پاک کردن دلیل قبلی از بین نبر.
اگر تصمیم جایگزین شد، وضعیت آن را superseded کن و به ADR تازه لینک بده.

## ADRهای واقعی نمونه

فایل‌ها نسبت به samples/58-architecture-tests هستند:

| فایل | تصمیم |
| --- | --- |
| docs/adr/001-boundaries.md | جهت وابستگی و مرز assembly |
| docs/adr/002-command-query.md | دو port روی همان storage |
| docs/adr/003-validation.md | validation صریح و هزینهٔ اجرای دوباره |
| docs/adr/004-delivery-storage.md | محدودیت storage آزمایشی و تحویل بعدی |

ADR چهارم تصمیم مسیر تحویل بعدی را ثبت می‌کند؛ این snapshot هنوز configuration فایل host درس ۶۰ را ندارد.
هر فایل وضعیت، زمینه، تصمیم، جایگزین‌ها، پیامد و شاهد یا شرط بازنگری دارد.
اسکریپت فقط وجود بخش‌ها را کنترل می‌کند؛ کیفیت استدلال همچنان به review انسانی نیاز دارد.

## اجرا

```powershell
dotnet run --project samples/58-architecture-tests/SupportDesk.csproj -- --check
python scripts/architecture_final_smoke.py
```

--check عبارت `PASS: compiled assembly dependency boundaries` می‌دهد.
اسکریپت جملهٔ `PASS: declared project/package policy, ADR completeness and deliberate violation rejection` را چاپ می‌کند.
SDK 10 و restore package ثابت لازم است.
بدون SDK فقط بررسی metadata ممکن است؛ موفقیت آن را با موفقیت build یکی اعلام نکن.

## دام policy سخت یا سست

اگر همهٔ referenceها را مجاز کنی، تست چیزی را محدود نمی‌کند.
اگر هر dependency را ممنوع کنی، حتی adapter درست هم ممکن نیست.
اگر policy را به layout ریز فایل‌ها بیش از حد گره بزنی، هر refactor بی‌خطر تست را می‌شکند.
قانون را به نیاز وصل کن: «Domain بدون host و provider قابل بررسی باشد» دلیل روشنی دارد.
تغییر package یا assembly باید با تصمیم و شواهد بازنگری شود، نه با حذف assertion ناموفق.

## تمرین مستقل

برای نیاز فرضی ارسال اعلان پس از resolve یک ADR بنویس.
هنوز worker یا صف نساز؛ مسئلهٔ commit داده و موفقیت ارسال را جدا شرح بده.
جای port، adapter و مالک orchestration را تعیین کن.
حداقل دو جایگزین و یک حالت failure داشته باش.
سپس یک policy کوچک بنویس که Domain به implementation اعلان وابسته نشود.

### راهنمای مرحله‌ای

1. trigger و نتیجهٔ قابل مشاهده را مشخص کن.
2. قبل از انتخاب ابزار، failure پس از commit را توضیح بده.
3. تصمیم و فرض‌ها را جدا بنویس.
4. یک تخلف عمدی برای تست خود policy بساز.

## معیار عبور

- graph مستقیم پروژه و packageها با قرارداد برابر باشد.
- یک reference نامجاز در کپی موقت رد شود.
- بررسی assembly پس از publish هم اجرا شود.
- adapter داخلی از exported types بیرون نرود.
- ADR هزینه و جایگزین داشته باشد، نه فقط نام معماری.
- محدودیت policy و نیاز به review را توضیح بدهی.

## منابع رسمی

بررسی در 2026-10-17:

- [GetReferencedAssemblies](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getreferencedassemblies?view=net-10.0)
- [راهنمای Architecture Decision Record](https://learn.microsoft.com/en-us/azure/well-architected/architect-role/architecture-decision-record)
- [مرزهای Application و Infrastructure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

[قبلی](../058-validation-library/README.md) · [بعدی](../060-teamdesk/README.md)
