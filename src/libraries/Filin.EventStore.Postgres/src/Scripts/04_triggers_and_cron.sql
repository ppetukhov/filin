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
