CREATE TABLE tickets (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY CHECK (id > 0),
    title text NOT NULL CHECK (title = btrim(title) AND char_length(title) BETWEEN 1 AND 120),
    status text NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'InProgress', 'Closed')),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE notes (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY CHECK (id > 0),
    ticket_id integer NOT NULL REFERENCES tickets(id) ON DELETE RESTRICT,
    body text NOT NULL CHECK (char_length(body) BETWEEN 1 AND 500)
);
