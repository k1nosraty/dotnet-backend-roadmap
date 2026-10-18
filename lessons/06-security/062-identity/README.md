# درس 062 — Identity و مدیریت امن حساب

## خروجی قابل مشاهده

یک حساب واقعی با ASP.NET Core Identity و EF Core در PostgreSQL ساخته می‌شود.
نام نرمال‌شدهٔ تکراری و رمز نامعتبر رد می‌شوند، تأیید ایمیل بررسی می‌شود و تلاش‌های ناموفق به lockout می‌رسند.
هیچ جدول یا hash دست‌ساز برای credential ساخته نمی‌شود.

پیش‌نیاز: [درس 061](../061-threat-model/README.md)، EF Core، scope و PostgreSQL بخش چهارم.
این نمونه کنسولی است و HTTP login ندارد؛ بررسی چرخهٔ حساب را از حمل‌ونقل credential جدا می‌کند.

## Identity چه چیزی را برعهده می‌گیرد؟

UserManager مسئول عملیات حساب و policy و store است.
SignInManager قواعد ورود مانند تأیید ایمیل، lockout و صدور session را هماهنگ می‌کند.
IdentityDbContext شکل ذخیرهٔ حساب، نقش، claim و tokenهای مربوط را به EF معرفی می‌کند.
وجود جدول نقش به معنی تعریف مجوز business در برنامه نیست.
مدل Ticket همچنان مستقل از IdentityUser باقی می‌ماند.

## فایل‌ها و مالکیت

| فایل نسبت به نمونه | نقش |
| --- | --- |
| Accounts/AccountServices.cs | ثبت سرویس‌ها و policy نمونه |
| Persistence/AccountContext.cs | IdentityDbContext و mapping استاندارد |
| Accounts/AccountChecks.cs | چرخهٔ حساب با UserManager و SignInManager |
| Infrastructure/Lab.cs | مالک schema موقت و پاک‌سازی |
| Program.cs | composition و scope و اجرای آزمایش |

در OnModelCreating باید base.OnModelCreating اجرا شود تا کلیدها و indexها حفظ شوند.
UserManager و context scoped هستند؛ آن‌ها را در singleton نگه ندار.
برای تست واقعی storage، از EF InMemory به‌عنوان جایگزین PostgreSQL استفاده نمی‌کنیم.

## نسخه‌های ثابت

مبنای این گروه net10.0 است:

| package | نسخه |
| --- | --- |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.13 |
| Microsoft.EntityFrameworkCore | 10.0.13 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.13 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |

این‌ها نسخهٔ مرجع هستند، نه ادعای آخرین patch آینده.
شمارهٔ patch provider مستقل از Microsoft است.
ثبت packageها در csproj انجام شده؛ اولین restore به NuGet نیاز دارد.

## policy نمونه

```csharp
options.User.RequireUniqueEmail = true;
options.SignIn.RequireConfirmedEmail = true;
options.Password.RequiredLength = 12;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
```

قواعد پیچیدگی پیش‌فرض Identity در این نمونه حفظ شده‌اند؛ ۱۲ حداقل طول انتخابی آزمایش است.
این اعداد نسخهٔ همیشگی برای هر محصول نیستند؛ usability، MFA، تهدید و rate limit باید با هم تصمیم‌گیری شوند.
lockout حساب به‌تنهایی جلوی password spraying را نمی‌گیرد و می‌تواند هدف denial of service شود.
بودجهٔ درخواست و کنترل سوءاستفاده در درس ۶۸ اضافه می‌شوند.

## ساخت و normalization

UserName و Email در این آزمایش هر دو همان آدرس ایمیل‌اند.
CreateAsync نتیجهٔ IdentityResult می‌دهد؛ شکست را نادیده نگیر.
نمونه همان username را با حروف بزرگ دوباره امتحان می‌کند؛ normalization و index نام کاربر از تکرار جلوگیری می‌کنند.
برای policy متفاوتِ username و email، uniqueness و race دیتابیس را جدا طراحی کن.
RequireUniqueEmail را بدون بررسی index و شکل داده، معادل هر تضمین هم‌زمانی دلخواه فرض نکن.

## تأیید حساب

قبل از تأیید ایمیل، CanSignInAsync برابر false است.
GenerateEmailConfirmationTokenAsync و ConfirmEmailAsync نمونهٔ تغییر واقعی state حساب را نشان می‌دهند.
در این lab، token داخل همان process به متد تأیید داده می‌شود؛ ایمیل واقعی فرستاده نمی‌شود و token چاپ نمی‌شود.
این bypass عمومی برای تأیید حساب نیست؛ هیچ endpoint نمایش لینک یا تأیید خودکار وجود ندارد.
در محصول باید مالکیت آدرس با کانال واقعیِ خارج از درخواست ثبت‌نام اثبات شود.

## password check با sign-in یکسان نیست

CheckPasswordAsync فقط بخشی از بررسی credential است.
CheckPasswordSignInAsync policyهای ورود را هم بررسی می‌کند، اما در آزمایش کنسولی session صادر نمی‌کنیم.
پس از پنج failure، حساب قفل می‌شود و حتی رمز درست تا پایان lockout آن را دور نمی‌زند.
userId و PasswordHash را در response عمومی serialize نکن؛ hash هم دادهٔ حساس است.

## اجرای مرجع

[راهنمای PostgreSQL](../../../handbook/postgresql-lab.md) را انجام بده و SUPPORTDESK_PG را به دیتابیس اختصاصی آزمایش بده:

```powershell
dotnet run --project samples/61-identity/SupportDesk.csproj
dotnet run --project samples/61-identity/SupportDesk.csproj -- --keep
```

هر اجرا schema تصادفی auth62 می‌سازد و در پایان فقط همان را پاک می‌کند.
--keep برای بازرسی دستی است؛ schema باقی‌مانده را خودت بعداً پاک کن.
خروجی شامل `PASS: Identity account creation, normalization, confirmation and lockout` و `PASS: lesson scenario` است.
password تصادفی و tokenها به خروجی نمی‌روند.

CreateTables در این مثال فقط روی schema تازهٔ تحت مالکیت Lab اجرا می‌شود.
آن را جایگزین migration دیتابیس برنامه نکن؛ مسیر migration بخش چهارم برای محصول باقی است.
Data Protection در این دو آزمایش کنسولی ephemeral است و برای token بین دو process مناسب نیست؛ درس ۶۴ key ring پایدار دارد.

## بررسی و سناریوهای شکست

| سناریو | انتظار |
| --- | --- |
| password کوتاه | IdentityResult ناموفق و عدم درج حساب |
| username نرمال‌شدهٔ تکراری | رد ایجاد |
| ایمیل تأییدنشده | ورود مجاز نشود |
| token تأیید دستکاری‌شده | شکست بدون تأیید حساب |
| token معتبر | EmailConfirmed و امکان ادامهٔ ورود |
| تلاش ناموفق تکراری | lockout |

```powershell
python scripts/security_smoke.py
```

اجرای زنده به PostgreSQL و برای بخش HTTP بعدی به psql نیاز دارد.
--build-only فقط کامپایل و مرز HTTP درس ۶۱ را بررسی می‌کند؛ SQL را اجرا نمی‌کند.

## تمرین مستقل

یک account summary بساز که فقط userId و وضعیت تأیید را برای مصرف داخلی لازم برگرداند.
PasswordHash، SecurityStamp و tokenها نباید در DTO باشند.
سپس شکست CreateAsync را با IdentityResult مدیریت کن و توضیح بده چه جزئیاتی برای کاربر و چه جزئیاتی برای تشخیص داخلی مناسب‌اند.

### سرنخ

IdentityUser را مدل دامنهٔ تیکت نکن.
از UserManager برای تغییر credential استفاده کن؛ setter مستقیم PasswordHash راه‌حل reset نیست.

## معیار عبور

- چرخهٔ حساب روی PostgreSQL واقعی اجرا شود.
- حساب نامعتبر واقعاً در DB درج نشده باشد.
- تأیید و lockout را از صدور cookie جدا توضیح بدهی.
- password، hash و token در log یا response آزمایش منتشر نشوند.
- بتوانی فرق schema موقت، migration محصول و key ring را شرح بدهی.

## منابع رسمی

تطبیق در 2026-10-18:

- [مقدمهٔ ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)
- [تنظیمات Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [Identity EF package مرجع](https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore/10.0.13)

[قبلی](../061-threat-model/README.md) · [بعدی](../063-passwords/README.md)
