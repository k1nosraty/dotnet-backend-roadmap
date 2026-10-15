SELECT t.id,t.title,count(n.id) AS note_count
FROM tickets t LEFT JOIN notes n ON n.ticket_id=t.id
GROUP BY t.id,t.title ORDER BY t.id;
SELECT t.id,t.title FROM tickets t WHERE EXISTS (SELECT 1 FROM notes n WHERE n.ticket_id=t.id) ORDER BY t.id;
SELECT t.id,t.title FROM tickets t LEFT JOIN notes n ON n.ticket_id=t.id WHERE n.id IS NULL ORDER BY t.id;
SELECT status,count(*) AS ticket_count FROM tickets GROUP BY status ORDER BY status;
