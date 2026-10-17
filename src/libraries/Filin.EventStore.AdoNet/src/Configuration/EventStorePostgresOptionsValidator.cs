using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Provides compile-time generated validation for <see cref="EventStoreAdoNetOptions"/> using data annotations.
/// </summary>
[OptionsValidator]
public sealed partial class EventStorePostgresOptionsValidator : IValidateOptions<EventStoreAdoNetOptions>
{
}
