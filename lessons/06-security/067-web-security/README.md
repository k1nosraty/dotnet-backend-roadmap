# درس 067 — CORS و CSRF و injection

## خروجی قابل مشاهده

preflight از origin مجاز header مناسب می‌گیرد و origin غیرمجاز آن را نمی‌گیرد.
بدون bearer، origin مجاز هم به تیکت دسترسی ندارد؛ ورودی شبیه SQL به query تبدیل نمی‌شود.
پیش‌نیاز: [درس ۶۶](../066-ownership/README.md)، HTTP، cookie و mapping.

## سه مرز متفاوت

| مسئله | پرسش | کنترل در این نمونه |
| --- | --- | --- |
| CORS | کدام صفحهٔ مرورگر می‌تواند پاسخ cross-origin را بخواند؟ | allowlist دقیق origin و method و header |
| CSRF | آیا credential خودکار browser باعث عمل ناخواسته می‌شود؟ | antiforgery برای مسیر cookie و جداسازی scheme |
| Injection | آیا داده به دستور یا markup اجرایی تبدیل می‌شود؟ | query پارامتری و پاسخ JSON و DTO محدود |

CORS جای authentication یا ownership نیست. ابزار CLI و مهاجم سروری تابع همان محدودیت مرورگر نیستند.
حتی درخواست سادهٔ cross-origin ممکن است اجرا شود و فقط خواندن پاسخ در مرورگر محدود شود؛ بنابراین CSRF را با نبودن CORS header حل نکن.
Origin هم credential قابل اعتمادِ یک ابزار دلخواه نیست.

## policy مرورگر

در Program.cs یک policy با نام Portal داریم: فقط https://portal.example.test، متدهای GET و POST و headerهای Authorization و Content-Type و X-CSRF-TOKEN.
scheme و host و port، origin را مشخص می‌کنند؛ پسوند شبیه دامنهٔ مجاز پذیرفته نمی‌شود.
AllowAnyOrigin و AllowCredentials نداریم. portal در این lab با bearer صریح کار می‌کند؛ cross-origin cookie پشتیبانی نمی‌شود.
آدرس example.test قرارداد آموزشی است، نه frontend آنلاین یا سرویس login خارجی.

UseRouting پیش از UseCors و UseAuthentication و UseAuthorization قرار دارد.
preflight لازم نیست خودش bearer داشته باشد؛ درخواست اصلی هنوز باید authenticate و authorize شود.
پاسخ 204 برای OPTIONS به‌تنهایی نشانهٔ دسترسی مجاز نیست؛ Access-Control-Allow-Origin و method/headerهای پاسخ را بخوان.
برای origin غیرمجاز ممکن است OPTIONS هم 204 باشد ولی header مجازکننده نداشته باشد.

## CSRF از login تا mutation

/auth/login?mode=cookie قبل از ورود هم antiforgery می‌خواهد. token anonymous پس از login جای token وابسته به principal جدید را نمی‌گیرد.
/auth/csrf token جدید می‌دهد و cookie-action و logout آن را از X-CSRF-TOKEN اعتبارسنجی می‌کنند.
/api/tickets فقط bearer را می‌پذیرد؛ cookie به‌تنهایی درخواست ساخت یا rename را مجاز نمی‌کند.
body JSON، SameSite و HttpOnly به‌تنهایی قرارداد کامل CSRF نیستند.
اگر بعداً endpoint تیکت برای cookie باز شد، باید کنترل CSRF تمام mutationها و تست منفی آن‌ها هم اضافه شود.
XSS می‌تواند رفتار browser واردشده را سوءاستفاده کند؛ CSRF راه‌حل همهٔ حمله‌های سمت client نیست.

## query ایمن

GET /api/tickets/search?term=...، scope مالک و tenant درس قبل را حفظ می‌کند و بعد Title.Contains(term) را روی EF query اعمال می‌کند.
term متغیرِ query است؛ رشتهٔ SQL با ورودی HTTP ساخته نمی‌شود.
طول term حداکثر ۱۲۰ و خروجی حداکثر ۵۰ ردیف است؛ ورودی طولانی 400 می‌گیرد.
نمونه‌های ' OR 1=1 -- و % و _ داده‌اند؛ نباید شرط owner را حذف کنند یا wildcard آزاد شوند.
تست وجود عنوان معمولی و نتیجهٔ خالی ورودی‌های مخرب را روی PostgreSQL واقعی می‌سنجد.

اگر raw SQL لازم شد، SQL و مقدار را جدا کن؛ FromSql با interpolation پارامتری با الحاق رشته به FromSqlRaw یکسان نیست.
نام ستون یا جهت ORDER BY معمولاً value parameter نیست؛ برای آن‌ها mapping allowlist از گزینه‌های شناخته‌شده لازم است.
escape دستی و حذف چند کاراکتر خطرناک را جایگزین پارامتر نکن.
دسترسی database role نیز باید محدود باشد؛ پارامترگذاری به‌تنهایی مجوز resource را اعمال نمی‌کند.

## متن کاربر و XSS

عنوانی با متن <script>alert(1)</script> عمداً به‌عنوان داده پذیرفته می‌شود.
پاسخ application/json و X-Content-Type-Options: nosniff دارد و DTO فقط داده را برمی‌گرداند.
این تست، ایمنی هر frontend آینده را ثابت نمی‌کند؛ client باید متن را با textContent یا escaping معمول framework نمایش دهد و سراغ innerHTML برای متن کاربر نرود.
اگر rich text واقعاً لازم شد، sanitizer معتبر و policy محدود محتوا و تست browser لازم است.
این snapshot HTML UI ندارد؛ CSP و DOM-based XSS و اجرای مرورگر با این تست HTTP پوشش داده نمی‌شوند.

## راه‌اندازی آزمایشگاه

ابتدا [راهنمای PostgreSQL](../../../handbook/postgresql-lab.md) را انجام بده.
یک database اختصاصی با role دارای مجوز ساخت schema لازم است؛ از database محصول استفاده نکن.
هر نمونه schema و key ring مستقل دارد و جدول موجود را جایگزین نمی‌کند.
فرمان init، ساخت حساب را فقط در CLI محلیِ Development انجام می‌دهد؛ endpoint ثبت‌نام یا اعطای نقش وجود ندارد.

```powershell
$schema = 'auth67_' + [guid]::NewGuid().ToString('N')
$env:SUPPORTDESK_PG = "Host=127.0.0.1;Database=supportdesk_lab;Username=course;Password=رمز_آزمایشگاه;Search Path=$schema"
$env:TEAMDESK_KEYS = "$env:TEMP/teamdesk-access-keys-$schema"
New-Item -ItemType Directory -Force $env:TEAMDESK_KEYS | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TEAMDESK_INSECURE_LAB = '1'
$secret = Read-Host 'Disposable password: 12+ characters and default Identity complexity' -AsSecureString
$env:TEAMDESK_LAB_PASSWORD = [System.Net.NetworkCredential]::new('', $secret).Password
dotnet run --project samples/66-web-security/SupportDesk.csproj -- --init-lab
Remove-Item Env:TEAMDESK_LAB_PASSWORD
dotnet run --project samples/66-web-security/SupportDesk.csproj -- --urls http://127.0.0.1:5067
```

این lab شش حساب موقت می‌سازد؛ رمز انتخابی فقط برای همین آزمایش است. رمز را در تاریخچهٔ فرمان ننویس.
همهٔ حساب‌ها همان رمز موقت دارند و CLI تأییدشان می‌کند؛ ایمیل واقعی ارسال نمی‌شود.
حساب‌ها و نقش‌ها توسط client تعیین نمی‌شوند:

| حساب | سازمان | نقش SupportAgent | permission=tickets.manage |
| --- | --- | --- | --- |
| owner@example.test | alpha | خیر | خیر |
| peer@example.test | alpha | خیر | خیر |
| other@example.test | beta | خیر | خیر |
| staff@example.test | alpha | بله | بله |
| roleonly@example.test | alpha | بله | خیر |
| claimonly@example.test | alpha | خیر | بله |

برای ورود، POST /auth/login با JSON شامل email و password بزن و accessToken را فقط در حافظهٔ client نگه دار.
درخواست بعدی، header با شکل Authorization: Bearer <accessToken> دارد؛ token را چاپ، commit یا در URL قرار نده.
مسیرهای cookie، CSRF، refresh و logout درس ۶۴ حفظ شده‌اند.
بدون flag محلی، HTTP رد می‌شود و cookie Secure است؛ در Production این flag باعث شکست startup می‌شود.
key ring خصوصی است؛ HTTPS، حفاظت at-rest و تنظیم proxy واقعی هنوز مسئولیت استقرارند.
محدودیت refresh reuse، عمر access token و اثر محدود logout همان درس ۶۴ است.
این نمونه fresh-schema آموزشی دارد؛ migration محصول را از بخش data پیاده کن.


## آزمایش دستی مرورگر و HTTP

با یک client HTTP، OPTIONS /api/tickets را با Origin مجاز و Access-Control-Request-Method: POST بفرست.
دوباره origin را به https://evil.example.test عوض کن و نبود header مجازکننده را مقایسه کن.
سپس با bearer معتبر و Origin غیرمجاز GET بزن؛ server می‌تواند 200 بدهد، ولی header اجازهٔ خواندن مرورگر را ندارد.
بدون bearer، Origin مجاز نباید پاسخ خصوصی بدهد.
یک عنوان markup ثبت کن و Content-Type پاسخ و رفتار نمایش امن را جدا بررسی کن.

## اجرای بررسی

```powershell
python scripts/check_content.py
python scripts/security_next_smoke.py --build-only
python scripts/security_next_smoke.py
```

اجرای کامل SDK 10، Python 3، PostgreSQL، psql و SUPPORTDESK_PG می‌خواهد.
متغیرهای SUPPORTDESK_TEST_PGHOST، SUPPORTDESK_TEST_PGPORT، SUPPORTDESK_TEST_PGDATABASE، SUPPORTDESK_TEST_PGUSER و SUPPORTDESK_TEST_PGPASSWORD را مطابق همان اتصال تنظیم کن؛ جزئیات در [فهرست بخش](../README.md) است.
اسکریپت برای هر snapshot یک schema تصادفی و key ring موقت می‌سازد، خروجی publish را از پوشهٔ جدا اجرا می‌کند و در پایان فقط schema خودش را حذف می‌کند.
--build-only ساخت و HTTP ناشناس/جعلی و fallback را بررسی می‌کند و آزمون مجوز واقعی دیتابیس نیست.
password و token در خروجی تست چاپ نمی‌شوند.


## تمرین مستقل

برای frontend واقعی خودت origin دقیق dev و production را در configuration جدا قرار بده؛ wildcard نساز.
یک صفحهٔ کوچک با نمایش textContent و یک تست مرورگر برای عنوان markup بساز.
برای endpoint cookie جدید، تست absent token، token نادرست و token معتبر بنویس؛ فقط تست موفق کافی نیست.

### سرنخ

سه تصمیم CORS، CSRF و authorization را برای یک درخواست در سه جملهٔ جدا بنویس.
برای SQL ورودی مخرب را فقط رد نکن؛ نشان بده query scope و پارامترگذاری حتی هنگام جست‌وجو حفظ شده‌اند.

## معیار عبور

- allowlist دقیق origin و نبود AllowCredentials را بررسی کنی.
- origin مجاز بدون bearer داده نگیرد.
- cookie mutation بدون token ضد CSRF رد شود.
- جست‌وجوی مخرب نه دادهٔ دیگران برگرداند و نه query را تغییر دهد.
- JSON شدن متن را تضمین ایمنی HTML مصرف‌کننده ندانی.

## منابع رسمی

بررسی در 2026-10-18:

- [CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0)
- [Antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [SQL query و parameter](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries)
- [XSS و encoding](https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0)

[قبلی](../066-ownership/README.md) · [فهرست بخش](../README.md) · [بعدی](../068-rate-limits/README.md)
