CREATE TABLE tickets (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY CHECK (id > 0),
    title text NOT NULL CHECK (title=btrim(title) AND char_length(title) BETWEEN 1 AND 120),
    priority text NOT NULL DEFAULT 'Normal' CHECK (priority IN ('Low','Normal','High')),
    status text NOT NULL DEFAULT 'Open' CHECK (status IN ('Open','InProgress','Closed')),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE notes (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY CHECK (id > 0),
    ticket_id integer NOT NULL REFERENCES tickets(id) ON DELETE RESTRICT,
    body text COLLATE "C" NOT NULL CHECK (body=btrim(body) AND char_length(body) BETWEEN 1 AND 500),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_notes_ticket_body UNIQUE (ticket_id,body)
);
CREATE INDEX ix_tickets_status_id ON tickets(status,id);
-- The unique index begins with ticket_id, so don't add the same single-column index blindly.
