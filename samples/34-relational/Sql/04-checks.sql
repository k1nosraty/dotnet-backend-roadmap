DO $$
DECLARE rejected boolean;
BEGIN
    IF (SELECT count(*) FROM tickets) <> 2 THEN RAISE EXCEPTION 'Wrong seed count'; END IF;
    rejected := false;
    BEGIN INSERT INTO tickets(title) VALUES (NULL); EXCEPTION WHEN not_null_violation THEN rejected := true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'NULL accepted'; END IF;
    rejected := false;
    BEGIN INSERT INTO tickets(title) VALUES (' '); EXCEPTION WHEN check_violation THEN rejected := true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Empty title accepted'; END IF;
    rejected := false;
    BEGIN UPDATE tickets SET status='Unknown'; EXCEPTION WHEN check_violation THEN rejected := true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Unknown status accepted'; END IF;
    rejected := false;
    BEGIN INSERT INTO tickets(id,title) OVERRIDING SYSTEM VALUE VALUES (1,'Duplicate'); EXCEPTION WHEN unique_violation THEN rejected := true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Duplicate key accepted'; END IF;
    IF (SELECT count(*) FROM tickets) <> 2 THEN RAISE EXCEPTION 'Rejected input changed rows'; END IF;
END $$;
