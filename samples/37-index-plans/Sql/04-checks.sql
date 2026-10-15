DO $$
BEGIN
    IF (SELECT count(*) FROM tickets) <> 100000 THEN RAISE EXCEPTION 'Fixture count'; END IF;
    IF (SELECT count(*) FROM tickets WHERE status='Open') <> 100 THEN RAISE EXCEPTION 'Selectivity'; END IF;
    IF to_regclass('ix_tickets_status_id') IS NULL THEN RAISE EXCEPTION 'Missing index'; END IF;
    IF (SELECT array_agg(id ORDER BY id) FROM (SELECT id FROM tickets WHERE status='Open' ORDER BY id LIMIT 3) q) <> ARRAY[1000,2000,3000] THEN RAISE EXCEPTION 'Result changed'; END IF;
END $$;
