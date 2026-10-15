INSERT INTO tickets(title,status)
SELECT 'Ticket ' || n, CASE WHEN n % 1000=0 THEN 'Open' ELSE 'Closed' END
FROM generate_series(1,100000) AS n;
ANALYZE tickets;
