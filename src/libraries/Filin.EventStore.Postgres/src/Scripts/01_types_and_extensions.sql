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
