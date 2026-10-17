# ADR 003 — validation صریح و حفظ قواعد Domain

## وضعیت
پذیرفته‌شده؛ FluentValidation 12.2.0، بررسی در 2026-10-17.

## زمینه
ResolveTicket به خطاهای فیلدمحور و قابلیت اجرای خارج از HTTP نیاز دارد. قاعدهٔ تیکت بسته به Domain تعلق دارد و نباید با اعتبارسنجی فرم جایگزین شود.

## تصمیم
فقط package اصلی FluentValidation در Application اضافه شده است. endpoint برای errors پاسخ، ValidateAsync را صریح اجرا می‌کند؛ use case نیز از همان validator برای محافظت از ورودی غیر HTTP استفاده می‌کند. قواعد این validator pure و بدون I/O هستند. package قدیمی auto-validation نصب نمی‌شود.

## جایگزین‌ها
guard دستی برای ورودی کوچک هنوز قابل دفاع است. DataAnnotations برای request ساده مناسب است. pipeline عمومی یا mediator در این اندازه ضرورتی ندارد.

## پیامدها
نوع خطاهای ورودی روشن می‌شود و یک dependency اضافه داریم. validator در HTTP و use case دوبار اجرا می‌شود ولی قانون ورودی فقط در یک کلاس تعریف شده؛ این هزینه در نمونهٔ کوچک پذیرفته شده است. قواعد async متصل به DB، نیاز به بازنگری این تصمیم دارند؛ uniqueness همچنان با constraint/commit تضمین می‌شود.

## شاهد و بازنگری
ValidationChecks، errors پاسخ HTTP و guardهای مستقل Ticket. اگر کتابخانه حذف شود، همان قرارداد ورودی و سناریوهای مرزی باید با implementation جدید حفظ شوند.
