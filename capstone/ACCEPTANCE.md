# پذیرش و ارزیابی پروژه

از ریشهٔ مخزن اجرا کن. .NET SDK 10 و Python 3 لازم‌اند؛ تست پایگاه داده و تحویل نهایی، Docker با Linux containers می‌خواهند. نبود Docker به معنی پاس‌شدن تست یکپارچه نیست.

```powershell
python scripts/check_content.py
python scripts/capstone_smoke.py
python scripts/capstone_smoke.py --docker-only
python scripts/capstone_delivery.py
```

| شاهد | محل واقعی | چیزی که اثبات می‌کند |
| --- | --- | --- |
| ۸ unit test | tests/087-contract/SupportDesk.Tests.csproj | قواعد Domain، موعد، version و یک reopen |
| ۲ تست HTTP | tests/088-prototype/SupportDesk.Tests.csproj | default deny، replay، مرز سازمان و رقابت حافظه |
| ۹ تست PostgreSQL | tests/089-capstone/SupportDesk.Tests.csproj | login واقعی، payload متفاوت، نقش/سازمان، لغو grant، CAS، rollback audit، restart و محدودیت نرخ |
| smoke | scripts/capstone_smoke.py | خروجی قرارداد، publish، نبود test DLL و رد config بدون نشت password |
| delivery | scripts/capstone_delivery.py | TLS معتبر، non-root، پورت خصوصی، جریان واقعی، restore و همان DLL |

تست audit یک interceptor فقط در test assembly دارد: ذخیرهٔ اول انجام می‌شود، ذخیرهٔ audit عمداً exception می‌دهد؛ پس از پاسخ 500، تعداد هر دو جدول باید صفر باشد. بنابراین صرفاً mock کردن نتیجهٔ یک repository نیست. هیچ endpoint عمومی برای تزریق خطا نداریم.

تست رقابتی create باید از شش درخواست یک 201 و پنج 200 با یک شناسه بسازد؛ تست close رقابتی فقط یک 200 و پنج 409 می‌خواهد. تست امنیت فقط status مثبت را نمی‌سنجد: peer و tenant دیگر حتی audit را هم نمی‌خوانند و JSON با tenant اضافه رد می‌شود. تست مجوز، flag کارشناس را در DB تغییر می‌دهد و با token قبلی 403 می‌گیرد.

## شاهد عملکرد

اسکریپت تحویل، ۲۴ خواندن احراز هویت‌شده با concurrency چهار، صحت لیست و error صفر را می‌سنجد. p95 باید زیر ۵۰۰۰ میلی‌ثانیه باشد؛ این فقط بودجهٔ گستردهٔ CI است، SLA محصول نیست. برای ادعای ظرفیت، روش warmup، مجموعه‌داده، مدت، arrival rate، CPU، allocation و error rate درس ۸۵ را روی سخت‌افزار مشخص تکرار کن. هرگز از ۲۴ درخواست ظرفیت تولید را نتیجه نگیر.

## نمره و شرط توقف

| معیار | امتیاز |
| --- | --- |
| قرارداد و مثال‌های مرزی | ۱۵ |
| قواعد و مسیر کامل مستقل | ۲۵ |
| هویت، سازمان و مجوز | ۲۰ |
| transaction و رقابت و تست منفی | ۲۰ |
| استقرار، restore و دفاع مستند | ۲۰ |

قبولی حداقل ۸۰ از ۱۰۰ است، با اجرای همهٔ شاهدهای لازم. دسترسی بین سازمان‌ها، secret در log، ذخیرهٔ نیمه‌کاره، یا restore ناموفق شرط توقف هستند و با امتیاز بخش دیگر جبران نمی‌شوند. دانشجو باید یک تغییر مستقل و شکست عمدی سپس رفع آن را نشان بدهد؛ سبز بودن نمونهٔ مرجع به‌تنهایی قبولی نیست.

گزارش تحویل را با commit، نسخهٔ SDK/Docker، فرمان، تعداد تست اجراشده/پاس/skip، نتیجهٔ restore و محدودیت‌های باقی‌مانده پر کن. خروجی pipeline را نگه دار؛ token و password و محتوای key ring را ضمیمه نکن.
