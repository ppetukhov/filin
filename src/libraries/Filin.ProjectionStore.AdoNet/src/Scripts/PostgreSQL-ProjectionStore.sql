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

CREATE OR REPLACE FUNCTION upsert_projection(
    p_id_hash bigint,
    p_id_name text,
    p_type_hash bigint,
    p_type_name text,
    p_position bigint,
    p_timestamp_utc timestamp,
    p_payload bytea
)
RETURNS VOID AS $$
BEGIN
    UPDATE projections
    SET
        position = p_position,
        timestamp_utc = p_timestamp_utc,
        payload = p_payload
    WHERE
        id_hash = p_id_hash
        AND type_hash = p_type_hash
        AND id_name IS NOT DISTINCT FROM p_id_name
        AND type_name = p_type_name
        AND (position < p_position OR (position <= p_position AND payload IS NULL));

    IF FOUND THEN
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM projections
        WHERE id_hash = p_id_hash
          AND type_hash = p_type_hash
          AND id_name IS NOT DISTINCT FROM p_id_name
          AND type_name = p_type_name
    ) THEN
        INSERT INTO projections (id_hash, id_name, type_hash, type_name, position, timestamp_utc, payload)
        VALUES (p_id_hash, p_id_name, p_type_hash, p_type_name, p_position, p_timestamp_utc, p_payload);
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION upsert_projections_batch(
    p_id_hashes bigint[],
    p_id_names text[],
    p_type_hashes bigint[],
    p_type_names text[],
    p_positions bigint[],
    p_timestamps_utc timestamp[],
    p_payloads bytea[]
)
RETURNS VOID AS $$
BEGIN
    UPDATE projections p
    SET
        position = d.new_position,
        timestamp_utc = d.new_timestamp_utc,
        payload = d.new_payload
    FROM (
        SELECT
            unnest(p_id_hashes) as id_hash,
            unnest(p_id_names) as id_name,
            unnest(p_type_hashes) as type_hash,
            unnest(p_type_names) as type_name,
            unnest(p_positions) as new_position,
            unnest(p_timestamps_utc) as new_timestamp_utc,
            unnest(p_payloads) as new_payload
    ) d
    WHERE p.id_hash = d.id_hash
      AND p.type_hash = d.type_hash
      AND p.id_name IS NOT DISTINCT FROM d.id_name
      AND p.type_name = d.type_name
      AND (p.position < d.new_position OR (p.position <= d.new_position AND p.payload IS NULL));

    INSERT INTO projections (id_hash, id_name, type_hash, type_name, position, timestamp_utc, payload)
    SELECT
        d.id_hash, d.id_name, d.type_hash, d.type_name, d.new_position, d.new_timestamp_utc, d.new_payload
    FROM (
        SELECT
            unnest(p_id_hashes) as id_hash,
            unnest(p_id_names) as id_name,
            unnest(p_type_hashes) as type_hash,
            unnest(p_type_names) as type_name,
            unnest(p_positions) as new_position,
            unnest(p_timestamps_utc) as new_timestamp_utc,
            unnest(p_payloads) as new_payload
    ) d
    WHERE NOT EXISTS (
        SELECT 1
        FROM projections p
        WHERE p.id_hash = d.id_hash
          AND p.type_hash = d.type_hash
          AND p.id_name IS NOT DISTINCT FROM d.id_name
          AND p.type_name = d.type_name
    );
END;
$$ LANGUAGE plpgsql;

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
    VALUES ('UpsertStorageKey',
    'SELECT upsert_projection(
        @id_hash::bigint,
        @id_name::text,
        @type_hash::bigint,
        @type_name::text,
        @position::bigint,
        @timestamp_utc::timestamp,
        @payload::bytea)');

INSERT INTO Filin_query
    VALUES ('UpsertBatchStorageKey',
    'SELECT upsert_projections_batch(
        ARRAY[@id_hash]::bigint[],
        ARRAY[@id_name]::text[],
        ARRAY[@type_hash]::bigint[],
        ARRAY[@type_name]::text[],
        ARRAY[@position]::bigint[],
        ARRAY[@timestamp_utc]::timestamp[],
        ARRAY[@payload]::bytea[])');

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
