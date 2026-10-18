# درس 074 — تست دیتابیس و Testcontainers

## خروجی قابل مشاهده

همان port تیکت را با PostgreSQL واقعی می‌سنجی: دوام داده، constraint، rollback و رد writer قدیمی.
دو writer واقعی با یک version رقابت می‌کنند؛ دقیقاً یک Save موفق و دیگری Conflict می‌شود.
پیش‌نیاز: [درس ۷۳](../073-integration/README.md)، SQL پارامتری و transaction و بخش data.

## چرا double کافی نیست؟

RecordingStore می‌تواند نتیجهٔ Conflict بدهد ولی ثابت نمی‌کند SQL adapter با UPDATE شرطی از lost update جلوگیری می‌کند.
حافظه یا SQLite هم رفتار constraint، SQL، encoding و connectionهای PostgreSQL را کامل بازنمایی نمی‌کنند.
در این درس provider واقعی و یک instance موقت PostgreSQL اجرا می‌شود؛ endpoint و Application از همان قرارداد ITicketStore استفاده می‌کنند.

## پیش‌نیاز محیط

Docker CLI و engine قابل دسترسی با Linux containers لازم است. روی Windows، Docker Desktop را با Linux containers اجرا کن.
نصب Docker در این درس خودکار انجام نمی‌شود؛ ابتدا docker info باید موفق باشد.
تصویر postgres:18.6 بار اول دانلود می‌شود و دسترسی به registry لازم است؛ زمان download را با زمان اجرای assertionها یکی نگیر.
Testcontainers.PostgreSql نسخهٔ ثابت 4.15.0 فقط در پروژهٔ تست است. driver Npgsql 10.0.3 در sample ثبت شده است.
این شماره‌ها نسخهٔ مرجع بررسی‌شده در 2026-10-18 هستند؛ image tag معادل pin کردن digest غیرقابل تغییر نیست.

```powershell
docker info
dotnet build samples/73-database-testing/SupportDesk.csproj
dotnet test tests/074-database-testing/SupportDesk.Tests.csproj
python scripts/testing_smoke.py --with-docker
```

برای اجرای فقط suite کانتینر، testing_smoke.py --docker-only را بزن.
حالت بدون flag، ۷۱ تا ۷۳ را اجرا و ۷۴ را فقط build می‌کند و صریح NOT RUN چاپ می‌کند؛ آن را تأیید رفتار PostgreSQL حساب نکن.
اگر Docker در حالت کامل در دسترس نباشد، runner شکست می‌خورد؛ skip خودکار و نتیجهٔ سبز جایگزین نمی‌شود.

## مالکیت lifecycle و داده

PostgresFixture با IAsyncLifetime مربوط به xUnit v2 container را StartAsync و در پایان DisposeAsync می‌کند.
یک container در سطح کلاس fixture مشترک است، ولی هر تست شناسه‌های تازه و دادهٔ خودش را دارد؛ reset سراسری بین تست‌ها نداریم.
نام database داخل container course_testing است، password تصادفی و پورت host به‌صورت پویا انتخاب می‌شود.
GetConnectionString آدرس mapped واقعی را می‌دهد؛ فرض localhost:5432 یا پورت ثابت نداریم.
Search Path با prefix testing74_ و ۳۲ hex تازه تنظیم می‌شود. init فقط schema تازه را می‌سازد و DDL destructive ندارد.
Resource Reaper ابزار و dispose عادی به cleanup کمک می‌کنند؛ reuse container را فعال نکرده‌ایم.
آزمایش‌های runtime environment و خطاهای Docker باید با logهای محلی بررسی شوند، نه با ارسال connection string و password به دیگران.

## نقشهٔ فایل‌ها

| فایل | مسئولیت |
| --- | --- |
| samples/73-database-testing/Infrastructure/PostgresTicketStore.cs | port واقعی با Npgsql و UPDATE مبتنی بر version و owner |
| samples/73-database-testing/Persistence/schema.sql | schema تازه و constraintهای SQL |
| tests/074-database-testing/PostgresTests.cs | fixture، تست adapter و transaction و HTTP واقعی با DB |
| tests/074-database-testing/TestIdentity.cs | فقط authentication قابل کنترل در test host |

این schema.sql یک fixture bootstrap برای database تازه است، نه زنجیرهٔ migration محصول.
تست migrationهای بخش چهارم و دادهٔ موجود باید جدا بماند؛ ساخت موفق schema خالی، ارتقای schema واقعی را ثابت نمی‌کند.
EnsureDeleted و حذف database خارجی نداریم. تست‌ها SUPPORTDESK_PG محیط کاربر را برای اتصال خود استفاده نمی‌کنند.

## چه چیزهایی واقعاً سنجیده می‌شوند؟

| سناریو | شاهد |
| --- | --- |
| Create و Start و Resolve | roundtrip با adapter جدید و version افزایشی |
| دو Save با یک snapshot | یک Saved و یک Conflict از SQL واقعی |
| insert معتبر، سپس CHECK نامعتبر در transaction | خطای CheckViolation و نبود insert اول بعد از rollback |
| title شبیه SQL | متن literal بدون تبدیل شدن به دستور |
| OwnerId نامعتبر در snapshot | Save ناموفق و حفظ Open |
| id غایب و token از ابتدا cancelled | نبود منبع و exception لغو، بدون موفقیت کاذب |
| dispose و ساخت دوبارهٔ WebApplicationFactory | دادهٔ PostgreSQL باقی بماند و bob منبع alice را نبیند |

PostgresTicketStore تمام مقدارها را parameter می‌کند. identifier schema فقط پس از regex محدود وارد DDL می‌شود؛ نام schema، input HTTP نیست.
UPDATE شرط Id و OwnerId و Version دارد و version را در همان statement افزایش می‌دهد.
تعداد ردیف تغییرکرده شاهد commit است؛ Load قدیمی به‌تنهایی مجوز overwrite ایجاد نمی‌کند.
اگر UPDATE هیچ ردیفی نداشت، lookup وضعیت Missing/Conflict را دسته‌بندی می‌کند؛ این lookup دوم یک snapshot تراکنشی واحد با UPDATE نیست.
در API این lab، حذف و انتقال مالکیت نداریم؛ اگر اضافه شوند قرارداد race آن‌ها را نیز تست کن.

## Unicode و constraint

char_length در PostgreSQL و EnumerateRunes در Domain برای Unicode معتبر scalar را می‌شمارند.
Title بین ۳ و ۱۲۰ و Resolution بین ۳ و ۲۰۰ scalar است؛ Close بدون resolution معتبر در DB پذیرفته نمی‌شود.
هم‌خوان بودن واحد طول از اختلاف emoji بین string.Length و SQL جلوگیری می‌کند.
این کنترل جای تست normalization و grapheme و تمام ورودی‌های Unicode در محصول را نمی‌گیرد.

## HTTP با دیتابیس واقعی و هویت تست

DatabaseFactory در ConfigureTestServices، NpgsqlDataSource و ITicketStore را به connection کانتینر fixture وصل می‌کند.
data source ساخته‌شده توسط factory متعلق به DI است و با host dispose می‌شود؛ container تا پایان fixture باقی می‌ماند.
بستن host و باز کردن host جدید، دوام داده را در همان database می‌سنجد؛ restart خود PostgreSQL یا crash-recovery تست نمی‌شود.
HTTP هنوز TestServer است و actor در test assembly ساخته می‌شود؛ این suite ورود Identity یا TLS و proxy production را تأیید نمی‌کند.

## اجرای دستی خارج از تست

host عادی ClosedGate دارد؛ به X-Test-Actor اعتماد نمی‌کند و provider ورود محصول در آن ثبت نشده است.
برای تمرین init روی database اختصاصی، SUPPORTDESK_PG با Search Path=testing74_<32 hex> بده و --init-lab را اجرا کن.
این فرمان schema و جدول تازه می‌سازد و روی schema موجود fail می‌کند؛ reset نمی‌کند.
برای یادگیری حساب و key و session از نمونه‌های امنیت استفاده کن؛ هویت تست را برای قابل استفاده کردن UI به app منتقل نکن.

## گزارش و CI

runner، TRX را می‌خواند و تعداد executed و failed و skipped را کنترل می‌کند؛ فقط exit code صفر یا build کافی نیست.
در CI، unit و WAF روی Windows/Linux و container suite در job مستقل Linux اجرا می‌شوند.
این تست به سرویس PostgreSQL مشترک jobهای قبلی متصل نمی‌شود؛ lifecycle و connection کانتینر خودش را دارد.
backup/restore، چند replica، outage شبکه و تست load در این درس اجرا نمی‌شوند.

## تمرین مستقل

برای تیکت بسته‌شده یک constraint تازه و تست رد آن بنویس و پیام کاربردی را از exception driver جدا نگه دار.
یک تست ارتقای migration روی دادهٔ قبلی اضافه کن؛ ساخت schema خالی را با ارتقا یکی ندان.
تست race را با دو adapter جدا و connectionهای واقعی بنویس و حفظ نسخه را با GET یا query مستقل بررسی کن.

### سرنخ

هر تست فقط دادهٔ خودش را کنترل کند؛ برای تمیز شدن تست، دیتابیس واقعیِ کاربر را حذف نکن.

## معیار عبور

- Docker واقعاً اجرا شود و نبود آن در حالت کامل fail شود.
- پورت و credential از fixture بیایند و connection چاپ نشود.
- race و constraint و rollback و roundtrip از PostgreSQL واقعی شاهد داشته باشند.
- host جدید همان داده را بخواند و actor دیگر به آن دسترسی نگیرد.
- مرز bootstrap، migration، restart host و crash-recovery را توضیح بدهی.

## منابع رسمی

- [Testcontainers PostgreSQL module](https://dotnet.testcontainers.org/modules/postgres/)
- [Lifecycle و xUnit](https://dotnet.testcontainers.org/test_frameworks/xunit_net/)
- [نسخهٔ مرجع Testcontainers.PostgreSql](https://www.nuget.org/packages/Testcontainers.PostgreSql/4.15.0)
- [Integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

[قبلی](../073-integration/README.md) · [فهرست بخش](../README.md)
درس‌های ۷۵ تا ۷۸ هنوز نوشته نشده‌اند.
