-- Table: dcb_events

-- DROP TABLE IF EXISTS dcb_events;

CREATE TABLE IF NOT EXISTS dcb_events
(
    "position" bigint NOT NULL,
    timestamp_utc timestamp NOT NULL,
    type_hash integer NOT NULL,
    type_name varchar(30) COLLATE pg_catalog."C.utf8" NOT NULL,
    tag_hashes bigint[],
    tag_names varchar[] COLLATE pg_catalog."C.utf8",
	payload bytea
);

-- Index: ix_dcb_events_position

-- DROP INDEX IF EXISTS ix_dcb_events_position;

CREATE INDEX IF NOT EXISTS ix_dcb_events_position
    ON dcb_events USING brin
    ("position")
    WITH (pages_per_range=128, autosummarize=False);


-- FUNCTION: read_events(bigint, dcb_query_item[])

-- DROP FUNCTION IF EXISTS read_events(bigint, dcb_query_item[]);

CREATE OR REPLACE FUNCTION read_events(
	p_after_position bigint,
	VARIADIC p_fail_if_match dcb_query_item[])
	RETURNS TABLE (
	    "position" bigint,
	    timestamp_utc timestamp,
	    type_hash integer,
	    type_name varchar,
	    tag_hashes bigint[],
	    tag_names varchar[],
	    payload bytea
	)
    LANGUAGE 'plpgsql'
    COST 100
    IMMUTABLE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$DECLARE
	TEMPLATE_TYPES_HASHES CONSTANT text := 'e.type_hash = ANY($2[%1$s].type_hashes::integer[])';
	TEMPLATE_TAGS_HASHES CONSTANT text := 'e.tag_hashes @> $2[%1$s].tag_hashes::bigint[]';
	TEMPLATE_TYPES_NAMES CONSTANT text := 'e.type_name = ANY($2[%1$s].type_names::varchar[])';
	TEMPLATE_TAGS_NAMES CONSTANT text := 'e.tag_names @> $2[%1$s].tag_names::varchar[]';
	
	execute_statement text;
BEGIN
	SELECT 'SELECT * FROM dcb_events e WHERE e.position > $1 AND (' || string_agg(
				concat_ws(' AND ',
					(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_HASHES, i) ELSE 'type_hash IS NOT NULL' END),
					(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_HASHES, i) ELSE NULL END),
					(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_NAMES, i) ELSE NULL END),
					(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_NAMES, i) ELSE NULL END)
				),
				' OR '
			) || ') ORDER BY e.position'
	INTO STRICT execute_statement
	FROM (SELECT i, array_length(p_fail_if_match[i].type_hashes, 1) as types_count, array_length(p_fail_if_match[i].tag_hashes, 1) as tags_count FROM generate_series(1, array_length(p_fail_if_match, 1)) AS i);

	RETURN QUERY EXECUTE execute_statement
	USING p_after_position, p_fail_if_match;
END;$BODY$;

CREATE OR REPLACE FUNCTION append_event(
	p_event dcb_event_raw,
	p_after_position bigint,
	VARIADIC p_fail_if_match dcb_query_item[])
    RETURNS TABLE("position" bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1

AS $BODY$
DECLARE
	TEMPLATE_TYPES_HASHES CONSTANT text := 'e.type_hash = ANY($2[%1$s].type_hashes::integer[])';
	TEMPLATE_TAGS_HASHES CONSTANT text := 'e.tag_hashes @> $2[%1$s].tag_hashes::bigint[]';
	TEMPLATE_TYPES_NAMES CONSTANT text := 'e.type_name = ANY($2[%1$s].type_names::varchar[])';
	TEMPLATE_TAGS_NAMES CONSTANT text := 'e.tag_names @> $2[%1$s].tag_names::varchar[]';

	ins_statement text;

	MAX_LOCK_TIME_OUT_SEC CONSTANT real := 15;
	cur_wait_sec real := 0;
	total_wait_sec real := 0;
BEGIN
	ins_statement := 'INSERT INTO dcb_events("position", timestamp_utc, type_hash, type_name, tag_hashes, tag_names, payload) '
	   'SELECT 0::bigint, $3.timestamp_utc::timestamp, $3.type_hash::integer, $3.type_name::varchar, $3.tag_hashes::bigint[], $3.tag_names::varchar[], $3.payload::bytea '
	   'WHERE NOT EXISTS (SELECT 1 FROM dcb_events e WHERE e.position > $1 AND (' || string_agg(
			concat_ws(' AND ',
				(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_HASHES, i) ELSE 'type_hash IS NOT NULL' END),
				(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_HASHES, i) ELSE NULL END),
				(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_NAMES, i) ELSE NULL END),
				(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_NAMES, i) ELSE NULL END)
			),
			' OR '
		) || '))'
		' RETURNING position'
		FROM (SELECT i, array_length(p_fail_if_match[i].type_hashes, 1) as types_count, array_length(p_fail_if_match[i].tag_hashes, 1) as tags_count FROM generate_series(1, array_length(p_fail_if_match, 1)) AS i);
	
	WHILE total_wait_sec < MAX_LOCK_TIME_OUT_SEC AND NOT pg_try_advisory_xact_lock(101010101) LOOP
		cur_wait_sec := random() * 0.015;
		total_wait_sec := total_wait_sec + cur_wait_sec;
		PERFORM pg_sleep(cur_wait_sec);
	END LOOP;

	IF total_wait_sec >= MAX_LOCK_TIME_OUT_SEC THEN
		RAISE EXCEPTION 'Time out lock event store.'
		USING ERRCODE = 'DCB01', 
		HINT = 'System busy. Please retry later.';
	END IF;

	RETURN QUERY EXECUTE ins_statement USING p_after_position, p_fail_if_match, p_event;
END;
$BODY$;

CREATE OR REPLACE FUNCTION append_events(
	p_events dcb_event_raw[],
	p_after_position bigint,
	VARIADIC p_fail_if_match dcb_query_item[])
    RETURNS TABLE("position" bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 10

AS $BODY$
DECLARE
	TEMPLATE_TYPES_HASHES CONSTANT text := 'e.type_hash = ANY($2[%1$s].type_hashes::integer[])';
	TEMPLATE_TAGS_HASHES CONSTANT text := 'e.tag_hashes @> $2[%1$s].tag_hashes::bigint[]';
	TEMPLATE_TYPES_NAMES CONSTANT text := 'e.type_name = ANY($2[%1$s].type_names::varchar[])';
	TEMPLATE_TAGS_NAMES CONSTANT text := 'e.tag_names @> $2[%1$s].tag_names::varchar[]';

	TEMPLATE_INSERT CONSTANT text := '(0::bigint, $3[%1$s].timestamp_utc::timestamp, $3[%1$s].type_hash::integer, $3[%1$s].type_name::varchar, $3[%1$s].tag_hashes::bigint[], $3[%1$s].tag_names::varchar[], $3[%1$s].payload::bytea)::dcb_event';
	ins_statement text;

	MAX_LOCK_TIME_OUT_SEC CONSTANT real := 15;
	cur_wait_sec real := 0;
	total_wait_sec real := 0;
BEGIN
	SELECT 'SELECT * from unnest(array[' || string_agg(format(TEMPLATE_INSERT, j) , ',') || ']) '
	INTO STRICT ins_statement
	FROM generate_series(1, array_length(p_events, 1)) AS j;

	ins_statement := 'INSERT INTO dcb_events(position, timestamp_utc, type_hash, type_name, tag_hashes, tag_names, payload) ' || ins_statement ||
	   'WHERE NOT EXISTS (SELECT 1 FROM dcb_events e WHERE e.position > $1 AND (' || string_agg(
			concat_ws(' AND ',
				(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_HASHES, i) ELSE 'type_hash IS NOT NULL' END),
				(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_HASHES, i) ELSE NULL END),
				(SELECT CASE WHEN types_count IS NOT NULL THEN format(TEMPLATE_TYPES_NAMES, i) ELSE NULL END),
				(SELECT CASE WHEN tags_count IS NOT NULL THEN format(TEMPLATE_TAGS_NAMES, i) ELSE NULL END)
			),
			' OR '
		) || '))'
		' RETURNING position'
		FROM (SELECT i, array_length(p_fail_if_match[i].type_hashes, 1) as types_count, array_length(p_fail_if_match[i].tag_hashes, 1) as tags_count FROM generate_series(1, array_length(p_fail_if_match, 1)) AS i);

	WHILE total_wait_sec < MAX_LOCK_TIME_OUT_SEC AND NOT pg_try_advisory_xact_lock(101010101) LOOP
		cur_wait_sec := random() * 0.015;
		total_wait_sec := total_wait_sec + cur_wait_sec;
		PERFORM pg_sleep(cur_wait_sec);
	END LOOP;

	IF total_wait_sec >= MAX_LOCK_TIME_OUT_SEC THEN
		RAISE EXCEPTION 'Time out lock event store.'
		USING ERRCODE = 'DCB01', 
		HINT = 'System busy. Please retry later.';
	END IF;

	RETURN QUERY EXECUTE ins_statement USING p_after_position, p_fail_if_match, p_events;
END;
$BODY$;


-- Table: dcb_positions

-- DROP TABLE IF EXISTS dcb_positions;

CREATE TABLE IF NOT EXISTS dcb_positions
(
    last_position bigint NOT NULL
);

-- FUNCTION: update_event_position()

-- DROP FUNCTION IF EXISTS update_event_position();

CREATE OR REPLACE FUNCTION update_event_position()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
	payload TEXT;
BEGIN
    SELECT last_position + 1 INTO NEW.position
	    FROM dcb_positions LIMIT 1 FOR UPDATE;

	UPDATE dcb_positions
		SET last_position = NEW.position;

	payload := json_build_object(
		'position', NEW.position,
		'timestamp_utc', NEW.timestamp_utc,
		'type_hash', NEW.type_hash,
		'type_name', NEW.type_name,
		'tag_hashes', NEW.tag_hashes,
		'tag_names', NEW.tag_names
	);

	-- Notify the channel
  	PERFORM pg_notify('dcb_events', payload);

    RETURN NEW;
END;
$BODY$;

-- Trigger: update_event_position

-- DROP TRIGGER IF EXISTS update_event_position ON dcb_events;

CREATE OR REPLACE TRIGGER update_event_position
    BEFORE INSERT
    ON dcb_events
    FOR EACH ROW
    EXECUTE FUNCTION update_event_position();


TRUNCATE TABLE ONLY dcb_positions;

INSERT INTO dcb_positions (SELECT COALESCE(MAX("position"), 0) as last_position FROM dcb_events);
