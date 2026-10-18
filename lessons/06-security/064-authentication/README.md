# درس 064 — cookie و bearer و انتخاب روش

## خروجی قابل مشاهده

یک حساب PostgreSQL با cookie یا protected bearer خود Identity وارد می‌شود.
مرز scheme، CSRF، refresh، logout و key ring پایدار با HTTP واقعی بررسی می‌شود.
credential دست‌ساز، X-User و X-Role هیچ هویتی ایجاد نمی‌کنند.

پیش‌نیاز: [درس 063](../063-passwords/README.md)، Identity، middleware و مدل تهدید.
این snapshot آزمایش authentication است؛ هنوز سازمان، نقش، مالکیت تیکت و حساب self-service کامل ندارد.
هیچ مسیر تیکت ناامنِ نسخهٔ ۶۰ به‌صورت خودکار در آن باز نشده است.

## انتخاب بر اساس client

| روش | رفتار client | تصمیم‌های مهم |
| --- | --- | --- |
| Cookie | مرورگر معمولاً خودکار می‌فرستد | HttpOnly، Secure، SameSite و CSRF |
| Bearer header | client صریحاً credential را می‌فرستد | نگهداری امن، عمر، دامنهٔ مصرف و refresh |
| OAuth/OIDC | قرارداد استاندارد با issuer مشخص | audience، issuer، policy و provider واقعی |

Bearer شکل حمل credential است؛ الزاماً JWT نیست.
HttpOnly دسترسی مستقیم JavaScript به cookie را محدود می‌کند، ولی XSS را به‌طور کامل خنثی نمی‌کند.
SameSite هم جای طراحی کامل CSRF نیست.
نگهداری token در localStorage راه‌حل پیش‌فرضِ بی‌خطر برای مرورگر نیست.

## token این نمونه چیست؟

از AddIdentityApiEndpoints برای ثبت schemeهای framework استفاده می‌کنیم، ولی routeهای محدود آزمایش را صریح تعریف می‌کنیم.
SignInManager با IdentityConstants.BearerScheme، handler رسمی را برای صدور protected token فعال می‌کند.
این token، JWT استاندارد و سرویس کامل OAuth/OIDC نیست.
برای API بین چند سرویس با issuer و audience استاندارد، provider مناسب و validation استاندارد لازم است؛ تولید JWT دست‌ساز جواب این مسئله نیست.

## نقشهٔ فایل‌ها

| فایل | نقش |
| --- | --- |
| Program.cs | DI، schemeها، cookie و transport و ترتیب middleware |
| Endpoints/AuthenticationEndpoints.cs | login، csrf، refresh، logout و routeهای scheme مشخص |
| Accounts/AccountServices.cs | policy حساب و store |
| Persistence/AccountContext.cs | جداول Identity در PostgreSQL |
| Hosting/LabProvisioning.cs | فقط فرمان محلی ساخت آزمایشگاه تازه |

Authentication قبل از Authorization اجرا می‌شود.
GET /api/me فقط userId سروری می‌دهد و IdentityUser کامل serialize نمی‌شود.
/api/cookie-action و /api/bearer-action فقط شاهد scheme هستند و هنوز منطق تغییر تیکت ندارند.
نقش و کنترل مالکیت در درس‌های بعد به use case و منبع وصل می‌شوند.

## قرارداد مسیرها

| مسیر | ورودی یا انتظار |
| --- | --- |
| GET /auth/csrf | ساخت token ضد CSRF و cookie مربوط؛ no-store |
| POST /auth/login?mode=cookie | email/password و X-CSRF-TOKEN معتبر |
| POST /auth/login یا mode=bearer | email/password صریح و صدور protected token |
| POST /auth/refresh | refreshToken در body؛ اعتبار purpose، expiry و stamp |
| POST /auth/logout | cookie معتبر و CSRF معتبر؛ 204 |
| GET /api/me | cookie یا bearer معتبر؛ 200 و userId |
| POST /api/cookie-action | فقط scheme cookie و CSRF معتبر |
| POST /api/bearer-action | فقط scheme bearer |

ورودی credential خراب، حساب ناموجود، رمز نادرست و ورود نامجاز پاسخ عمومی 401 دارند.
برابری shape پاسخ به معنی تضمین یکسان بودن زمان پاسخ یا حل کامل user enumeration نیست.
خطاهای فنی پاسخ 500 عمومی دارند.

## CSRF از همین مرحله

برای ورود cookie نیز login CSRF را در نظر می‌گیریم؛ ابتدا GET /auth/csrf بزن.
پس از ورود، token جدید بگیر چون token قبلی به وضعیت anonymous وابسته بود.
برای cookie-action و logout، token را در X-CSRF-TOKEN بفرست.
مسیر bearer-only به cookie برای ورود تکیه نمی‌کند و ambient cookie آن را مجاز نمی‌کند.
فصل ۶۷ CSRF، CORS و injection را عمیق‌تر ادامه می‌دهد؛ این نمونه تا آن زمان مسیر cookie باز و بی‌محافظ نمی‌سازد.

## عمر و ابطال

cookie بیست دقیقه و بدون sliding expiration است؛ در این login غیرpersistent صادر می‌شود.
بررسی security stamp cookie هر یک دقیقه تنظیم شده است.
access token پنج دقیقه و refresh token بیست دقیقه اعتبار دارند.
reset-token پانزده‌دقیقه‌ای درس قبل قرارداد دیگری است.

Refresh با protector مربوط و expiry و ValidateSecurityStampAsync بررسی می‌شود.
هر refresh یک pair تازه می‌دهد، ولی پیاده‌سازی فعلی ledger مصرف یا تشخیص reuse ندارد؛ refresh قبلی ممکن است تا انقضا و تغییر stamp دوباره کار کند.
Logout این route، cookie را از client پاک می‌کند و access token قبلی را فوراً باطل نمی‌کند.
همچنین کپی دزدیده‌شدهٔ cookie الزاماً با حذف cookie مرورگر خودبه‌خود باطل نمی‌شود.
برای revocation فوری و session management و replay detection به قابلیت جدا و تست آن نیاز داریم.

## کلیدهای پایدار

TEAMDESK_KEYS یک پوشهٔ خصوصیِ متعلق به host است.
Data Protection با SetApplicationName ثابت، token و cookie را در restart همان برنامه قابل خواندن نگه می‌دارد.
تغییر app name یا از دست دادن key ring می‌تواند session و tokenها را غیرقابل استفاده کند.
این فایل‌های lab برای نمایش persistence هستند؛ حفاظت at-rest، ACL، backup و encryption کلید باید در محیط واقعی جدا طراحی شوند.
کلید، password و token را در repo، URL عیب‌یابی یا log نگذار.

## راه‌اندازی آزمایشگاه محلی

این فرمان از schema تازه با نام محدود استفاده می‌کند و جدول موجود را جایگزین نمی‌کند.
PostgreSQL اختصاصی و پوشهٔ key تازه لازم است:

```powershell
$schema = 'auth64_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-auth-keys"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$env:TEAMDESK_LAB_EMAIL = 'learner@example.test'
$secret = Read-Host 'Temporary lab password (12+ characters and default Identity complexity)' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/63-cookie-bearer/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
Remove-Item Env:TEAMDESK_LAB_EMAIL
dotnet run --project samples/63-cookie-bearer/SupportDesk.csproj -- --urls http://127.0.0.1:5064
```

password موقت را از مدیر رمز/حافظهٔ خودت برای آزمایش client بگیر؛ آن را در تاریخچهٔ فرمان وارد نکن.
--init-lab فقط با Development و flag صریح اجرا می‌شود و تأیید حساب را در همان فرمان مورد اعتماد انجام می‌دهد؛ endpoint عمومیِ تأیید خودکار نیست.
این فرمان ایمیل واقعی نمی‌فرستد و برای ثبت‌نام production طراحی نشده است.
ابتدا schema بساز و بعد host عادی را اجرا کن؛ startup معمول DDL ندارد.
برای migration محصول از مسیر بخش چهارم استفاده کن؛ این lab، migration تولیدی ندارد.

## transport

بدون flag آزمایش، cookie به Secure=Always تنظیم شده و plain HTTP توسط middleware رد می‌شود.
flag HTTP فقط در Development و برای remote IP از loopback مجاز است؛ برای نمونه‌های موقت بدون credential واقعی استفاده کن.
در Production، فعال کردن flag باعث شکست startup می‌شود.
پشت reverse proxy، HTTPS و forwarded headers فقط برای proxy مورد اعتماد باید پیکربندی شوند؛ این نمونه proxy production پیکربندی نمی‌کند.
برای کاربرد واقعی، HTTPS قابل اعتماد و حفاظت کلید و سرویس حساب کامل لازم است.

## اجرای بررسی

```powershell
python scripts/security_smoke.py
```

SDK 10، PostgreSQL و psql لازم‌اند؛ راهنمای env آزمایش در فهرست بخش آمده است.
اسکریپت حساب و schema و key خصوصیِ موقت خودش را می‌سازد و password/token چاپ نمی‌کند.
موارد بررسی: credential جعلی، رمز نادرست، token خراب، refresh، CSRF قبل و بعد از login، logout، مرز scheme و باقی ماندن حساب و key در restart.
انقضای پنج‌دقیقه‌ای با انتظار واقعی و مرورگرهای مختلف به‌صورت دستی آزمایش نشده‌اند؛ آن را با تست time provider مستقل تکمیل کن.
خروجی publish هم از پوشه‌ای جدا اجرا می‌شود.

## تمرین مستقل

برای سه client یک ADR انتخاب روش ورود بنویس: مرورگر هم‌origin، ابزار CLI و API خارجی.
برای هرکدام محل نگهداری credential، عمر، CSRF، refresh، logout و revocation را مشخص کن.
یک تست منفی اضافه کن که credential معتبرِ scheme دیگر به مسیر اختصاصی دسترسی نگیرد.
JWT دست‌ساز یا header role را به‌عنوان میان‌بر اضافه نکن.

### سرنخ

اول مشخص کن browser چه چیزی را خودکار می‌فرستد.
سپس تفاوت پاک کردن credential client و باطل کردن server-side آن را روی کاغذ بنویس.

## معیار عبور

- cookie و bearer معتبر از framework صادر و credential جعلی رد شوند.
- cookie-only و bearer-only اشتباه همدیگر را نپذیرند.
- login و تغییر cookie بدون CSRF رد شوند.
- restart با key و DB ثابت، اعتبار session را بی‌دلیل از بین نبرد.
- محدودیت refresh reuse و logout و stamp روشن باشد.
- تفاوت این protected token با JWT و OAuth/OIDC را توضیح بدهی.

## منابع رسمی

تطبیق در 2026-10-18:

- [Identity برای API و protected token](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
- [کد رسمی Identity API در نسخهٔ مرجع](https://github.com/dotnet/aspnetcore/blob/v10.0.13/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs)
- [Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)
- [CSRF](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

[قبلی](../063-passwords/README.md) · [فهرست بخش](../README.md)
درس‌های ۶۵ تا ۷۰ ادامهٔ بخش‌اند و هنوز نوشته نشده‌اند.
