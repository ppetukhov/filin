--- DOMAINS ---

-- DOMAIN: dcb_event_position

-- DROP DOMAIN IF EXISTS dcb_event_position;

CREATE DOMAIN dcb_event_position
    AS bigint;

-- DOMAIN: dcb_event_tag_hash

-- DROP DOMAIN IF EXISTS dcb_event_tag_hash;

CREATE DOMAIN dcb_event_tag_hash
    AS integer;

-- DOMAIN: dcb_event_tag_name

-- DROP DOMAIN IF EXISTS dcb_event_tag_name;

CREATE DOMAIN dcb_event_tag_name
    AS character varying(512)
    COLLATE pg_catalog."C.utf8";

-- DOMAIN: dcb_event_timestamp

-- DROP DOMAIN IF EXISTS dcb_event_timestamp;

CREATE DOMAIN dcb_event_timestamp
    AS timestamp without time zone;

COMMENT ON DOMAIN dcb_event_timestamp
    IS 'in UTC';

-- DOMAIN: dcb_event_type_hash

-- DROP DOMAIN IF EXISTS dcb_event_type_hash;

CREATE DOMAIN dcb_event_type_hash
    AS integer;

-- DOMAIN: dcb_event_type_name

-- DROP DOMAIN IF EXISTS dcb_event_type_name;

CREATE DOMAIN dcb_event_type_name
    AS character varying(512)
    COLLATE pg_catalog."C.utf8";

-- DOMAIN: dcb_event_payload

-- DROP DOMAIN IF EXISTS dcb_event_payload;

CREATE DOMAIN dcb_event_payload
    AS bytea;

-- Type: dcb_query_item

-- DROP TYPE IF EXISTS dcb_query_item;

CREATE TYPE dcb_query_item AS
(
	type_hashes dcb_event_type_hash[],
	type_names dcb_event_type_name[],
	tag_hashes dcb_event_tag_hash[],
	tag_names dcb_event_tag_name[]
);

-- Type: dcb_event_raw

-- DROP TYPE IF EXISTS dcb_event_raw;

CREATE TYPE dcb_event_raw AS
(
	timestamp_utc dcb_event_timestamp,
	type_hash dcb_event_type_hash,
	type_name dcb_event_type_name,
	tag_hashes dcb_event_tag_hash[],
	tag_names dcb_event_tag_name[],
	payload dcb_event_payload
);

-- Type: dcb_event

-- DROP TYPE IF EXISTS dcb_event;

CREATE TYPE dcb_event AS
(
	"position" dcb_event_position,
	timestamp_utc dcb_event_timestamp,
	type_hash dcb_event_type_hash,
	type_name dcb_event_type_name,
	tag_hashes dcb_event_tag_hash[],
	tag_names dcb_event_tag_name[],
	payload dcb_event_payload
);
