# ADR 001 — جهت وابستگی و مرز assembly

## وضعیت
پذیرفته‌شده برای نسخهٔ آموزشی معماری؛ تاریخ 2026-10-17.

## زمینه
قواعد Ticket باید مستقل از endpoint، serializer و provider قابل بررسی باشند. پوشه و namespace به‌تنهایی این مرز را محدود نمی‌کنند.

## تصمیم
Domain reference پروژه و package خارجی ندارد. Application فقط به Domain وابسته است؛ FluentValidation از درس ۵۸ در Application pin شده است. Infrastructure قراردادهای Application و مدل Domain را پیاده می‌کند. host برای composition هر سه را می‌شناسد. adapterها internal هستند.

## جایگزین‌ها
یک project با convention ساده‌تر است ولی enforcement کمتری دارد. یک project برای هر قابلیت، برای اندازهٔ فعلی هزینهٔ زیادی دارد. microservice نیز مسئلهٔ فعلی نیست.

## پیامدها
ساخت و جابه‌جایی فایل‌ها تنظیم بیشتر می‌خواهد. تست referenceهای مستقیم و assemblyهای کامپایل‌شده تخلف‌های مشخص را رد می‌کند؛ تضمین تحلیل همهٔ reflectionها یا تمام وابستگی‌های type-level نیست.

## شاهد و بازنگری
ArchitectureChecks.cs و scripts/architecture_final_smoke.py. اگر وابستگی تازه‌ای لازم شد، نیاز و اثر آن را در ADR بعدی ثبت کن؛ policy را صرفاً برای سبز شدن تست باز نکن.
