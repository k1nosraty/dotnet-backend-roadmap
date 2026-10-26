# مکمل ۶۴ـالف — JWT و اعتبارسنجی واقعی API

پیش‌نیاز: [درس ۶۴](../064-authentication/README.md)، HTTP و تفاوت احراز هویت و مجوز. این مکمل بخشی از مسیر امنیت است؛ شماره‌های اصلی ۹۰ درس تغییر نکرده‌اند.

## مسئله: API چطور به token اعتماد کند؟

client می‌گوید «من learner هستم و اجازهٔ خواندن تیکت دارم». نوشتن همین متن در header یا حتی Base64 کردن آن، دلیل اعتماد نیست. API باید بفهمد credential را صادرکنندهٔ مورد اعتماد صادر کرده، برای همین API است، دست‌کاری نشده و هنوز معتبر است. بعد از آن، تازه بررسی مجوز عملیات شروع می‌شود.

در درس ۶۴ protected bearer مخصوص Identity داشتیم. این نمونه از **AddJwtBearer** برای اعتبارسنجی JWT استفاده می‌کند؛ API نقش resource server دارد، نه صفحهٔ ورود یا صادرکنندهٔ توکن.

## ۱. JWT چیست و چه چیزی نیست؟

JWT مخفف JSON Web Token است. نمونهٔ امضاشدهٔ JWS در این درس سه بخش جداشده با نقطه دارد:

| بخش | چه دارد؟ | چه چیزی را ثابت نمی‌کند؟ |
| --- | --- | --- |
| header | نوع توکن، الگوریتم و گاهی kid برای انتخاب کلید | خود header قابل اعتماد نیست تا اعتبارسنجی کامل شود |
| payload | claimها، یعنی گزاره‌های صادرشده دربارهٔ subject و کاربرد توکن | صرف خواندن claim به معنی معتبر بودن آن نیست |
| signature | امضای داده با کلید صادرکننده | رمزگذاری یا مخفی‌کردن payload نیست |

Base64url یک شیوهٔ نمایش بایت‌هاست، نه رمزنگاری. payload توکن امضاشده را می‌توان خواند؛ password، secret و اطلاعات حساس را داخل آن نگذار. این درس JWS با RS256 را بررسی می‌کند؛ JWT رمز‌شدهٔ JWE موضوع این lab نیست. Bearer روش حمل credential است، JWT شکل داده؛ هر bearer الزاماً JWT نیست.

## ۲. claimها را با یک سناریو بخوان

فرض کن issuer به یک client اجازه داده برای learner، خلاصهٔ تیکت را از course-api بخواند:

| claim / داده | معنی |
| --- | --- |
| iss | صادرکننده‌ای که باید دقیقاً مورد اعتماد API باشد |
| aud | مخاطب؛ اینجا course-api، نه هر برنامه‌ای که توکن را دریافت کرد |
| sub | شناسهٔ subject در محدودهٔ issuer؛ لزوماً email نیست |
| exp | زمان انقضا؛ عدد NumericDate بر حسب ثانیه از Unix epoch |
| nbf | پیش از این زمان توکن قابل پذیرش نیست |
| iat | زمان صدور؛ به‌تنهایی جای exp و اعتبارسنجی نیست |
| scope | محدودهٔ اختیار واگذارشده؛ نمونه tickets.read را می‌خواهد |
| roles | نقش، اگر قرارداد issuer آن را صادر کرده باشد؛ با scope یکی نیست |
| jti | شناسهٔ توکن در قراردادهای دارای آن؛ وجودش به‌تنهایی replay را متوقف نمی‌کند |

scope حق انجام کاری در این API را توصیف می‌کند؛ نقش معمولاً جایگاه actor را بیان می‌کند. حتی داشتن tickets.read به‌تنهایی مجوز خواندن تیکت سازمان دیگر نیست؛ کنترل مالکیت درس ۶۶ همچنان لازم است. endpoint این نمونه فقط دادهٔ مصنوعی ثابت می‌دهد.

## ۳. API چه چیزهایی را واقعاً بررسی می‌کند؟

[Program](../../../supplements/identity/JwtApi/Program.cs) را از AddAuthentication تا MapGet دنبال کن:

1. Authority از تنظیمات و فقط با HTTPS پذیرفته می‌شود؛ metadata و کلید عمومی از مسیر استاندارد صادرکننده به دست می‌آیند.
2. JwtBearer امضا، issuer، audience، exp و nbf را بررسی می‌کند؛ RequireExpirationTime فعال است.
3. این lab فقط الگوریتم RS256 و نوع at+jwt را قبول می‌کند؛ client نمی‌تواند با تغییر alg بررسی امضا را خاموش کند.
4. sub خالی رد می‌شود. MapInboundClaims خاموش است تا نام claimها در این تمرین همان نام‌های صادرشده بماند.
5. policy ReadTickets پس از احراز هویت، scope را بررسی می‌کند. scope چند مقدار با فاصله دارد؛ جست‌وجوی substring مانند Contains روی متن خام مناسب نیست.

ClockSkew پنج ثانیه تلورانس زمانی است، نه افزایش اختیاری عمر توکن توسط client. نمونهٔ API پروفایل محدود access token با typ=at+jwt را انتخاب کرده است؛ هر provider چنین توکنی صادر نمی‌کند. پیش از اتصال واقعی، قرارداد provider را تطبیق بده. با خاموش‌کردن ValidateAudience یا امضا، ناسازگاری را پنهان نکن.

RS256 امضای نامتقارن است: issuer کلید خصوصی را نگه می‌دارد و API کلید عمومی را برای بررسی می‌گیرد. JWKS مجموعهٔ کلیدهای عمومی است و kid به انتخاب کلید کمک می‌کند؛ آن را به URL دلخواه داخل token تبدیل نکن. cache/refresh کلیدها را handler استاندارد اداره می‌کند؛ rotation را در محیط واقعی جدا آزمایش کن.

## ۴. اجرای قابل تکرار بدون حساب خارجی

از ریشهٔ مخزن، با SDK 10:

```powershell
dotnet test supplements/identity/JwtApi.Tests/JwtApi.Tests.csproj
python scripts/identity_smoke.py
```

۱۷ case اجرا می‌شوند: توکن معتبر، scope ناکافی، ناشناس، token در query string، HTTP نامجاز، Authority نامعتبر و token با issuer/audience/امضا/الگوریتم/نوع/زمان نامعتبر یا sub/exp مفقود و payload دست‌کاری‌شده. انتظار صفر skip و failure است. فایل [JwtTests](../../../supplements/identity/JwtApi.Tests/JwtTests.cs) توکن مصنوعی را با RSA تصادفی می‌سازد؛ کلید خصوصی و ابزار صدور فقط در test assembly هستند. API هیچ endpoint ساخت token ندارد.

این تست، JwtBearer واقعی و cryptography واقعی را داخل WebApplicationFactory اجرا می‌کند؛ metadata در این suite کنترل‌شده است. شبکه، TLS واقعی و issuer بیرونی ادعا نمی‌شوند. JSON خواندن توکن، جای این تست اعتبارسنجی نیست.

## ۵. مشاهدهٔ 401 و 403

| درخواست | نتیجهٔ همین API |
| --- | --- |
| بدون Authorization یا credential نامعتبر | 401 |
| access token معتبر با scope دیگر به /api/me | 200؛ هویت معتبر است |
| همان token به /api/tickets | 403؛ اختیار tickets.read ندارد |
| access token معتبر و scope لازم | 200 |
| ID token با typ=JWT به API | 401؛ نوع credential این API نیست |

در تست Valid_identity_without_required_scope_gets_403 همین دو مسیر را کنار هم ببین. ابتدا نتیجه را حدس بزن؛ سپس scope توکن آزمایشی را فقط در کپی تمرین تغییر بده. API به token داخل URL یا X-Role اعتماد نمی‌کند.

برای اتصال به issuer واقعی، در همان ترمینال server تنظیم کن:

```powershell
$env:Tokens__Authority = 'https://issuer.example.test/realms/course'
$env:Tokens__Audience = 'course-api'
dotnet run --project supplements/identity/JwtApi/JwtApi.csproj -- --urls https://localhost:5444
```

URL بالا **placeholder** است؛ با issuer واقعی، معتبر و قابل دسترس با metadata/JWKS و قرارداد RS256/at+jwt جایگزینش کن. HTTPS محلی، گواهی development مورد اعتماد می‌خواهد؛ روش trust در مکمل بعد آمده است. scope لازم باید در access token صادرشده وجود داشته باشد. API آمادهٔ تولید token برای client نیست؛ این کار با authorization server است. token واقعی را در log، screenshot، URL یا ابزار decode عمومی قرار نده.

## ۶. محدودیت عمر و refresh

JWT امضاشده به‌تنهایی فهرست session و ابطال فوری ندارد. این API پس از صدور توکن برای هر درخواست به DB حساب مراجعه نمی‌کند؛ حذف نقش در DB خودبه‌خود claim قدیمی را عوض نمی‌کند. با درس ۶۵ که grant فعلی را می‌خواند مقایسه کن. عمر کوتاه، revocation، cache مجوز و rotation نیاز به تصمیم و تست دارند.

refresh token به authorization server فرستاده می‌شود، نه به /api/tickets. این API refresh endpoint ندارد. client credentials برای هویت یک برنامه بدون کاربر، با ورود تعاملی OIDC کاربر یکی نیست؛ scope و audience همچنان باید درست باشند. ساخت endpoint password-to-JWT دلخواه، جای پیاده‌سازی استاندارد OAuth نیست.

## تمرین و معیار عبور

- برای هر سه بخش JWT با زبان خودت توضیح بده چه کسی آن را می‌سازد و چه کسی اعتبارسنجی می‌کند.
- در JwtTests تغییر issuer، حذف exp و تغییر scope را جدا دنبال کن؛ دو مورد اول 401 و مورد سوم روی endpoint مجوزدار 403 است.
- یک policy tickets.write اضافه کن؛ token فقط دارای tickets.read نباید عبور کند.
- تهدید تغییر کلید، توکن قدیمی و مالکیت سازمان را در ADR بنویس؛ صرف امضای معتبر را مجوز همهٔ داده‌ها ندان.

عبور یعنی فرق decode/validation، authentication/authorization و access/ID/refresh token را با تست توضیح بدهی، نه فقط token را در یک صفحه نمایش بدهی.

## منابع رسمی

- [Microsoft JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [JWT، RFC 7519](https://www.rfc-editor.org/rfc/rfc7519)
- [JWT access-token profile، RFC 9068](https://www.rfc-editor.org/rfc/rfc9068)
- [OAuth Security BCP، RFC 9700](https://www.rfc-editor.org/rfc/rfc9700)

[بعدی: OAuth/OIDC و PKCE](../064b-oidc/README.md) · [فهرست امنیت](../README.md)
