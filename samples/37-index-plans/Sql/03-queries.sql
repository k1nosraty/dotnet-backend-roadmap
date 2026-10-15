EXPLAIN (ANALYZE, BUFFERS) SELECT id,title FROM tickets WHERE status='Open' ORDER BY id LIMIT 20;
CREATE INDEX ix_tickets_status_id ON tickets(status,id);
ANALYZE tickets;
EXPLAIN (ANALYZE, BUFFERS) SELECT id,title FROM tickets WHERE status='Open' ORDER BY id LIMIT 20;
EXPLAIN (ANALYZE, BUFFERS) SELECT id,title FROM tickets WHERE status='Closed';
