SELECT t.id,t.title,t.priority,t.status,count(n.id) AS note_count
FROM tickets t LEFT JOIN notes n ON n.ticket_id=t.id GROUP BY t.id ORDER BY t.id;
SELECT t.id,t.title,n.id AS note_id,n.body,n.created_at
FROM tickets t JOIN notes n ON n.ticket_id=t.id ORDER BY t.id,n.id;
