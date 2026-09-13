--- DOMAINS ---

-- Type: dcb_query_item

-- DROP TYPE IF EXISTS dcb_query_item;

CREATE TYPE dcb_query_item AS
(
	type_hashes integer[],
	type_names text[],
	tag_hashes bigint[],
	tag_names text[]
);

-- Type: dcb_event_raw

-- DROP TYPE IF EXISTS dcb_event_raw;

CREATE TYPE dcb_event_ins AS
(
	timestamp_utc timestamp,
	type_hash integer,
	type_name text,
	tag_hashes bigint[],
	tag_names text[],
	payload json
);

-- Type: dcb_event

-- DROP TYPE IF EXISTS dcb_event;

CREATE TYPE dcb_event AS
(
	"position" bigint,
	timestamp_utc timestamp,
	type_hash integer,
	type_name varchar,
	tag_hashes bigint[],
	tag_names varchar[],
	payload bytea
);
