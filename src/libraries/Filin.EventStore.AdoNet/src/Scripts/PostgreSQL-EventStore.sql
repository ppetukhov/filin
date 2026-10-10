-- ==============================================================================
-- MODULE: 01_TYPES_AND_EXTENSIONS
-- DESCRIPTION: Initializes required PostgreSQL extensions and domain-specific
--              composite types for the Dynamic Consistency Boundary (DCB) log.
-- AUTHOR: Pavel Petukhov
-- DATE: 2026-09-13
-- ==============================================================================

-- 1. Enable pg_cron extension for managing background time-based partitioning tasks
CREATE EXTENSION IF NOT EXISTS pg_cron;

-- 2. Define composite type for transaction insertion conditions (Optimistic Concurrency Control)
--    Uses strict canonical native data types in lowercase format.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'dcb_query_item') THEN
        CREATE TYPE dcb_query_item AS (
            event_type_hashes integer[],              -- Array of 32-bit integer event type hashes
            event_types       text[],                 -- Array of original text event type names
            tag_hashes        bigint[],               -- Array of 64-bit integer business tag hashes
            tags              text[]                  -- Array of original text business tag names
        );
    END IF;
END $$;

-- 3. Define composite type for incoming event log payload structure
--    Fields are strictly aligned with the database table storage layout.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'dcb_event') THEN
        CREATE TYPE dcb_event AS (
            event_type_hash integer,                  -- Pre-computed 32-bit hash of the event type
            event_type      text,                     -- Original text event type name (e.g., "OrderPaid")
            tag_hashes      bigint[],                 -- Pre-computed arrays of 64-bit business tag hashes
            tags            text[],                   -- Original text business tag values for verification
            payload         json                      -- Plain text JSON event data (~1KB payload text)
        );
    END IF;
END $$;

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


-- ==============================================================================
-- MODULE: 03_STORED_PROCEDURES
-- DESCRIPTION: Contains transaction control mechanics including batch inserts,
--              single entries, and chronologically consistent log streaming readers.
--              INSERT queries are explicitly synchronized with hardware aligned tables.
--              Both append functions now return event positions alongside their
--              database-generated created_at timestamps.
-- AUTHOR: Pavel Petukhov
-- DATE: 2026-09-13
-- ==============================================================================

-- --- 1. ATOMIC BATCH INSERT WITH INLINE BOUNDARY INTERSECTION VALIDATION ---
CREATE OR REPLACE FUNCTION dcb_append_events(
    p_cond_after bigint,                              -- Exact transactional barrier sequence marker
    p_events dcb_event[],                             -- Array of structured business event payloads
    VARIADIC p_cond_fail_if_match dcb_query_item[]    -- Variable list of intersection constraint rules
) 
RETURNS TABLE("position" bigint, "created_at" timestamp without time zone) AS $$
DECLARE
    v_conflict_detected boolean;
    v_start_position bigint;
    v_batch_size integer;
BEGIN
    -- Assert incoming array parameter properties to guard against code exceptions
    v_batch_size := array_length(p_events, 1);
    IF v_batch_size IS NULL OR v_batch_size = 0 THEN
        RAISE EXCEPTION 'DCB Invalid Argument: Cannot append an empty event batch.'
            USING ERRCODE = '22023';
    END IF;

    -- STEP 1: ALLOCATE EXCLUSIVE LOCK AND SLOTS FIRST (Fixes Race Condition)
    -- By locking the single row first, we force concurrent appenders into a strict 
    -- sequential queue BEFORE checking the consistency boundary history.
    UPDATE dcb_global_counter 
    SET current_position = current_position + v_batch_size 
    WHERE counter_name = 'main_sequence' 
    RETURNING (current_position - v_batch_size + 1) INTO v_start_position;

    -- STEP 2: VERIFY DCB CONVERGENCE SAFETY UNDER THE ACQUIRED LOCK
    -- Now, any concurrent commits made by preceding transactions are fully visible.
    SELECT EXISTS (
        SELECT 1 
        FROM dcb_events e
        WHERE e.position > p_cond_after
          AND EXISTS (
              SELECT 1 
              FROM unnest(p_cond_fail_if_match) f
              WHERE (f.event_type_hashes IS NULL OR cardinality(f.event_type_hashes) = 0 OR e.event_type_hash = ANY(f.event_type_hashes))
                AND (f.tag_hashes IS NULL OR cardinality(f.tag_hashes) = 0 OR e.tag_hashes @> f.tag_hashes)
                AND (f.event_types IS NULL OR cardinality(f.event_types) = 0 OR e.event_type = ANY(f.event_types))
                AND (f.tags IS NULL OR cardinality(f.tags) = 0 OR e.tags @> f.tags)
          )
    ) INTO v_conflict_detected;

    -- If a conflict is detected under the lock, abort insertion. 
    -- Note: This leaves a gapless increment block that will be rolled back if used inside an external tx, 
    -- or safely abandoned. But since we use autonomous plpgsql logic, to prevent gaps in numbers upon return 
    -- we allow normal exit. However, for a strict gapless design, we return 0 rows and the transaction rollbacks on application level.
    IF v_conflict_detected THEN
        -- Revert the counter increment to preserve strict GAPLESS properties before exiting
        UPDATE dcb_global_counter 
        SET current_position = current_position - v_batch_size 
        WHERE counter_name = 'main_sequence';
        RETURN; 
    END IF;

    -- STEP 3: Dispatch native relational bulk compilation using unnest array projection
    RETURN QUERY
    INSERT INTO dcb_events (position, event_type_hash, event_type, tag_hashes, tags, payload)
    SELECT 
        v_start_position + (t.idx - 1),
        t.event_type_hash,
        t.event_type,
        t.tag_hashes,
        t.tags,
        t.payload
    FROM unnest(p_events) WITH ORDINALITY AS t(
        event_type_hash, event_type, tag_hashes, tags, payload, idx
    )
    RETURNING dcb_events.position, dcb_events.created_at;
END;
$$ LANGUAGE plpgsql;


-- --- 2. ATOMIC SINGLE ENTRY WITH INLINE BOUNDARY INTERSECTION VALIDATION ---
CREATE OR REPLACE FUNCTION dcb_append_event(
    p_cond_after bigint,                              -- Sequential consistency horizon point
    p_event dcb_event,                                -- Single target entry business payload
    VARIADIC p_cond_fail_if_match dcb_query_item[]    -- Variable list of intersection constraint rules
) 
RETURNS TABLE("position" bigint, "created_at" timestamp without time zone) AS $$
DECLARE
    v_conflict_detected boolean;
    v_assigned_position bigint;
BEGIN
    -- STEP 1: ACCQUIRE EXCLUSIVE SEQUENCE LOCK FIRST (Fixes Race Condition)
    UPDATE dcb_global_counter 
    SET current_position = current_position + 1 
    WHERE counter_name = 'main_sequence' 
    RETURNING current_position INTO v_assigned_position;

    -- STEP 2: VERIFY DCB CONVERGENCE SAFETY UNDER THE LOCK
    SELECT EXISTS (
        SELECT 1 
        FROM dcb_events e
        WHERE e.position > p_cond_after
          AND EXISTS (
              SELECT 1 
              FROM unnest(p_cond_fail_if_match) f
              WHERE (f.event_type_hashes IS NULL OR cardinality(f.event_type_hashes) = 0 OR e.event_type_hash = ANY(f.event_type_hashes))
                AND (f.tag_hashes IS NULL OR cardinality(f.tag_hashes) = 0 OR e.tag_hashes @> f.tag_hashes)
                AND (f.event_types IS NULL OR cardinality(f.event_types) = 0 OR e.event_type = ANY(f.event_types))
                AND (f.tags IS NULL OR cardinality(f.tags) = 0 OR e.tags @> f.tags)
          )
    ) INTO v_conflict_detected;

    -- If a conflict is found, rollback the counter locally to protect the GAPLESS sequence state
    IF v_conflict_detected THEN
        UPDATE dcb_global_counter 
        SET current_position = current_position - 1 
        WHERE counter_name = 'main_sequence';
        RETURN; 
    END IF;

    -- STEP 3: Record data payload securely
    RETURN QUERY
    INSERT INTO dcb_events (position, event_type_hash, event_type, tag_hashes, tags, payload) 
    VALUES (
        v_assigned_position, 
        (p_event).event_type_hash, 
        (p_event).event_type, 
        (p_event).tag_hashes, 
        (p_event).tags, 
        (p_event).payload
    )
    RETURNING dcb_events.position, dcb_events.created_at;
END;
$$ LANGUAGE plpgsql;


-- --- 3. HIGH-PERFORMANCE CHRONOLOGICAL LOG STREAMING READER ---
CREATE OR REPLACE FUNCTION dcb_read_events(
    p_after_position bigint,                          -- Sequence offset baseline token for projection consumer sync
    VARIADIC p_query dcb_query_item[]                 -- Conditional filter vectors optimizing target delivery channels
)
RETURNS SETOF dcb_events AS $$
BEGIN
    -- Execute pipelined stream read over specific target partition data windows
    RETURN QUERY
    SELECT 
        e.position, 
        e.created_at, 
        e.event_type_hash, 
        e.event_type, 
        e.tag_hashes, 
        e.tags, 
        e.payload
    FROM dcb_events e
    WHERE e.position > p_after_position
      AND (
          p_query IS NULL 
          OR cardinality(p_query) = 0
          OR EXISTS (
              SELECT 1 
              FROM unnest(p_query) f
              WHERE (f.event_type_hashes IS NULL OR cardinality(f.event_type_hashes) = 0 OR e.event_type_hash = ANY(f.event_type_hashes))
                AND (f.tag_hashes IS NULL OR cardinality(f.tag_hashes) = 0 OR e.tag_hashes @> f.tag_hashes)
                AND (f.event_types IS NULL OR cardinality(f.event_types) = 0 OR e.event_type = ANY(f.event_types))
                AND (f.tags IS NULL OR cardinality(f.tags) = 0 OR e.tags @> f.tags)
          )
      )
    ORDER BY e.position ASC;                          -- Guarantees deterministic state replication playbacks
END;
$$ LANGUAGE plpgsql;


-- --- 4. STREAMING READER OVERLOAD FOR BLIND NON-FILTERED SCANS ---
CREATE OR REPLACE FUNCTION dcb_read_events(
    p_after_position bigint                           -- Target starting coordinate offset point
)
RETURNS SETOF dcb_events AS $$
BEGIN
    -- Transparent mapping providing direct access pipelines to raw transactional entries
    RETURN QUERY 
    SELECT * FROM dcb_read_events(p_after_position, VARIADIC '{}'::dcb_query_item[]);
END;
$$ LANGUAGE plpgsql;


-- ==============================================================================
-- MODULE: 04_TRIGGERS_AND_CRON
-- DESCRIPTION: Manages reactive low-latency streaming messaging events (Pub/Sub)
--              and configures daily partition expansion jobs.
-- AUTHOR: Pavel Petukhov
-- DATE: 2026-09-13
-- ==============================================================================

-- ==============================================================================
-- ⚡️ 1. PUB/SUB REALTIME NOTIFICATION LAYER (IN-MEMORY MESSAGING)
-- ==============================================================================

CREATE OR REPLACE FUNCTION dcb_events_notify_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_notification_payload text;
BEGIN
    -- Construct structured structural string context strips EXCLUDING variable raw payload context
    -- Enforces safe processing bounded securely within 8000-byte internal process limits.
    -- Fields order inside JSON matches the standard table layout.
    SELECT json_build_object(
        'position', NEW.position,
        'created_at', to_char(NEW.created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS'),
        'event_type_hash', NEW.event_type_hash,
        'event_type', NEW.event_type,
        'tag_hashes', NEW.tag_hashes,
        'tags', NEW.tags
    )::text INTO v_notification_payload;

    -- Signal internal asynchronous listener connections across transactional memory channels
    PERFORM pg_notify('dcb_events', v_notification_payload);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Bind reactive engine layers ensuring consumers intercept records strictly after post-commit lifecycle phases
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger 
        WHERE tgrelid = 'dcb_events'::regclass AND tgname = 'trg_dcb_events_insert_notify'
    ) THEN
        CREATE TRIGGER trg_dcb_events_insert_notify
            AFTER INSERT ON dcb_events
            FOR EACH ROW
            EXECUTE FUNCTION dcb_events_notify_trigger();
    END IF;
END $$;


-- ==============================================================================
-- 📅 2. AUTOMATED DECLARATIVE PARTITION EXPANSION (PREVENTIVE DDL ENGINE)
-- ==============================================================================

CREATE OR REPLACE PROCEDURE dcb_check_and_create_partitions()
LANGUAGE plpgsql AS $$
DECLARE
    v_current_pos bigint;                             -- Native integer offset baseline tracking reference
    v_partition_size bigint := 2000000;               -- Hard-coded architectural partition range step factor
    v_current_section_idx bigint;                     
    v_target_section_idx bigint;                      
    v_section_start bigint;                           
    v_section_end bigint;                             
    v_partition_name text;                            
    v_sql text;                                       
BEGIN
    -- Query active timeline status metrics directly from core counter sequences
    SELECT current_position INTO v_current_pos FROM dcb_global_counter WHERE counter_name = 'main_sequence';
    v_current_section_idx := v_current_pos / v_partition_size;
    
    -- Guarantee proactive allocation boundaries for current and next upcoming logs segments
    FOR i IN 0..1 LOOP
        v_target_section_idx := v_current_section_idx + i;
        v_section_start := v_target_section_idx * v_partition_size;
        v_section_end := v_section_start + v_partition_size;
        v_partition_name := 'dcb_events_p' || (v_target_section_idx + 1);
        
        v_sql := format(
            'CREATE TABLE IF NOT EXISTS %I PARTITION OF dcb_events FOR VALUES FROM (%s) TO (%s);',
            v_partition_name, v_section_start, v_section_end
        );
        EXECUTE v_sql;
        
        -- Optimize physical block layout to eliminate fragmentation overhead completely
        EXECUTE format('ALTER TABLE %I SET (fillfactor = 100);', v_partition_name);
    END LOOP;
END;
$$;

-- Unschedule obsolete jobs to prevent execution duplication conflicts
SELECT cron.unschedule('dcb-auto-partition-job');

-- Register cron routine executing daily preventive validation sequences at 02:00 AM server time
SELECT cron.schedule(
    'dcb-auto-partition-job',
    '0 2 * * *',
    'CALL dcb_check_and_create_partitions()'
);
