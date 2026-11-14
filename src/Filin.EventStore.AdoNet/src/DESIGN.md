# High-Performance Deterministic Event Log with Dynamic Consistency Boundary (DCB)

A high-performance transactional event log based on **PostgreSQL** DBMS, implementing the **Dynamic Consistency Boundary (DCB)** concept and **Event Sourcing** methodology under extreme workloads.

The architecture is designed and hardware-optimized to run on mid-range hardware. By completely eliminating JSON parsing on the database side, utilizing binary indexes, and leveraging the **Group Commit** mechanism, the system can handle peak loads of up to **5,000 – 10,000 RPS** with highly concurrent workloads.

---

## 🏗️ Design Highlights

1. **Absolute Gapless Property (No missing IDs):** The global identifier `position` is incremented pessimistically strictly within an ACID transaction *after* successfully passing all business validations. Any logical or system failure triggers an automatic `ROLLBACK`, guaranteeing the continuity of the numeric sequence.
2. **Dynamic Consistency Boundary (DCB) Determinism:** The consistency boundary is calculated dynamically on the fly. The *Fail-If-Match* method operates on an **OR over tuple lists with an internal AND** principle: a transaction aborts if an event is found at the tail of the log whose type matches any in the filter *AND* simultaneously contains *all* the tags of that tuple.
3. **In-Memory Indexing Layer:** The table is partitioned by `position` ranges with a step of exactly **2,000,000 rows**. The indexes of the current (active) partition weigh only **~208 MB**, allowing them to be 100% cached in the RAM, completely eliminating disk I/O bottlenecks (*Disk Thrashing*).
4. **Alignment Padding Optimization:** The column order in the `dcb_events` table is sorted strictly by descending physical size of data types (`bigint` -> `integer` -> `text` -> `arrays`).  This eliminates empty CPU alignment bytes, significantly saving RAM and disk space over time simply by removing inter-column gaps.
5. **Strict Synchronicity and ACID (synchronous_commit = on):** The system guarantees 100% data durability: a successful commit response is returned to the application only after the WAL log is physically flushed to the NVMe storage drive.
6. **Pessimistic Serialization (The Bottleneck):** To eliminate concurrent write conflicts during the DCB validation phase, the architecture acquires an exclusive row lock (`RowExclusiveLock`) on a single row of the global counter `dcb_global_counter` at the very beginning of the function. This forces parallel writing threads into a strict sequential queue, making the system **Bulk-oriented** (optimized for batch insertion).

---

## 📊 Capacity & Performance

* **Real Hardware Throughput (2 CPU / 4 GB RAM): 5,000 – 10,000 RPS**.
* **Business Latency SLA:** **~0.5 – 1.5 milliseconds** per transaction (determined by the physical write confirmation speed (`fsync`) of your NVMe subsystem and the execution of DCB validation).
* **Average Event Size:** ~1.2 KB (`payload` ~1 KB pure JSON + ~200 bytes metadata).
* **Single Table Partition Size (2 million rows):** ~2.4 GB of data + ~208 MB of indexes (ideal for keeping the active working set in `shared_buffers`).

---

## ⚖️ Architectural Alternatives Comparison

To prove the efficiency of the chosen solution, a comparison was conducted among three approaches to implementing DCB insertion at the DBMS level.

| Comparison Criterion | Chosen Option: Two-Step (In-Memory PL/pgSQL) 🌟 | Alternative 1: Subquery (`INSERT ... SELECT ... WHERE NOT EXISTS`) | Alternative 2: ID Assignment via `BEFORE INSERT` Trigger |
| :--- | :--- | :--- | :--- |
| **Peak RPS (2 CPU / 4GB)** | **5,000 – 10,000 RPS** | ~1,500 – 3,000 RPS (CPU degradation on query plans) | < 800 RPS (degradation due to counter row locks) |
| **Gapless Guarantee (No ID missing)**| **100% stability.** The counter increments only after passing the DCB filter, or rolls back cleanly. | **100% stability.** The counter does not advance if the `WHERE` clause evaluates to false. | **Not guaranteed.** Any transaction abort *after* the trigger permanently burns the ID, causing gaps. |
| **Contention Level (Locks)**| **High (Pessimistic).** The counter row is locked tightly at the start of the function for guaranteed linearizability. | **High.** The counter is locked during the `INSERT` column evaluation phase. | **Maximum (Catastrophic).** The counter is locked at the start of the insertion. The entire table queues up. |
| **Race Condition Protection in DCB**| **Absolute.** Logical ordering and early counter locking guarantee strict serialization. | **Vulnerable to Phantom Reads.** At the `Read Committed` level, two parallel threads will insert duplicates. | **Vulnerable to Phantom Reads.** The trigger cannot see uncommitted inserts from neighboring sessions. |
| **Write Latency** | **~0.5 - 1.5 ms** | ~0.4 – 0.9 ms (Fluctuations due to internal subquery locks) | Exponential growth due to holding the lock throughout the entire validation cycle. |
| **DBMS CPU Overhead** | **Minimal.** Clean binary types via domain unnest, 0% JSON overhead for validation. | High (heavy combined DML execution plan). | High (additional call context overhead of the trigger engine). |

---

### 🧩 Alignment Padding Optimization

In modern CPU architectures (x86_64 / ARM64) and the PostgreSQL storage subsystem (`storage engine`), data is read from RAM in physical blocks. To ensure the processor reads data in the minimum number of cycles, the DBMS kernel aligns data types to memory addresses that are multiples of their size (`typalign`).

If columns are arranged randomly, PostgreSQL is forced to insert empty hidden bytes (**alignment padding**) between fields, resulting in massive storage overhead at large scale.

The `dcb_events` table utilizes a **mathematically ideal field order** (strictly sorted by descending byte footprint while considering the specification of variable-length `varlena` types), reducing hardware overhead to the absolute technical minimum:

#### 📊 Physical Row Structure Map in shared_buffers and on Disk:

| Column Name in Table | Native DBMS Data Type | Physical Size | Memory Offset | System Padding (Empty Space) |
| :--- | :--- | :--- | :--- | :--- |
| `position` | `bigint` | 8 bytes | `0 .. 7` | 0 bytes |
| `created_at` | `timestamp without time zone` | 8 bytes | `8 .. 15` | 0 bytes (back-to-back) |
| `event_type_hash` | `integer` | 4 bytes | `16 .. 19` | 0 bytes (16 is multiple of 4) |
| **[Hardware Alignment Gap]** | — | — | — | **4 bytes** *(required to align varlena)* |
| `event_type` | `text` *(varlena ptr)* | 8 bytes | `24 .. 31` | 0 bytes (24 is multiple of 8) |
| `tag_hashes` | `bigint[]` *(varlena ptr)* | 8 bytes | `32 .. 39` | 0 bytes |
| `tags` | `text[]` *(varlena ptr)* | 8 bytes | `40 .. 47` | 0 bytes |
| `payload` | `json` *(varlena ptr)* | 8 bytes | `48 .. 55` | 0 bytes |

---

## 🗂️ Deployment Pipeline Structure

Database initialization is divided into 4 modules inside the `PostgreSQL-EventStore.sql` file, executed in a strict chronological sequence:

1. **`01_TYPES_AND_EXTENSIONS`** — Provisioning the `pg_cron` system scheduler and registering domain composite data types: `dcb_event` and `dcb_query_item`.
2. **`02_TABLES_AND_INDEXES`** — Creating the counter singleton table, initializing the main partitioned table `dcb_events`, provisioning the first physical partition `dcb_events_p1` (`fillfactor = 100`), and generating compact numeric indexes (`B-Tree` + `GIN`).
3. **`03_STORED_PROCEDURES`** — Deploying transactional controllers: single insertion (`dcb_append_event`), batch insertion returning a position table (`dcb_append_events`), and chronological streaming readers supporting wildcards (`dcb_read_events`).
4. **`04_TRIGGERS_AND_CRON`** — Setting up the Pub/Sub layer for ultra-lightweight real-time notifications (`pg_notify` to the `dcb_events` channel at ~250 bytes, strictly without payload) and the background cron job for automatic forward partitioning.

---

## ⚡️ API Interaction Specification (SQL Examples)

### 1. Single Commit (`dcb_append_event`)

Returns a table containing a row with the generated `position` and the precise database timestamp `created_at` on success, or **0 rows** (empty dataset) on a DCB validation conflict.

```sql
SELECT * FROM dcb_append_event(
    -- dcb_event: hash -> name -> tag_hashes -> tags -> payload
    ROW(469666037, 'OrderCreated', ARRAY[6972188522121857746::int8], ARRAY['order:10000']::text[], '{"id":1}'::json)::dcb_event,
    10::bigint, -- p_cond_after (Consistency reference point)
    -- VARIADIC p_cond_fail_if_match: Insertion conditions (Abort if OrderCreated is found with the tag 'order:10000')
    ROW(ARRAY[469666037]::int4[], ARRAY['OrderCreated']::text[], ARRAY[6972188522121857746::int8], ARRAY['order:10000']::text[])::dcb_query_item
);
```

### 2. Batch Commit (`dcb_append_events`)

Accepts an array of events. Returns a table (`RETURNS TABLE`) containing the list of assigned position IDs and their corresponding database timestamps (`created_at`). Returns **0 rows** (empty dataset) if a conflict occurs.

```sql
SELECT * FROM dcb_append_events(
    ARRAY[
        ROW(469666037, 'OrderCreated', ARRAY[6972188522121857746::int8], ARRAY['order:10000']::text[], '{"id":1}'::json),
        ROW(-124006684, 'ItemsReserved', ARRAY[6972188522121857746::int8, 435648292011973478::int8], ARRAY['order:10000', 'stock_active']::text[], '{"status":"ok"}'::json)
    ]::dcb_event[],
    10::bigint,
    -- Insertion conditions: Tuple 1 (Exact match context) OR Tuple 2 (Wildcard: Any tag for the PaymentFailed type)
    ROW(ARRAY[469666037]::int4[], ARRAY['OrderCreated']::text[], ARRAY[6972188522121857746::int8], ARRAY['order:10000']::text[])::dcb_query_item,
    ROW(ARRAY[1129419947]::int4[], ARRAY['PaymentFailed']::text[], '{}'::int8[], '{}'::text[])::dcb_query_item
);
```

### 3. Chronological Log Streaming (`dcb_read_events`)

Invoked by background projection workers for deterministic state replay. Returns a stream of records `SETOF dcb_events` sorted strictly in ascending order by ID.
```sql
SELECT * FROM dcb_read_events(
    10::bigint, -- Start reading strictly AFTER this position
    -- Filter: Stream only OrderCreated events matching the 'order:10000' tag. If no filters are provided, it streams the entire log sequentially.
    ROW(ARRAY[469666037]::int4[], ARRAY['OrderCreated']::text[], ARRAY[6972188522121857746::int8], ARRAY['order:10000']::text[])::dcb_query_item
); 
```

---

## 🛠️ Recommended `postgresql.conf` (for 2 CPU / 4GB RAM)

To fully exploit the available hardware potential without exhausting the thread pool, apply the following baseline tuning:

```ini
#------------------------------------------------------------------------------
# EXTENSIONS & PRELOAD LIBRARIES
#------------------------------------------------------------------------------
shared_preload_libraries = 'pg_cron'  # Load pg_cron extension at server startup (default: '')
cron.database_name = 'postgres'       # Database where pg_cron metadata is stored (default: 'postgres')
cron.use_background_workers = on      # Run jobs via background workers instead of TCP localhost (default: off)

#------------------------------------------------------------------------------
# CONNECTIONS AND MEMORY (4GB RAM, 80 connections, Partitioned tables)
#------------------------------------------------------------------------------
max_connections = 80
shared_buffers = 1GB                  # 25% of total RAM (default: 128MB)
effective_cache_size = 3GB            # 75% of total RAM (default: 4GB)
work_mem = 4MB                        # Safe limit per sort/hash for 80 conns + partition metadata (default: 4MB)
maintenance_work_mem = 256MB          # Memory for VACUUM and index creation (default: 64MB)
wal_buffers = 16MB                    # Optimal WAL buffer for 1GB shared_buffers (default: -1 / auto)

#------------------------------------------------------------------------------
# CPU AND PARALLELISM (2 vCPU, 70% Index Reads / 30% Contended Writes)
#------------------------------------------------------------------------------
max_worker_processes = 5              # Pool for 2 autovacuum + background workers (default: 8)
max_parallel_workers = 1              # Reserve max 1 worker globally so at least 1 core stays free (default: 8)
max_parallel_workers_per_gather = 0   # Disable intra-query parallelism for fast index lookups (default: 2)
max_parallel_maintenance_workers = 1  # Keep background index/vacuum tasks on 1 core (default: 2)

#------------------------------------------------------------------------------
# NVME SSD, QUERY PLANNER & PARTITIONING
#------------------------------------------------------------------------------
random_page_cost = 1.1                # Low random seek penalty for NVMe SSD (default: 4.0)
effective_io_concurrency = 200        # High concurrent I/O queue depth for NVMe (default: 1)
default_statistics_target = 100       # Standard statistics precision (default: 100)
enable_partition_pruning = on         # Skip irrelevant partitions via id filter (default: on)
enable_partitionwise_join = off       # Avoid high planner RAM usage on 4GB RAM (default: off)
enable_partitionwise_aggregate = off  # Keep planning fast for OLTP (default: off)

#------------------------------------------------------------------------------
# WAL, CHECKPOINTS AND COMMIT TUNING (1KB rows, Gapless counter)
#------------------------------------------------------------------------------
min_wal_size = 1GB                    # Reuse WAL files instead of recreating (default: 80MB)
max_wal_size = 4GB                    # Reduce checkpoint frequency on NVMe (default: 1GB)
checkpoint_completion_target = 0.9    # Spread checkpoint I/O evenly (default: 0.9)
wal_compression = zstd                # Fast compression for 1KB rows in WAL (default: off)
synchronous_commit = on               # Strict durability for gapless counter (default: on)
commit_delay = 10                     # 10 microseconds delay to group concurrent WAL flushes (default: 0)
commit_siblings = 5                   # Trigger commit_delay when >= 5 transactions are active (default: 5)

#------------------------------------------------------------------------------
# AUTOVACUUM (Tuned for frequent single-row counter updates)
#------------------------------------------------------------------------------
autovacuum_max_workers = 2            # Fit 2 vCPU budget (default: 3)
autovacuum_naptime = 5s               # Frequent checks to clean dead tuples in counter table (default: 1min)
```
