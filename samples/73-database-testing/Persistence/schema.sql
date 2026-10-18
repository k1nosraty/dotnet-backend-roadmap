CREATE TABLE tickets (
    id uuid PRIMARY KEY,
    owner_id text NOT NULL,
    title varchar(120) NOT NULL CHECK (char_length(title) BETWEEN 3 AND 120),
    status text NOT NULL CHECK (status IN ('Open', 'InProgress', 'Closed')),
    resolution varchar(200),
    version bigint NOT NULL CHECK (version > 0),
    CHECK ((status = 'Closed' AND resolution IS NOT NULL AND char_length(resolution) BETWEEN 3 AND 200)
        OR (status <> 'Closed' AND resolution IS NULL))
);
