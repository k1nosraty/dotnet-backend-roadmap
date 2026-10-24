# درس 061 — مدل تهدید و مرز اعتماد

## خروجی قابل مشاهده

قبل از افزودن حساب کاربری، دارایی‌ها، actorها و مرزهای اعتماد TeamDesk را مشخص می‌کنی.
نمونهٔ HTTP در حالت پیش‌فرض بسته است: header جعلی یا متن دلخواه bearer آن را باز نمی‌کند.
هر تهدید یک کنترل، یک شاهد و یک محدودیت معلوم دارد.

پیش‌نیاز: [تحویل معماری درس 060](../../05-architecture/060-teamdesk/README.md)، HTTP و DI.
این snapshot کوچک فقط مرز ورود را نشان می‌دهد؛ هنوز login واقعی ندارد.
نمونهٔ قبلی یک workspace بدون حساب بود؛ اسم TeamDesk به‌تنهایی مجوز یا جداسازی سازمان ایجاد نمی‌کند.

## مسئلهٔ قبل از کد

«ورود داشته باشیم» نیازمندی امنیتی کافی نیست.
باید بدانیم چه داده‌ای را از چه کسی، در چه مسیر و با چه ضمانتی محافظت می‌کنیم.
تهدید فقط مهاجم بیرونی نیست: کاربر عضوِ تیم دیگر، client خراب، credential لو‌رفته و تنظیم اشتباه هم مهم‌اند.
اعتماد به header سمت client به‌جای هویت معتبر، یک مرز را حذف می‌کند.

## واژگان

| واژه | معنی در پروژه |
| --- | --- |
| Asset | اطلاعات تیکت، حساب، hash رمز، token و key ring |
| Actor | کاربر ناشناس، عضو، مدیر، پشتیبان و سرویس |
| Trust boundary | جایی که دادهٔ کم‌اعتماد وارد بخش معتبر می‌شود |
| Authentication | اثبات هویت با credential معتبر |
| Authorization | تصمیم دربارهٔ اجازهٔ یک عملیات روی یک منبع |
| Abuse case | استفادهٔ عمدی خلاف انتظار، مانند دیدن تیکت تیم دیگر |

هویت معتبر، اجازهٔ مشاهدهٔ همهٔ تیکت‌ها را نمی‌دهد.
بررسی مالکیت و نقش در درس‌های ۶۵ و ۶۶ می‌آیند؛ این محدودیت را در نقشه از همین حالا ثبت کن.

## نقشهٔ اعتماد

| جریان | چه چیزی کم‌اعتماد است؟ | مرز لازم |
| --- | --- | --- |
| مرورگر به API | body، query، header و cookie دستکاری‌شده | binding، validation و authentication معتبر |
| API به Application | هویت تأییدشده و command محدود | authorization و قواعد use case |
| Application به storage | وضعیت هم‌زمان و failure | invariant، constraint و commit |
| سرویس به ایمیل | لینک recovery و مقصد | مقصد تأییدشده و عدم ثبت token در log |
| host به key storage | کلیدهای Data Protection | دسترسی محدود، نگهداری و حفاظت کلید |

X-User یا X-Role درخواستی از client مدرک هویت نیست.
اگر reverse proxy مورد اعتماد هویتی می‌فرستد، اعتبار proxy و حذف header ورودی و ارتباط بین سرویس‌ها هم بخشی از طراحی است.
این نمونه چنین proxy قراردادی ندارد.

## جدول تهدید عملی

| تهدید | کنترل فعلی یا برنامه‌ریزی‌شده | شاهد مورد انتظار |
| --- | --- | --- |
| جعل هویت با header | پیش‌فرض بسته، سپس Identity | درخواست جعلی 401 |
| دیدن تیکت دیگران | کنترل مالکیت روی منبع، درس ۶۶ | کاربر معتبرِ غیرمالک رد شود |
| حدس رمز و credential stuffing | lockout + rate limit و monitoring | failure، lockout و budget مشخص |
| سرقت token | HTTPS، HttpOnly برای cookie، عمر محدود | plain HTTP رد و cookie policy بررسی شود |
| CSRF | token برای مسیرهای cookie تغییر‌دهنده | درخواست بدون token رد شود |
| SQL injection | پارامتر و قرارداد query | متن ورودی داده باقی بماند |
| افشای log و backup | حذف secret از خروجی و حفاظت storage | پاسخ و log عادی secret نداشته باشند |
| خروج ناگهانی storage | transaction و کنترل failure | تغییر نیمه‌کاره باقی نماند |

برخی کنترل‌ها هنوز درس آینده‌اند؛ نوشتن آن‌ها در جدول به معنی پیاده‌سازی نیست.
امنیت یک checkbox پس از ساخت نیست و هیچ ابزار واحدی تمام این تهدیدها را حل نمی‌کند.

## نمونهٔ بسته پیش از انتخاب provider

Security/ClosedGate.cs یک AuthenticationHandler دارد که همیشه NoResult می‌دهد.
این handler آزمایش «وقتی provider نداریم، چه می‌شود؟» است؛ provider ساختگی login نیست.
FallbackPolicy احراز هویت را برای endpointها لازم می‌کند و فقط /health صریحاً AllowAnonymous است.

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build();
```

/api/tickets از هویت تأییدنشده داده برنمی‌گرداند.
401 یعنی credential پذیرفته نشده؛ 403 بعداً برای هویت معتبر با اجازهٔ ناکافی مطرح می‌شود.
در این مرحله هیچ username ویژه‌ای مانند admin راه میان‌بر ندارد.

## اجرا

```powershell
dotnet run --project samples/60-threat-model/SupportDesk.csproj -- --urls http://127.0.0.1:5061
```

در PowerShell جدا /health پاسخ 200 می‌دهد، اما:

```powershell
Invoke-WebRequest 'http://127.0.0.1:5061/api/tickets' -Headers @{ 'X-User'='admin'; 'X-Role'='Administrator' }
```

انتظار 401 است؛ PowerShell آن را به شکل خطا نمایش می‌دهد.
این آزمایش local loopback بدون credential واقعی است؛ انتقال credential از درس ۶۴ سیاست HTTPS دارد.

```powershell
python scripts/security_smoke.py --build-only
```

این mode چهار نمونه را build و مرز بستهٔ همین درس را با درخواست واقعی بررسی می‌کند.
به معنی آزمایش login و PostgreSQL نیست؛ اجرای کامل اسکریپت اتصال دیتابیس می‌خواهد.
SDK 10 و Python 3 لازم‌اند.

## تمرین مستقل

برای قابلیت «دانلود گزارش تیکت‌ها» یک مدل تهدید بنویس.
دارایی، actor، ورودی و خروجی، مرز اعتماد و پنج abuse case مشخص داشته باشد.
برای هر مورد کنترل، تست منفی و ریسک باقی‌مانده بنویس.
صرفاً نوشتن «JWT»، «رمزنگاری» یا «validation» پاسخ کامل نیست.

### راهنمای مرحله‌ای

ابتدا یک درخواست واقعی را از مرورگر تا فایل خروجی دنبال کن.
سپس فرض کن کاربر معتبر است ولی عضو تیم دیگری است؛ آیا طراحی هنوز درست تصمیم می‌گیرد؟
در پایان نشان بده چه چیزهایی در log یا URL نباید ظاهر شوند.

## معیار عبور

- مرزها با نام ورودی و صاحب تصمیم مشخص باشند.
- هویت معتبر و مجوز منبع را جدا توضیح بدهی.
- /api/tickets با header و bearer جعلی باز نشود.
- کنترل انجام‌شده را با کنترل آینده مخلوط نکنی.
- هر تهدید حداقل یک تست منفی قابل اجرا داشته باشد.

## منابع رسمی

تطبیق در 2026-10-18:

- [مدل‌سازی تهدید Microsoft](https://learn.microsoft.com/en-us/azure/security/develop/threat-modeling-tool-threats)
- [مقدمهٔ authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0)

[بعدی: Identity](../062-identity/README.md) · [فهرست بخش](../README.md)
