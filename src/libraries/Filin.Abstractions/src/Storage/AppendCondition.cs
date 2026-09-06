namespace Filin.Storage;

/// <summary>
/// The Append Condition is used to enforce consistency, ensuring that between the time of building the Decision Model and appending the events,
/// no new events were stored by another client that match the same query.
/// </summary>
/// <param name="FailIfEventsMatch">Query is typically the same Query that was used when building the Decision Model,
/// which guarantees that the Decision Model is still the same when we append new events.</param>
/// <param name="After">Represents the highest position the client was aware of while building the Decision Model.
/// The Event Store MUST ignore the Events before the specified position while checking the condition for appending events.
/// Note: This number can be higher than the position of the last event matching the Query.</param>
public readonly record struct AppendCondition(DcbQuery FailIfEventsMatch, EventPosition After);
