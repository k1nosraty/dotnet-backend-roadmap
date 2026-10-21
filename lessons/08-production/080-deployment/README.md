# درس ۰۸۰ — HTTPS جلوی برنامه، HTTP پشت proxy

مرورگر باید HTTPS ببیند، اما برنامهٔ پشت reverse proxy ممکن است روی HTTP داخلی اجرا شود.
در این درس یک Caddy جلوی Kestrel می‌گذاریم و بررسی می‌کنیم برنامه پروتکل اصلی درخواست را درست تشخیص بدهد.

## ۱. اول publish را از run جدا کن

از ریشهٔ مخزن:

```powershell
dotnet publish samples/79-proxy-tls/SupportDesk.csproj -c Release -o artifacts/lesson80 -p:UseAppHost=false
dotnet artifacts/lesson80/SupportDesk.Lesson80.dll --urls http://127.0.0.1:5080
```

اجرای مستقیم برای توسعه هم ممکن است:

```powershell
dotnet run --project samples/79-proxy-tls/SupportDesk.csproj --urls http://127.0.0.1:5080
```

در هر دو حالت، درخواست HTTP به /info پاسخ 307 و Location برابر `https://localhost:8443/info` می‌گیرد، اگر با Host برابر localhost درخواست بدهی.
این redirect خودش TLS server نمی‌سازد؛ هنوز باید proxy مرحلهٔ بعد را اجرا کنی. میزبان را با localhost صدا بزن؛ AllowedHosts نمونه فقط همین نام است.
اجرای قبلی را با Ctrl+C تمام کن و تست مرز proxy را ببین:

```powershell
dotnet test tests/080-proxy-tls/SupportDesk.Tests.csproj
```

## ۲. مسیر درخواست واقعی

| اتصال | پروتکل | مقصد |
| --- | --- | --- |
| client به proxy | HTTPS | localhost:8443 روی میزبان |
| proxy به app | HTTP | app:8080 در شبکهٔ داخلی Docker |

پورت app روی میزبان publish نشده است. در این lab، TLS در proxy تمام می‌شود.
برای شبکهٔ نامطمئن یا عبور بین hostها باید رمزنگاری اتصال داخلی را هم جدا بررسی کنی؛ این نمونه شبکهٔ محلی Docker دارد.

## ۳. اجرای TLS محلی

Docker با Linux containers لازم است. همهٔ فرمان‌ها از ریشهٔ مخزن هستند:

```powershell
docker compose -p course80 -f samples/79-proxy-tls/compose.yaml up -d --build
docker compose -p course80 -f samples/79-proxy-tls/compose.yaml logs proxy
```

Caddy با `tls internal` گواهی localhost را از CA محلی خودش می‌سازد. این CA عمومی نیست و مرورگر تو از قبل به آن اعتماد ندارد.
برای آزمایش curl، **فقط گواهی عمومی root** را به پوشهٔ موقت ببر:

```powershell
docker compose -p course80 -f samples/79-proxy-tls/compose.yaml cp proxy:/data/caddy/pki/authorities/local/root.crt "$env:TEMP/course80-root.crt"
curl.exe --noproxy '*' --cacert "$env:TEMP/course80-root.crt" https://localhost:8443/info
```

اگر cp گفت فایل هنوز ساخته نشده، logs را بررسی کن و پس از آماده شدن proxy دوباره بزن.
در Linux مسیر موقت مثل /tmp/course80-root.crt را جایگزین متغیر PowerShell کن.
پاسخ:

```json
{"scheme":"https","secure":true}
```

--cacert اعتماد را به همین درخواست و همان فایل محدود می‌کند. از -k یا خاموش کردن اعتبارسنجی certificate استفاده نکن؛ آن کار hostname و اعتماد به CA را آزمایش نمی‌کند.
کلید خصوصی CA را خارج نکن و CA آزمایش را در trust store سراسری نصب نکن.

## ۴. چرا برنامه https می‌بیند؟

Caddy مقدار X-Forwarded-Proto را بر اساس اتصال واقعی client تنظیم می‌کند.
`UseForwardedHeaders` پیش از `UseHttpsRedirection` اجرا می‌شود؛ در نتیجه redirect تصمیم خود را با scheme اصلاح‌شده می‌گیرد.
اگر این ترتیب برعکس باشد، امکان redirect loop وجود دارد.

در Program.cs فقط یک peer با IP مشخص معتبر است؛ تنظیم Compose آن را 172.31.79.10 گذاشته است.
KnownProxies شامل همین آدرس و شکل IPv4-mapped آن است و ForwardLimit برابر یک است.
همهٔ شبکه‌ها را trusted نکرده‌ایم. Host forwarding فعال نیست و AllowedHosts فقط localhost را می‌پذیرد.

| درخواست | نتیجهٔ مورد انتظار |
| --- | --- |
| peer معتبر + proto=https | /info پاسخ 200 |
| peer ناشناس + proto=https جعلی | header نادیده گرفته می‌شود؛ redirect 307 |
| Host نامعتبر | رد درخواست |
| client به Caddy با proto=http جعلی، روی TLS واقعی | Caddy مقدار را بازنویسی می‌کند؛ app همچنان https می‌بیند |

پیش‌فرض اجرای بدون Compose، proxy را 127.0.0.1 می‌داند. این یعنی processهای همان ماشین در آن آزمایش trusted هستند؛ تنظیم محصول نیست.
IP و subnet این lab را برای محیط خودت بررسی کن. اگر با شبکهٔ موجود تداخل دارد، subnet و IP ثابت proxy و ReverseProxy__KnownIp را هماهنگ عوض کن؛ صرفاً KnownProxies را خالی نکن.

## ۵. چه چیزهایی واقعاً تست می‌شوند؟

```powershell
python scripts/production_docker_smoke.py
```

این اسکریپت یک پروژهٔ Compose با نام تصادفی می‌سازد، root certificate عمومی را موقت می‌خواند و درخواست TLS واقعی می‌فرستد.
هم موفقیت با CA درست و hostname localhost و هم شکست با trust پیش‌فرض را بررسی می‌کند؛ سپس جعل scheme، Host نامعتبر و منتشر نشدن پورت backend را می‌سنجد.
WAF در tests/080-proxy-tls جای TLS نیست؛ آن تست جداگانه تصمیم middleware با peerهای مختلف را بررسی می‌کند.

## ۶. پاک‌سازی و مرز استقرار

فقط برای resourceهای همین lab که با نام course80 ساختی:

```powershell
docker compose -p course80 -f samples/79-proxy-tls/compose.yaml down --volumes
Remove-Item "$env:TEMP/course80-root.crt"
```

--volumes در این فرمان CA آزمایشی همین پروژه را حذف می‌کند. روی Compose دیتابیس واقعی بدون فهم پیامد آن اجرا نکن.
برای دامنهٔ عمومی باید DNS، دسترسی پورت‌ها، صدور گواهی عمومی، نگه‌داری state گواهی و تنظیم proxy واقعی مشخص شوند؛ localhost و tls internal را مستقیم به production منتقل نکن.
این درس روی سرویس عمومی چیزی deploy نمی‌کند.

## تمرین

1. ترتیب دو middleware را در کپی موقت برعکس کن؛ تست trusted proxy باید خطا را پیدا کند.
2. header جعلی proto=http را با curl روی اتصال TLS بفرست؛ پاسخ باید همچنان secure=true باشد.
3. توضیح بده secret به‌عنوان environment متغیر runtime با COPY کردن آن در image چه تفاوتی دارد. این نمونه secret ندارد.

معیار عبور: هم handshake معتبر TLS و هم رد peer/Host نامعتبر شاهد اجرایی داشته باشند.

## منابع رسمی

- [Forwarded headers و trust](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [Caddy automatic HTTPS و CA محلی](https://caddyserver.com/docs/automatic-https)
- [Caddy reverse_proxy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy)
- [Caddy official image](https://hub.docker.com/_/caddy)

[قبلی](../079-docker/README.md) · [بعدی](../081-ci/README.md) · [فهرست بخش](../README.md)
