-- ==============================================================================
-- MODULE: 02_TABLES_AND_INDEXES
-- DESCRIPTION: Establishes the core relational schema, the gapless global counter,
--              declarative range partitioning, and compact numeric indexing.
--              Optimized for 4GB RAM hardware limits and alignment padding.
-- AUTHOR: Pavel Petukhov
-- DATE: 2026-09-13
-- ==============================================================================

-- 1. Singleton table to enforce strict monotonic sequence generation without values gaps
CREATE TABLE IF NOT EXISTS dcb_global_counter (
    counter_name text PRIMARY KEY,                    -- Unique sequence descriptor identifier (text type)
    current_position bigint NOT NULL                  -- Current absolute head sequence offset value
);

-- Idempotent baseline seeding to prevent overwriting active production state on re-runs
INSERT INTO dcb_global_counter (counter_name, current_position)
VALUES ('main_sequence', 0)
ON CONFLICT (counter_name) DO NOTHING;

-- 2. Master Append-Only event log table partitioned declaratively by position ranges
--    Column sequence is meticulously ordered by bytes footprint to eliminate CPU padding slots:
--    bigint (8) -> timestamp (8) -> integer (4) -> [System padding 4 bytes] -> pointers (8)
CREATE TABLE IF NOT EXISTS dcb_events (
    position bigint NOT NULL,                         -- Corrected absolute sequential event sequence ID (8 bytes)
    created_at timestamp without time zone NOT NULL DEFAULT NOW(), -- Automated transactional server timestamp execution (8 bytes)
    event_type_hash integer NOT NULL,                 -- Numeric partition routing and fast filtering key (4 bytes)
    event_type text NOT NULL,                         -- String type identifier used for exact RAM matching (ptr)
    tag_hashes bigint[] NOT NULL,                     -- Tokenized numeric array used for sub-microsecond GIN scans (ptr)
    tags text[] NOT NULL,                             -- String tag arrays used to fully eliminate hash collisions (ptr)
    payload json NOT NULL                             -- Non-parsed plain JSON string optimizing database write I/O (ptr)
) PARTITION BY RANGE (position);

-- 3. Idempotent initialization of the bootstrap partition partition layer (0 to 2,000,000 records)
--    Enforces fillfactor=100 to maximize page packaging density for read-heavy cold scans.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'dcb_events_p1') THEN
        CREATE TABLE dcb_events_p1 PARTITION OF dcb_events FOR VALUES FROM (0) TO (2000000);
        ALTER TABLE dcb_events_p1 SET (fillfactor = 100);
    END IF;
END $$;

-- 4. Compound composite indexing strategy providing sub-millisecond seek speeds within 4GB RAM
--    Composite B-Tree satisfies structural boundary lookups: hash matching combined with scale bounds.
CREATE INDEX IF NOT EXISTS idx_dcb_events_type_position_btree 
ON dcb_events USING btree (event_type_hash, position) 
WITH (fillfactor = 100);

-- Inverted index accelerating comprehensive array inclusion operations (@>) across billions of rows.
CREATE INDEX IF NOT EXISTS idx_dcb_events_tag_hashes_gin 
ON dcb_events USING gin(tag_hashes);
