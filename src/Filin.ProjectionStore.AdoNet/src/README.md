## About

<!-- A description of the package and where one can find more documentation -->

`Filin.ProjectionStore.AdoNet` provides an ADO.NET-based `IProjectionStore` implementation for the **Filin Event Sourcing framework**.

> **Note:** Currently, this package supports **PostgreSQL** only. Support for additional ADO.NET database providers is planned for future releases.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### 1. Configure PostgreSQL

Initialize the database schema manually by executing the **`PostgreSQL-ProjectionStore.sql`** script included in this NuGet package against your target database (for example, via `psql` or your preferred migration tool):

```bash
psql -d your_database -f PostgreSQL-ProjectionStore.sql
```

### 2. Configure Filin

First, set up your PostgreSQL connection strings and service settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "LocalPostgresDev": "Server=localhost;Port=5432;Database=dev;Search Path=public;Application Name=Awesome App;User Id=***;Password=***"
  },
  "Filin": {
    "ProjectionStore": {
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

builder.Services.AddProjectionStoreAdoNet();
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Filin.Configuration.ProjectionStoreAdoNetOptions`
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
| [Filin.EventStore.AdoNet](https://www.nuget.org/packages/Filin.EventStore.AdoNet) | ADO.NET-based implementation of the Filin `IEventStore`. |

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Filin.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/ppetukhov/filin).