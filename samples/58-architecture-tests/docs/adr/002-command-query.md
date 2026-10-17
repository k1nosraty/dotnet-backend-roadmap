# ADR 002 — خواندن و نوشتن در یک storage

## وضعیت
پذیرفته‌شده؛ تاریخ 2026-10-17.

## زمینه
خواندن فهرست به aggregate قابل ویرایش نیاز ندارد. pagination و فیلتر باید خروجی معلوم و materialize شده داشته باشند.

## تصمیم
ITicketQueries از ITicketStore جداست. DTO خلاصه و TicketPage از port خواندن برمی‌گردند. دو port به همان storage در همان process وصل‌اند. mediator، event bus و دیتابیس خواندن مستقل نداریم.

## جایگزین‌ها
یک interface بزرگ ساده‌تر شروع می‌شود ولی نیازهای متفاوت را مخلوط می‌کند. دیتابیس خواندن جدا، نیاز به هماهنگی و بررسی lag دارد و فعلاً توجیه ندارد.

## پیامدها
query projection و query handler داریم، اما transaction توزیع‌شده نداریم. query state یا Version را تغییر نمی‌دهد. count و صفحه در همین adapter زیر یک lock محاسبه می‌شوند؛ consistency به provider آینده قابل تعمیم خودکار نیست.

## شاهد و بازنگری
QueryChecks و GETهای اسکریپت نهایی. اگر latency و حجم گزارش به مشکل واقعی تبدیل شد، اندازه‌گیری را قبل از تغییر topology ثبت کن.
