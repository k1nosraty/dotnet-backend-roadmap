# درس ۰۸۳ — cache سریع، بدون زنده کردن دادهٔ قدیمی

فرض کن ساختن خلاصهٔ تیکت پرهزینه است. می‌خواهیم خلاصه را نگه داریم تا دوباره نسازیم؛ اما بعد از تغییر عنوان، خلاصهٔ قبلی نباید جای نتیجهٔ تازه بنشیند.
ابتدا cache حافظه‌ای را اجرا می‌کنیم، سپس همان قرارداد را با Redis واقعی بررسی می‌کنیم.

## ۱. اجرا و خروجی

از ریشهٔ مخزن با SDK 10:

```powershell
dotnet run --project samples/82-versioned-cache/SupportDesk.csproj
dotnet test tests/083-cache/SupportDesk.Tests.csproj --filter 'Category!=Docker'
```

بدون SUPPORTDESK_REDIS، نمونه از حافظه استفاده می‌کند. خروجی:

```text
first=PRINTER; second=PRINTER; updated=NETWORK; builds=2
```

دو خواندن اول یک projection می‌سازند؛ بعد از تغییر عنوان projection تازه ساخته می‌شود.
این برنامه فقط دادهٔ مصنوعی دارد و TicketSource آن حافظه‌ای است؛ منبع پایدار یا سیستم هویت کامل نیست.

## ۲. cache-aside را از روی فایل بخوان

cache یک نگه‌دارندهٔ موقتِ نتیجه است؛ منبع حقیقت همان داده‌ای است که تغییر معتبر در آن انجام می‌شود. cache hit یعنی برای کلید مورد نظر نتیجهٔ قابل قبول پیدا شده؛ cache miss یعنی باید نتیجه را از مسیر اصلی بسازیم. projection این نمونه تبدیل عنوان به خلاصهٔ نمایشی با حروف بزرگ است.

خروجی بخش قبل را با همین مراحل توضیح بده:

| فراخوانی در Program | اتفاق | شمار ساخت خلاصه |
| --- | --- | --- |
| خواندن اول Printer | miss؛ خلاصهٔ PRINTER ساخته می‌شود | ۱ |
| خواندن دوم همان نسخه | hit؛ همان نتیجه استفاده می‌شود | ۱ |
| تغییر source به Network و خواندن | نسخهٔ تازه، کلید تازه؛ NETWORK ساخته می‌شود | ۲ |

cache-aside یعنی کد سرویس تصمیم می‌گیرد ابتدا cache را بررسی کند و در miss نتیجه را بسازد و آنجا نگه دارد؛ cache خودش منبع را به‌طور خودکار هماهنگ نمی‌کند. در این نمونه، پیش از lookup هنوز snapshot منبع خوانده می‌شود؛ تعداد builds شمار ساخت projection است، نه شمار مراجعه به source.

| جزء در Cache.cs | مسئولیت |
| --- | --- |
| TicketSource | منبع حقیقت و نسخهٔ فعلی تیکت |
| CachedSummary | گرفتن snapshot، lookup و ساخت projection در cache miss |
| ICache | قرارداد نگه‌داری موقت |
| RedisCache | adapter واقعی StringGet/Set/Delete |
| MemoryCache | adapter محدودِ آموزشی، با سقف ۱۲۸ entry و clock قابل کنترل |

ReadAsync اول snapshot معتبر را از source می‌گیرد، سپس کلیدش را می‌سازد.
کلید، namespace و tenant و شناسهٔ تیکت و **نسخهٔ داده** را دارد. JSON معتبر با نسخهٔ متفاوت پذیرفته نمی‌شود.
در cache miss، project اجرا و نتیجه با TTL سی ثانیه ذخیره می‌شود.

این نمونه خواندن منبع برای گرفتن revision را حذف نمی‌کند؛ هدفش جلوگیری از تکرار projection است. برای کاهش query واقعی، باید هزینهٔ version lookup و قرارداد سازگاری را جدا بسنجی.

## ۳. چرا فقط Delete کافی نیست؟

این ترتیب هم‌زمان ممکن است رخ بدهد:

| لحظه | خوانندهٔ قدیمی | نویسنده |
| --- | --- | --- |
| ۱ | snapshot نسخهٔ ۱ را می‌گیرد | — |
| ۲ | ساخت خلاصه هنوز تمام نشده | نسخهٔ ۲ را commit و کلید قبلی را حذف می‌کند |
| ۳ | خلاصهٔ نسخهٔ ۱ را دیر ذخیره می‌کند | — |

اگر هر دو نسخه یک کلید داشتند، مرحلهٔ سوم می‌توانست دادهٔ قدیمی را دوباره زنده کند.
اینجا کلید نسخهٔ ۱ با نسخهٔ ۲ فرق دارد. یک read تازه بعد از commit به کلید نسخهٔ ۲ می‌رود؛ نوشتهٔ دیررسِ کلید قبلی به آن راه ندارد.
خواننده‌ای که پیش از write snapshot گرفته، هنوز ممکن است همان snapshot قدیمی را دریافت کند؛ این قرارداد، linearizability کامل نیست.
تست Late_old_projection با signal، همین ترتیب را قطعی می‌سازد؛ به Sleep تکیه نمی‌کند.

## ۴. invalidation و TTL

invalidation یعنی نتیجهٔ قدیمی را از مسیر پاسخ معتبر خارج کنیم. TTL مخفف Time To Live و مدت اعتبار entry است؛ بعد از انقضا، آن entry نباید پاسخ معتبر این cache محسوب شود. TTL قول نمی‌دهد در طول همان مدت هیچ تغییری در source رخ ندهد.

best-effort در حذف یعنی برای پاک‌کردن تلاش می‌کنیم ولی درستی خواندن تازه را فقط به موفقیت آن تکیه نمی‌دهیم؛ کلید نسخه‌دار این نقش را دارد. eviction یعنی بیرون‌انداختن entryها طبق سیاست ظرفیت حافظه؛ با انقضای زمانی یک موضوع نیست.

برای فهم race بخش قبل، حدس بزن اگر آخرین نوشتنِ خوانندهٔ قدیمی بعد از Delete برسد چه می‌شود. در یک کلید مشترک، نتیجهٔ قدیمی ممکن است دوباره دیده شود؛ در دو کلید نسخه‌دار، read تازه نسخهٔ جدید را انتخاب می‌کند. این تفاوت علت وجود Version در key است، نه صرفاً تزئین نام کلید.

UpdateAsync اول source را تغییر می‌دهد و بعد حذف کلید قبلی را به‌صورت best-effort انجام می‌دهد.
اگر حذف شکست بخورد، کلید قدیمی به‌دلیل version از خواندن تازه کنار می‌رود و TTL عمر باقیماندهٔ آن را محدود می‌کند.
نسخه باید در منبع واقعی پایدار و غیرقابل استفادهٔ مجدد باشد؛ بعد از restore یا تغییر timeline، namespace/epoch cache را عوض کن.
حافظهٔ نمونه برای آموزش است و حذف سادهٔ entry دارد؛ روی سرور از سیاست eviction مدیریت‌شده استفاده کن.
Redis هم نیاز به بودجهٔ حافظه، maxmemory و سیاست eviction دارد؛ TTL به‌تنهایی کل ظرفیت را تضمین نمی‌کند.

## ۵. Redis واقعی

[آزمایشگاه عملیات](../../../handbook/operations-lab.md) یک Redis موقت با پورت loopback راه می‌اندازد.
پس از تنظیم SUPPORTDESK_REDIS همان فرمان run را بزن. برای تست کانتینری مستقل:

```powershell
dotnet test tests/083-cache/SupportDesk.Tests.csproj --filter 'Category=Docker'
```

Docker با Linux containers لازم است. Testcontainers، Redis خودش با پورت پویا می‌سازد و پس از تست جمع می‌کند؛ cache شخصی تو را خالی نمی‌کند.
تست‌ها roundtrip، TTL واقعی، حذف و انقضای نهایی را بررسی می‌کنند.
ConnectionMultiplexer یک بار ساخته و بین عملیات‌ها استفاده می‌شود؛ برای هر Get اتصال تازه نمی‌سازیم.
WaitAsync لغوِ انتظار caller است؛ تضمین نمی‌کند فرمان Redis که قبلاً ارسال شده، دیگر اجرا نمی‌شود.

## ۶. شکست cache را با شکست source قاطی نکن

RedisConnectionException و RedisTimeoutException در خواندن/نوشتن cache باعث fallback می‌شوند؛ خطای command/server ناشی از قرارداد نادرست را مخفی نمی‌کنیم؛ خطای projection یا source پنهان نمی‌شود.
JSON خراب cache یک miss است. tenant دیگر کلید جدا دارد؛ شناسهٔ تیکت به‌تنهایی برای key کافی نیست.
tenant در این کنسول ثابت و مصنوعی است؛ در API واقعی باید از هویت معتبر بیاید و ownership پیش از پاسخ cache اعمال شود.
این نمونه single-flight، stampede protection، توزیع بار replica یا تضمین cache مشترک در چند region ندارد.

## تمرین و معیار عبور

1. تست TTL حافظه‌ای را بخوان؛ clock جلو می‌رود و هیچ انتظار واقعی لازم نیست.
2. حذف کلید قدیمی را عمداً ناکام کن؛ read تازه باید نسخهٔ تازه را برگرداند.
3. بنویس اگر DB از backup قدیمی برگشت، چرا استفادهٔ دوباره از revision و namespace قبلی خطرناک است.
4. برای ورود هم‌زمان چند miss، تعداد projectionها را بشمار؛ single-flight را فقط پس از دیدن هزینه طراحی کن.

برای عبور، cache hit، invalidation race، جداسازی tenant و fallback را با شاهد توضیح بده؛ از «cache همیشه درست است» استفاده نکن.

## منابع رسمی

- [StackExchange.Redis basics](https://stackexchange.github.io/StackExchange.Redis/Basics.html)
- [نسخهٔ مرجع client 2.13.17](https://www.nuget.org/packages/StackExchange.Redis/2.13.17)
- [Testcontainers Redis](https://dotnet.testcontainers.org/modules/redis/)
- [Redis eviction](https://redis.io/docs/latest/develop/reference/eviction/)

[قبلی](../082-observability/README.md) · [بعدی](../084-jobs/README.md) · [فهرست بخش](../README.md)
