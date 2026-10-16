# درس 042 — migration و تکامل schema

پیش‌نیاز: درس 041 و [محیط PostgreSQL](../../../handbook/postgresql-lab.md).
خروجی این درس یک مثال قابل اجرا و یک تمرین مستقل است؛ صرفاً خواندن کد معیار عبور نیست.

مدل C# نسخهٔ امروز با دیتابیس دیروز برابر نیست.
افزودن property فقط برنامه را تغییر می‌دهد؛ جدول موجود تا اجرای migration تغییر نمی‌کند.
در این درس یک دیتابیس نسخهٔ اول با دادهٔ واقعی به نسخهٔ دوم ارتقا پیدا می‌کند.

## سه سند با سه نقش

| فایل/جدول | چه چیزی را نگه می‌دارد؟ |
| --- | --- |
| migration و Up/Down | تغییر قابل review نسبت به نسخهٔ قبلی |
| DeskContextModelSnapshot | مدل مبنای ساخت migration بعدی |
| __EFMigrationsHistory | شناسهٔ migrationهایی که روی همین دیتابیس اعمال شده‌اند |

snapshot را به جای migration اجرا نمی‌کنیم.
EF معمولاً اختلاف مدل امروز و snapshot را scaffold می‌کند؛ کشف همهٔ تفاوت‌های schema زنده کار آن نیست.
HasPendingModelChanges نبود drift بین کد و snapshot را بررسی می‌کند، نه سلامت کامل دیتابیس را.
migration و designer و snapshot را با هم commit کن؛ ویرایش تاریخچهٔ اعمال‌شده روش upgrade نیست.

## دو نسخهٔ مرجع

InitialCreate جدول tickets با Id و Title و Status را می‌سازد.
AddPriority ستون Priority با default برابر Normal اضافه می‌کند.
دادهٔ قبلی حذف نمی‌شود و مقدار آن ستون برای تیکت قدیمی Normal می‌شود.
در Program ابتدا فقط migration اول اجرا می‌شود، یک تیکت با SQL پارامتری‌نبودنِ ثابت آموزشی درج می‌شود، سپس migration دوم اجرا می‌شود.
SQL اینجا ورودی کاربر ندارد؛ در قابلیت واقعی همهٔ مقدارهای بیرونی باید parameter باشند.
context تا پایان upgrade نباید entity نسخهٔ جدید را از جدول قدیمی query کند.

## ابزار محلی مخزن

از ریشهٔ مخزن اجرا کن؛ نسخهٔ ابزار در .config/dotnet-tools.json ثبت شده است.
Microsoft.EntityFrameworkCore.Design فقط برای ابزار پروژه است و PrivateAssets=all دارد.
نسخهٔ EF و Design و ابزار این نسخه 10.0.13 و provider برابر 10.0.3 است.
این‌ها دو بسته با release مستقل‌اند؛ شمارهٔ patch آن‌ها لازم نیست برابر باشد.

```powershell
dotnet tool restore
dotnet ef migrations list --project samples/41-migrations/SupportDesk.csproj
dotnet ef migrations has-pending-model-changes --project samples/41-migrations/SupportDesk.csproj
dotnet ef migrations script --idempotent --project samples/41-migrations/SupportDesk.csproj --output upgrade.sql
```

script را پیش از اجرا بخوان: جدول هدف، نوع ستون، default، lock و احتمال از دست رفتن داده مهم‌اند.
دیتابیس این نمونه موقتی است؛ برای مشاهدهٔ history، اجرای درس را با --keep انجام بده.
SUPPORTDESK_PG را به همان schema نگه‌داشته‌شده وصل کن تا CLI تاریخچهٔ همان آزمایش را ببیند.
بدون Search Path، ابزار ممکن است schema پیش‌فرض دیگری را هدف بگیرد.

## یک تغییر جدید را خودت بساز

در یک کپی از پروژه، property با نام Category و مقدار اولیهٔ General اضافه کن.
mapping آن را IsRequired و HasMaxLength(40) و HasDefaultValue("General") قرار بده.

```powershell
dotnet ef migrations add AddCategory --project samples/41-migrations/SupportDesk.csproj
```

Up باید AddColumn باشد، نه DropTable/CreateTable.
Down معمولاً ستون را حذف می‌کند؛ دادهٔ آن ستون برنمی‌گردد.
این تمرین را در کپی انجام بده: سناریوی مرجع عمداً تعداد دو migration را بررسی می‌کند.
remove فقط برای آخرین migration اعمال‌نشده در روند توسعه است؛ migration مشترک اعمال‌شده را بازنویسی نکن.

## rollout بدون شعار

برای rename، generated code را بخوان؛ drop/add ممکن است داده را نابود کند.
برای ستون اجباری روی جدول بزرگ، الگوی expand → backfill → enforce را ارزیابی کن.
ابتدا سازگاری کد قدیم و جدید را برقرار کن، backfill را قابل کنترل انجام بده، سپس constraint را سخت کن.
یک default آموزشی برای جدول کوچک، تضمین deployment بدون lock روی جدول بزرگ نیست.
اجرای migration در همهٔ instanceهای وب، اختیار و زمان تغییر schema را به startup گره می‌زند.
درس ۴۸ فرمان maintenance جدا دارد؛ production به review script، backup و برنامهٔ بازیابی نیاز دارد.

## مرز rollback

Down معکوس نحوی بعضی DDLهاست؛ تضمین بازیابی کسب‌وکار نیست.
اگر ستون را حذف کنی و داده از دست برود، دوباره Up کردن دادهٔ قبلی را بازسازی نمی‌کند.
برنامهٔ قدیمی ممکن است schema جدید را نفهمد؛ rollback برنامه و rollback دیتابیس یک تصمیم واحد نیستند.
در خرابی، پیش از اجرای دستور اصلاح، migration history و نسخهٔ binary را ثبت کن.

## اجرای مرجع

از ریشهٔ مخزن و پس از تنظیم SUPPORTDESK_PG اجرا کن:

```powershell
dotnet run --project samples/41-migrations/SupportDesk.csproj
```

Lab یک schema تصادفی تازه می‌سازد و در پایان همان schema را پاک می‌کند.
با --keep در انتهای فرمان می‌توانی آن را برای بازرسی نگه داری؛ نام schema در خروجی چاپ می‌شود.
این گزینه ابزار مشاهده است، نه ذخیره‌سازی پایدار برنامه.
کد خروج صفر و PASS: lesson scenario یعنی assertionهای همین سناریو عبور کرده‌اند.
برای بررسی همهٔ این درس‌ها، python scripts/ef_smoke.py را با دیتابیس آماده اجرا کن.

## تمرین و معیار عبور

1. دو فایل Up را کنار هم بخوان و توضیح بده چرا دادهٔ Legacy ticket باقی می‌ماند.
2. upgrade را دو بار روی یک schema اجرا کن؛ history و تعداد ردیف‌ها باید ثابت بماند.
3. Category را در کپی اضافه کن و SQL idempotent را بررسی کن؛ نام constraintها را پیدا کن.
4. تغییر نام Title را عمداً scaffold کن و بررسی کن آیا RenameColumn تولید شده یا حذف/ایجاد.

معیار عبور: هم ساخت تازه و هم upgrade با دادهٔ قدیمی را بررسی کرده باشی.
پاسخ «build شد» برای یک تغییر دیتابیس کافی نیست.

## منبع رسمی

[مستندات مرتبط](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

[Npgsql EF provider](https://www.npgsql.org/efcore/)

[بخش چهارم](../README.md) · [نقشهٔ دوره](../../../ROADMAP.md) · [درس بعد](../043-relationships/README.md)
