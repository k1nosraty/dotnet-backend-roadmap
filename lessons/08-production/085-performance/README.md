# درس ۰۸۵ — سرعت را با قرارداد و بودجه بسنج

یک API مجموع مربع عددهای ۱ تا n را حساب می‌کند. یک نسخه loop دارد و نسخهٔ دیگر فرمول ریاضی.
می‌خواهیم اول ثابت کنیم نتیجه یکی است؛ بعد latency و خطا و مصرف process را بسنجیم.

## ۱. اول صحت، بعد سرعت

از ریشهٔ مخزن:

```powershell
dotnet test tests/085-performance/SupportDesk.Tests.csproj
dotnet run -c Release --project samples/84-performance/SupportDesk.csproj --urls http://127.0.0.1:5085
```

در ترمینال دیگر:

```powershell
curl.exe 'http://127.0.0.1:5085/sum/loop?count=3'
curl.exe 'http://127.0.0.1:5085/sum/formula?count=3'
```

هر دو از نظر داده پاسخ زیر را دارند:

```json
{"value":14}
```

فرمول n(n+1)(2n+1)/6 در Squares.cs با long محاسبه می‌شود. count فقط ۱ تا یک میلیون است تا هم overflow کنترل شود و هم هزینهٔ ورودی محدود بماند.
هشت تست نتیجهٔ دقیق، مرزها و قرارداد HTTP را نگه می‌دارند. نسخهٔ سریع با جواب اشتباه، بهینه‌سازی موفق نیست.

## ۲. بار محدود و قابل تکرار

Python 3 لازم است. در حالی که host در Release اجراست:

```powershell
python scripts/load_lab.py --algorithm loop --requests 64 --concurrency 4 --budget-p95-ms 200 --output artifacts/load-loop.json
python scripts/load_lab.py --algorithm formula --requests 64 --concurrency 4 --budget-p95-ms 200 --output artifacts/load-formula.json
```

مسیرهای output باید تازه باشند. اسکریپت فایل قبلی را overwrite نمی‌کند.
فقط URL پایهٔ HTTP روی loopback پذیرفته می‌شود و تعداد درخواست و concurrency محدود است؛ این ابزار را برای فشار روی سرویس دیگران طراحی نکرده‌ایم.
پنج درخواست warmup از آمار کنار می‌روند. زمان‌سنج با perf_counter کار می‌کند و هر درخواست timeout دارد.

## ۳. گزارش را درست بخوان

| فیلد | معنی |
| --- | --- |
| successful / failed | هم status و هم مقدار محاسبه بررسی می‌شود |
| errorRate | شکست تقسیم بر کل درخواست‌ها |
| latencyAllMs.p50 | صدک میانی زمان درخواست‌ها |
| latencyAllMs.p95 / p99 | بخش کندتر نمونه، شامل درخواست‌های ناموفق |
| successfulPerSecond | موفق‌ها تقسیم بر زمان کل آزمایش |
| processCpuMs | تغییر CPU time کل process میزبان |
| allocatedBytes | تغییر تخصیص حافظهٔ کل process |

صدک‌ها با nearest-rank حساب می‌شوند: داده‌ها مرتب و عضو ceil(p*N) انتخاب می‌شود.
میانگین به‌تنهایی tail latency را نشان نمی‌دهد. تعداد نمونهٔ ۶۴ برای نتیجه‌گیری دربارهٔ p99 محصول کافی نیست؛ برای شناخت ابزار کوچک انتخاب شده است.
CPU و allocation اینجا از /runtime می‌آیند و شامل overhead آزمایش و process هستند؛ اندازه‌گیری اختصاصی یک متد نیستند.
این endpoint مدیریتی فقط برای دادهٔ مصنوعی روی loopback است؛ آن را به API عمومی محصول کپی نکن.

## ۴. بودجه یعنی شرط خروج

بودجهٔ پیش‌فرض: p95 حداکثر ۲۰۰ میلی‌ثانیه و errorRate صفر.
اگر هر شرط رد شود، برنامه با exit code غیر صفر خارج می‌شود. آمار شکست‌ها حذف نمی‌شود تا نتیجه ظاهراً بهتر شود.
در CI برای probe کوچک از بودجهٔ ۵۰۰۰ میلی‌ثانیه استفاده می‌کنیم تا نوسان runner، تست صحت را ناپایدار نکند؛ این عدد SLA محصول نیست.

فرمول احتمالاً CPU کمتری می‌خواهد، اما در نمونهٔ کوچک ممکن است زمان شبکه، JIT، GC و scheduler بر p95 غالب باشند.
آزمایش را چند بار با ترتیب جابه‌جا و شرایط یکسان انجام بده؛ نتیجهٔ یک اجرای کم‌نمونه را قانون عمومی نکن.

## ۵. محدودیت مولد بار

این مولد closed-loop است: هر worker بعد از پایان درخواست قبلی، درخواست بعدی را می‌فرستد.
در کندی سیستم، نرخ ارسال خودش کم می‌شود؛ مشکل coordinated omission و بار واقعیِ arrival-rate ثابت را پوشش نمی‌دهد.
برای آزمون ظرفیت محصول، توزیع ورودی واقعی، مدت کافی، ramp، بودجهٔ خطا و مولد بار مناسب لازم است؛ ابزار آموزشی ما جای آن‌ها نیست.

## ۶. از اندازه‌گیری کلی به profile

اگر CPU بالا بود، قبل از تغییر معماری، hot path را پیدا کن. برای مشاهدهٔ دستی اختیاری:

```powershell
dotnet tool install --tool-path artifacts/tools dotnet-counters
dotnet tool install --tool-path artifacts/tools dotnet-trace
.rtifacts	ools\dotnet-counters.exe ps
.rtifacts	ools\dotnet-counters.exe monitor --process-id 12345 --counters System.Runtime
.rtifacts	ools\dotnet-trace.exe collect --process-id 12345 --duration 00:00:15 --output artifacts/worker.nettrace
```

12345 نمونه است؛ PID میزبان خودت را از ps بگیر. در Linux پسوند exe و شکل مسیر Windows را عوض کن.
نسخهٔ ابزار نصب‌شده و SDK و ماشین را در گزارش بنویس. این دو ابزار اختیاری در CI این درس نصب/اجرا نمی‌شوند؛ آنجا CPU/allocation سادهٔ process و قرارداد load بررسی می‌شود.
هنگام trace بار ایجاد کن، فایل را در ابزار سازگار باز کن و سهم Squares.Loop را با نسخهٔ فرمول مقایسه کن.
trace/dump ممکن است دادهٔ حساس داشته باشد؛ اینجا فقط host مصنوعی خودت را profile کن.

## تمرین و معیار عبور

1. بودجه را عمداً خیلی کم بگذار و exit code شکست را ببین؛ correctness testها همچنان باید pass شوند.
2. دو گزارش قابل مقایسه نگه دار و بنویس کدام تفاوت از CPU دیده می‌شود و کدام ممکن است نویز باشد.
3. یک ورودی بزرگ‌تر از حد بفرست؛ باید پیش از loop رد شود.
4. فرق regression test، load test و profiler را با یک مثال توضیح بده.

برای عبور، گزارش شامل محیط، workload، warmup، تعداد نمونه، خطا و p95 باشد؛ صرف نوشتن «خیلی سریع‌تر شد» کافی نیست.

## منابع رسمی

- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
- [Diagnostics overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)

[قبلی](../084-jobs/README.md) · [بعدی](../086-operations/README.md) · [فهرست بخش](../README.md)
