DO $$
DECLARE rejected boolean;
BEGIN
    rejected := false;
    BEGIN INSERT INTO notes(ticket_id,body) VALUES (999,'Missing parent'); EXCEPTION WHEN foreign_key_violation THEN rejected:=true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Orphan accepted'; END IF;
    rejected := false;
    BEGIN INSERT INTO notes(ticket_id,body) VALUES (1,'Resolution recorded'); EXCEPTION WHEN unique_violation THEN rejected:=true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Duplicate accepted'; END IF;
    rejected := false;
    BEGIN DELETE FROM tickets WHERE id=1; EXCEPTION WHEN foreign_key_violation THEN rejected:=true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Restrict ignored'; END IF;
    rejected := false;
    BEGIN UPDATE tickets SET priority='Urgent'; EXCEPTION WHEN check_violation THEN rejected:=true; END;
    IF NOT rejected THEN RAISE EXCEPTION 'Unknown priority accepted'; END IF;
    IF (SELECT count(*) FROM tickets) <> 2 OR (SELECT count(*) FROM notes) <> 1 THEN RAISE EXCEPTION 'Rejected operation changed state'; END IF;
END $$;
