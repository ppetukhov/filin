namespace Filin.Internal;

record struct StateMetadata(EventPosition Position, DateTimeOffset Timestamp, StateStatus Status);
