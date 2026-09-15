CREATE TABLE IF NOT EXISTS Filin_query
(
    query_key text NOT NULL,
    query_text text NOT NULL
);

-- Table: projections

-- DROP TABLE IF EXISTS projections;

CREATE TABLE IF NOT EXISTS projections
(
    id_hash bigint NOT NULL,
    id_name text COLLATE pg_catalog."C.utf8",
    type_hash bigint NOT NULL,
    type_name text COLLATE pg_catalog."C.utf8" NOT NULL,
    "position" bigint NOT NULL,
    timestamp_utc timestamp NOT NULL,
    payload bytea
);

-- Index: ix_projections

-- DROP INDEX IF EXISTS ix_projections;

CREATE INDEX IF NOT EXISTS ix_projections
    ON projections USING btree
    (id_hash ASC NULLS LAST, type_hash ASC NULLS LAST)
    WITH (fillfactor=50, deduplicate_items=True);

INSERT INTO Filin_query
    VALUES ('CreateStorageKey',
    'INSERT INTO
        projections(id_hash, id_name, type_hash, type_name, position, timestamp_utc, payload)
    SELECT
        @id_hash,
        @id_name,
        @type_hash,
        @type_name,
        @position,
        @timestamp_utc,
        @payload
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM
            projections
        WHERE
            id_hash = @id_hash
            AND type_hash = @type_hash
            AND (@id_name IS NOT NULL AND id_name = @id_name OR @id_name IS NULL AND id_name IS NULL)
            AND type_name = @type_name
    )');

INSERT INTO Filin_query
    VALUES ('ReadStorageKey',
    'SELECT
        position,
        timestamp_utc,
        payload
    FROM
        projections
    WHERE
        id_hash = @id_hash
        AND type_hash = @type_hash
        AND (@id_name IS NOT NULL AND id_name = @id_name OR @id_name IS NULL AND id_name IS NULL)
        AND type_name = @type_name
        AND payload IS NOT NULL');

INSERT INTO Filin_query
    VALUES ('UpdateStorageKey',
    'UPDATE
        projections
    SET
        position = @position,
        timestamp_utc = @timestamp_utc,
        payload = @payload       
    WHERE
        id_hash = @id_hash
        AND type_hash = @type_hash
        AND (@id_name IS NOT NULL AND id_name = @id_name OR @id_name IS NULL AND id_name IS NULL)
        AND type_name = @type_name
        AND (position < @position OR position <= @position AND payload IS NULL)');

INSERT INTO Filin_query
    VALUES ('ClearStorageKey',
    'UPDATE
        projections
    SET
        payload = NULL       
    WHERE
        id_hash = @id_hash
        AND type_hash = @type_hash
        AND (@id_name IS NOT NULL AND id_name = @id_name OR @id_name IS NULL AND id_name IS NULL)
        AND type_name = @type_name
        AND payload IS NOT NULL
        AND position <= @position');
