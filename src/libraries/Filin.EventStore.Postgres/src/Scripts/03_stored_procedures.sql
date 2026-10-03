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
