# درس 063 — hash رمز و سیاست بازیابی

## خروجی قابل مشاهده

برای یک password ثابت دو hash متفاوت تولید و هر دو را با PasswordHasher رسمی بررسی می‌کنی.
یک hash قدیمی به نتیجهٔ SuccessRehashNeeded می‌رسد.
reset tokenِ کاربر دیگر، token خراب و token مصرف‌شده رد می‌شوند؛ رمز جدید و SecurityStamp واقعی بررسی می‌شوند.

پیش‌نیاز: [درس 062](../062-identity/README.md)، exception، async و نگهداری دادهٔ حساس.
نمونه روی PostgreSQL واقعی اجرا می‌شود و هیچ password یا token را چاپ نمی‌کند.

## hash، رمزنگاری و encoding

Encoding مانند Base64 برگشت‌پذیر و برای پنهان کردن password نیست.
Encryption با کلید برای بازیابی plaintext طراحی می‌شود؛ برای نگهداری رمز ورود معمولاً آن plaintext را لازم نداریم.
Password hashing، همراه salt و هزینهٔ محاسبه، برای verification طراحی شده است.
SHA256(password) یا یک hash سریعِ تنها، جای password hasher مناسب نیست.
از الگوریتم یا salt دست‌ساز برای حساب این دوره استفاده نمی‌کنیم.

## ابزار استاندارد

از IPasswordHasher<IdentityUser> ثبت‌شدهٔ Identity استفاده می‌شود.
فرمت hash اطلاعات لازم برای verification و ارتقا را همراه خود نگه می‌دارد.
به‌جای مقایسهٔ رشتهٔ دو hash، VerifyHashedPassword را اجرا کن.
ساختار داخلی فرمت را در کد application parse نکن.

```csharp
string first = hasher.HashPassword(user, password);
string second = hasher.HashPassword(user, password);
```

دو خروجی برابر نیستند، چون salt مستقل دارند.
همان password می‌تواند هر دو را با موفقیت verify کند.
نمونه فقط مقایسه را گزارش می‌کند، نه خود hashها را.
لو رفتن hash همچنان خطر offline guessing دارد و مجوز انتشار آن نیست.

## ارتقای hash

PasswordVerificationResult سه حالت مهم دارد:

| نتیجه | معنی |
| --- | --- |
| Failed | password پذیرفته نشده |
| Success | معتبر و طبق policy فعلی کافی |
| SuccessRehashNeeded | معتبر، ولی نیازمند بازتولید با policy فعلی |

در تست، یک hasher با IterationCount=1000 عمداً فقط برای ساخت دادهٔ legacy ایجاد شده است.
این تنظیم را هرگز در AccountServices ثبت نکن.
hasher جاری همان Identity پیش‌فرض است و باید دادهٔ legacy را برای ارتقا علامت بزند.
برای policy واقعی، هزینه را با تهدید، ظرفیت و راهنمای جاری بسنج؛ یک عدد در تست، benchmark امنیت همهٔ محیط‌ها نیست.
UserManager مسیر ارتقای credential و ذخیرهٔ آن را مدیریت می‌کند؛ به‌جای دستکاری مستقیم ستون hash از آن استفاده کن.

## reset رمز یک قابلیت حساس است

Forgot password نباید به «ارسال رمز قبلی» تبدیل شود.
کانال بازیابی باید به مقصد تأییدشده برسد، token عمر محدود و purpose مشخص داشته باشد و مصرف آن credential را تغییر بدهد.
نام وجوددار و نام ناموجود نباید از پاسخ عمومیِ درخواست بازیابی قابل تشخیص باشند.
این نمونه endpoint forgot-password ندارد؛ درون process قراردادهای UserManager را آزمایش می‌کند.
بنابراین timing، ارسال ایمیل، لینک واقعی و کنترل سوءاستفادهٔ endpoint را تأیید نمی‌کند.

## جریان مرجع

1. حساب آزمایشی ساخته می‌شود.
2. GeneratePasswordResetTokenAsync یک token مخصوص همان کاربر می‌سازد.
3. همان token برای کاربر دوم امتحان و رد می‌شود.
4. token خراب برای کاربر اصلی رد می‌شود.
5. ResetPasswordAsync با token درست رمز را تغییر می‌دهد.
6. رمز قدیمی دیگر کار نمی‌کند و رمز تازه درست است.
7. SecurityStamp تغییر می‌کند و استفادهٔ دوباره از token قبلی رد می‌شود.

TokenLifespan در AccountServices پانزده دقیقه تنظیم شده است.
این زمان برای token provider مربوط به تأیید/بازیابی است و با عمر cookie یا access token یکی نیست.
تست حاضر منتظر گذشت پانزده دقیقه نمی‌ماند؛ انقضای زمانی را به‌عنوان سناریوی اجراشده اعلام نمی‌کند.
برای تست قطعی زمان، طراحی clock قابل کنترل و provider مرتبط را در فصل تست بررسی کن.

## SecurityStamp چه کار می‌کند؟

پس از reset، tokenهای وابسته به stamp قبلی دیگر برای همان عملیات معتبر نیستند.
نمونه token تازه‌ای هم می‌سازد، stamp را عوض می‌کند و رد شدن token معلق را بررسی می‌کند.
این به معنی ابطال فوری همهٔ credentialها در هر scheme نیست.
cookie stamp validation دوره‌ای است و access token محلی معمولاً تا پایان عمر خودش باقی می‌ماند؛ تفاوت در درس ۶۴ می‌آید.
برای «خروج همهٔ دستگاه‌ها» قرارداد و شاهد مستقل لازم است.

## مدل تهدید لینک بازیابی

| خطا | تصمیم لازم |
| --- | --- |
| نمایش token در response یا log | انتقال فقط از کانال بازیابی مورد اعتماد |
| ساخت URL با Host دلخواه request | origin مورد اعتمادِ تنظیم‌شده و محدود کردن host |
| ایمیل تأییدنشده | سیاست تأیید و بازیابی روشن |
| حدس یا تکرار token | token استاندارد، عمر محدود، کنترل سوءاستفاده |
| ذخیرهٔ password جدید در log | حذف کامل دادهٔ credential از تشخیص عادی |

Base64Url برای حمل امن‌تر token در URL است و رمزنگاری یا تضمین اعتبار آن نیست.
Data Protection و purpose و stamp، بخش‌های متفاوت قرارداد هستند.
اگر provider ایمیل آماده نیست، در production لینک تأیید یا reset را در صفحه یا console نمایش نده.

## اجرا و فایل‌ها

```powershell
dotnet run --project samples/62-password-recovery/SupportDesk.csproj
python scripts/security_smoke.py
```

SUPPORTDESK_PG باید به آزمایشگاه اختصاصی اشاره کند.
Lab schema تازهٔ auth63 می‌سازد و در پایان همان را پاک می‌کند؛ --keep برای بازرسی است.
Accounts/PasswordChecks.cs سناریوهای hash و reset را دارد.
Accounts/AccountChecks.cs بررسی‌های حساب درس قبل را هم اجرا می‌کند.
عبارت `PASS: salted password hashes, rehash, reset binding, replay and security stamp` نشانهٔ عبور این سناریوهاست.
کلیدهای این lab کنسولی ephemeral هستند؛ token ساخته‌شده برای جابه‌جایی میان دو process طراحی نشده است.

## تمرین مستقل

یک قرارداد ForgotPassword تعریف کن که برای ایمیل وجوددار و ناموجود پاسخ عمومی یکسان داشته باشد.
ارسال لینک را پشت یک port قرار بده و با test doubleِ فقط داخل تست بررسی کن که مقصد درست انتخاب می‌شود.
هیچ endpoint برای خواندن outbox یا چاپ token به host عمومی اضافه نکن.
سپس reset token کاربر دیگر و مصرف دوباره را در تست منفی قرار بده.

### سرنخ

ابتدا شکل پاسخ، مقصد مورد اعتماد و اطلاعات ممنوع در log را بنویس.
بعد contract ایمیل و UserManager را هماهنگ کن؛ ابزار SMTP یا سرویس بیرونی لازمهٔ فهم همین invariantها نیست.

## معیار عبور

- hashهای مستقلِ یک password متفاوت و verification معتبر باشند.
- حالت نیاز به rehash از failure جدا باشد.
- reset به کاربر و purpose درست وابسته بماند.
- token خراب، کاربر دیگر و replay رد شوند.
- رمز قدیمی بعد از reset پذیرفته نشود.
- محدودیت تست زمان و کانال ایمیل را صریح توضیح بدهی.

## منابع رسمی

تطبیق در 2026-10-18:

- [تأیید حساب و بازیابی رمز](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0)
- [PasswordHasher](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0)
- [تنظیمات Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)

[قبلی](../062-identity/README.md) · [بعدی](../064-authentication/README.md)
