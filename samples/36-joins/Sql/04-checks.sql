DO $$
BEGIN
    IF (SELECT count(*) FROM tickets t JOIN notes n ON n.ticket_id=t.id) <> 3 THEN RAISE EXCEPTION 'Inner join count'; END IF;
    IF (SELECT count(*) FROM tickets t LEFT JOIN notes n ON n.ticket_id=t.id) <> 4 THEN RAISE EXCEPTION 'Fan-out count'; END IF;
    IF (SELECT count(n.id) FROM tickets t LEFT JOIN notes n ON n.ticket_id=t.id WHERE t.id=3) <> 0 THEN RAISE EXCEPTION 'Empty child count'; END IF;
    IF (SELECT count(*) FROM tickets t WHERE NOT EXISTS(SELECT 1 FROM notes n WHERE n.ticket_id=t.id)) <> 1 THEN RAISE EXCEPTION 'Missing child count'; END IF;
END $$;
