## About

<!-- A description of the package and where one can find more documentation -->

`Filin.EventStore.AdoNet` provides an ADO.NET-based `IEventStore` implementation for the **Filin Event Sourcing framework**.

> **Note:** Currently, this package supports **PostgreSQL** only. Support for additional ADO.NET database providers is planned for future releases.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### 1. Configure PostgreSQL

Ensure the **`pg_cron`** extension is installed and enabled in your PostgreSQL instance before applying the schema.
It is used solely to schedule automated creation of new table partitions.

*(Note: `pg_cron` requires configuring `shared_preload_libraries = 'pg_cron'` in `postgresql.conf` and restarting PostgreSQL).*

Initialize the database schema manually by executing the **`PostgreSQL-EventStore.sql`** script included in this NuGet package against your target database (for example, via `psql` or your preferred migration tool):

```bash
psql -d your_database -f PostgreSQL-EventStore.sql
```

#### Recommended `postgresql.conf` (for 2 CPU / 4GB RAM)

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

### 2. Configure Filin

First, set up your PostgreSQL connection strings and service settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "LocalPostgresDev": "Server=localhost;Port=5432;Database=dev;Search Path=public;Application Name=Awesome App;User Id=***;Password=***"
  },
  "Filin": {
    "EventStore": {
      "ConnectionName": "LocalPostgresDev",
      "Invariant": "Npgsql"
    }
  }
}
```

Register the PostgreSQL event store in your dependency injection container (`Program.cs`):

```csharp
using Filin.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventStoreAdoNet();
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Filin.Configuration.EventStoreAdoNetBackoff`
* `Filin.Configuration.EventStoreAdoNetOptions`
* `Filin.DependencyInjection.FilinServiceCollectionExtensions`

## Additional Documentation

<!-- Links to documentation -->

* [Filin framework](https://github.com/ppetukhov/filin)
* [DCB Specification](https://dcb.events/specification/) - Dynamic Consistency Boundaries pattern

## Related Packages

<!-- The related packages associated with this package -->

| Package | Description |
|---------|-------------|
| [Filin](https://www.nuget.org/packages/Filin) | Core implementation of the Filin `IMediator`. |
| [Filin.Abstractions](https://www.nuget.org/packages/Filin.Abstractions) | Contains core Filin primitives, annotation attributes, and interfaces. |
| [Filin.ProjectionStore.AdoNet](https://www.nuget.org/packages/Filin.ProjectionStore.AdoNet) | ADO.NET-based implementation of the Filin `IProjectionStore`. |

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Filin.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/ppetukhov/filin).