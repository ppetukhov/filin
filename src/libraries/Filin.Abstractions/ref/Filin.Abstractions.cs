// This file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ----------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace Filin
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed partial class ApplicationPartAttribute : System.Attribute
    {
        public ApplicationPartAttribute(string assemblyName) { }

        public string AssemblyName { get { throw null; } }
    }

    public sealed partial class DcbConflictException : DcbException
    {
        public DcbConflictException() { }

        public DcbConflictException(string? message, System.Exception? innerException) { }

        public DcbConflictException(string? message) { }
    }

    public ref struct Enumerator<T> where T : notnull
    {
        public bool MoveNext() { throw null; }

        public readonly T Current { get { throw null; } }
    }

    public readonly record struct Error(string Code, string Message);

    public sealed record DcbEvent
    {
        public required object Data { get { throw null; } init { } }

        public required EventType EventType { get { throw null; } init { } }

        public System.Collections.Immutable.ImmutableHashSet<Tag>? Tags { get { throw null; } init { } }

        public required System.DateTimeOffset Timestamp { get { throw null; } init { } }

        public static implicit operator DcbQuery(DcbEvent evt) { throw null; }

        public static implicit operator DcbQueryItem(DcbEvent evt) { throw null; }

        public DcbQuery ToDcbQuery() { throw null; }

        public DcbQueryItem ToDcbQueryItem() { throw null; }

        public override string ToString() { throw null; }
    }

    public readonly partial record struct DcbEventSequenced(EventPosition Position, DcbEvent Event)
    {
    }

    public partial class DcbEventStoreException : DcbException
    {
        public DcbEventStoreException() { }

        public DcbEventStoreException(string? message, System.Exception? innerException) { }

        public DcbEventStoreException(string? message) { }
    }

    public abstract partial class DcbException : System.Exception
    {
        protected DcbException() { }

        protected DcbException(string? message, System.Exception? innerException) { }

        protected DcbException(string? message) { }
    }

    public partial class DcbProjectionStoreException : DcbException
    {
        public DcbProjectionStoreException() { }

        public DcbProjectionStoreException(string? message, System.Exception? innerException) { }

        public DcbProjectionStoreException(string? message) { }
    }

    public abstract partial class DcbProjectorException : DcbException
    {
        protected DcbProjectorException() { }

        protected DcbProjectorException(string? message, System.Exception? innerException) { }

        protected DcbProjectorException(string? message) { }
    }

    public sealed partial record DcbQuery(System.Collections.Immutable.ImmutableHashSet<DcbQueryItem> Items)
    {
        public static readonly DcbQuery All;
        public bool IsAll { get { throw null; } }

        public DcbQuery Compact() { throw null; }

        public static DcbQuery FromImmutableHashSet(System.Collections.Immutable.ImmutableHashSet<DcbEvent> dcbEvents) { throw null; }

        public static DcbQuery FromItems(System.Collections.Generic.IEnumerable<DcbQueryItem> items) { throw null; }

        public bool MatchesEvent(DcbEvent @event) { throw null; }

        public static implicit operator DcbQuery(System.Collections.Immutable.ImmutableHashSet<DcbEvent> dcbEvents) { throw null; }

        public DcbQuery Or(DcbQuery query) { throw null; }

        public DcbQuery Or(DcbQueryItem queryItem) { throw null; }
    }

    public sealed partial record DcbQueryItem(System.Collections.Immutable.ImmutableHashSet<EventType> EventTypes, System.Collections.Immutable.ImmutableHashSet<Tag> Tags)
    {
        public static readonly DcbQueryItem Any;
        public DcbQueryItem(EventType eventType) : this([eventType], []) { }

        public DcbQueryItem(Tag tag) : this([], [tag]) { }

        public DcbQueryItem(System.Collections.Generic.IEnumerable<EventType> eventTypes, System.Collections.Generic.IEnumerable<Tag> tags) : this([.. eventTypes], [.. tags]) { }

        public DcbQueryItem(System.Collections.Generic.IEnumerable<EventType> eventTypes) : this([.. eventTypes], []) { }

        public DcbQueryItem(System.Collections.Generic.IEnumerable<Tag> tags) : this([], [.. tags]) { }

        public bool IsAny { get { throw null; } }

        public DcbQueryItem And(EventType eventType, Tag tag) { throw null; }

        public DcbQueryItem And(EventType eventType, System.Collections.Generic.IEnumerable<Tag> tags) { throw null; }

        public DcbQueryItem And(Tag tag) { throw null; }

        public DcbQueryItem And(System.Collections.Generic.IEnumerable<EventType> eventTypes, Tag tag) { throw null; }

        public DcbQueryItem And(System.Collections.Generic.IEnumerable<EventType> eventTypes, System.Collections.Generic.IEnumerable<Tag> tags) { throw null; }

        public DcbQueryItem And(System.Collections.Generic.IEnumerable<Tag> tags) { throw null; }

        public bool MatchesEvent(DcbEvent @event) { throw null; }

        public static implicit operator DcbQuery(DcbQueryItem queryItem) { throw null; }

        public DcbQueryItem Or(EventType eventType) { throw null; }

        public DcbQueryItem Or(System.Collections.Generic.IEnumerable<EventType> eventTypes) { throw null; }

        public DcbQuery ToDcbQuery() { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed partial class DeciderAttribute : System.Attribute
    {
    }

    public readonly partial record struct EventPosition(ulong Value) : System.IEquatable<EventPosition>, System.IComparable<EventPosition>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        private readonly ulong _dummyPrimitive = Value;

        public bool IsDefault { get { throw null; } }

        public readonly int CompareTo(EventPosition other) { throw null; }

        public static bool operator >(EventPosition left, EventPosition right) { throw null; }

        public static bool operator >=(EventPosition left, EventPosition right) { throw null; }

        public static bool operator <(EventPosition left, EventPosition right) { throw null; }

        public static bool operator <=(EventPosition left, EventPosition right) { throw null; }

        public override readonly string ToString() { throw null; }

        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Property | System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed partial class EventTagAttribute : System.Attribute
    {
        public EventTagAttribute(string tag) { }

        public string Tag { get { throw null; } }
    }

    public readonly partial struct EventType : System.IEquatable<EventType>, System.IComparable<EventType>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        public EventType(string name) { }

        public bool IsDefault { get { throw null; } }

        public readonly System.ReadOnlySpan<byte> AsSpan() { throw null; }

        public readonly int CompareTo(EventType other) { throw null; }

        public readonly bool Equals(EventType obj) { throw null; }

        public override readonly bool Equals(object? obj) { throw null; }

        public static EventType FromIdentity(Identity rawIdentity) { throw null; }

        public static EventType FromString(string name) { throw null; }

        public override readonly int GetHashCode() { throw null; }

        public static bool operator ==(EventType left, EventType right) { throw null; }

        public static explicit operator Identity(EventType eventType) { throw null; }

        public static explicit operator EventType(Identity id) { throw null; }

        public static bool operator >(EventType left, EventType right) { throw null; }

        public static bool operator >=(EventType left, EventType right) { throw null; }

        public static implicit operator string?(EventType eventType) { throw null; }

        public static implicit operator EventType(string name) { throw null; }

        public static bool operator !=(EventType left, EventType right) { throw null; }

        public static bool operator <(EventType left, EventType right) { throw null; }

        public static bool operator <=(EventType left, EventType right) { throw null; }

        public static Identity ToIdentity(EventType eventType) { throw null; }

        public override readonly string? ToString() { throw null; }

        public readonly string ToString(string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed partial class EventTypeAttribute : System.Attribute
    {
        public EventTypeAttribute(string eventType, uint version) { }

        public EventTypeAttribute(string eventType) { }

        public string EventType { get { throw null; } }

        public uint Version { get { throw null; } }
    }

    public partial interface ICommand<TState> : IWithQueryTags where TState : notnull
    {
    }

    public partial interface IDecider<TCommand, TState>
        where TCommand : notnull, ICommand<TState>
        where TState : notnull
    {
        static abstract DecideResult Decide(TCommand command, TState state);
        static abstract DecideResult Decide(TCommand command);
    }

    public readonly partial struct Identity : System.IEquatable<Identity>, System.IComparable<Identity>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        private readonly object _value;
        private readonly int _dummyPrimitive;
        public Identity(byte[]? value) { }

        public Identity(string? id) { }

        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, "_value")]
        public bool IsDefault { get { throw null; } }

        public readonly System.ReadOnlySpan<byte> AsSpan() { throw null; }

        public readonly int CompareTo(Identity other) { throw null; }

        public readonly bool Equals(Identity obj) { throw null; }

        public override readonly bool Equals(object? obj) { throw null; }

        public static Identity FromString(string value) { throw null; }

        public override readonly int GetHashCode() { throw null; }

        public static bool operator ==(Identity left, Identity right) { throw null; }

        public static bool operator >(Identity left, Identity right) { throw null; }

        public static bool operator >=(Identity left, Identity right) { throw null; }

        public static implicit operator Identity(string name) { throw null; }

        public static bool operator !=(Identity left, Identity right) { throw null; }

        public static bool operator <(Identity left, Identity right) { throw null; }

        public static bool operator <=(Identity left, Identity right) { throw null; }

        public override readonly string? ToString() { throw null; }

        public readonly string ToString(string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }

        public static Identity UnsafeCreate(byte[]? value, int hashCode) { throw null; }
    }

    public partial interface IProjector<TState> where TState : notnull
    {
        static abstract ProjectorId ProjectorId { get; }
        static abstract ProjectionId GetProjectionId(System.Collections.Immutable.ImmutableHashSet<Tag> tags);
        static abstract DcbQuery GetQuery(System.Collections.Immutable.ImmutableHashSet<Tag> tags);
        static abstract TState Project(TState state, DcbEventSequenced domainEvent);
        static abstract TState Project(DcbEventSequenced domainEvent);
    }

    public partial interface IQuery<TProjection> : IWithQueryTags where TProjection : notnull
    {
    }

    public partial interface IWithQueryTags
    {
        System.Collections.Immutable.ImmutableHashSet<Tag> GetTags();
    }

    public sealed partial class FilinConfigurationException : System.Exception
    {
        public FilinConfigurationException() { }

        public FilinConfigurationException(string? message, System.Exception? innerException) { }

        public FilinConfigurationException(string? message) { }
    }

    public readonly partial struct ProjectionId : System.IEquatable<ProjectionId>, System.IComparable<ProjectionId>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        public ProjectionId(ProjectorId projectorId, Identity key) { }

        public bool IsDefault { get { throw null; } }

        public Identity Key { get { throw null; } }

        public ProjectorId ProjectorId { get { throw null; } }

        public readonly int CompareTo(ProjectionId other) { throw null; }

        public readonly void Deconstruct(out ProjectorId projectorId, out Identity key) { throw null; }

        public readonly bool Equals(ProjectionId obj) { throw null; }

        public override readonly bool Equals(object? obj) { throw null; }

        public static ProjectionId FromTuple((ProjectorId Projector, Identity Key) tuple) { throw null; }

        public override readonly int GetHashCode() { throw null; }

        public static bool operator ==(ProjectionId left, ProjectionId right) { throw null; }

        public static explicit operator ProjectionId((ProjectorId Projector, Identity Key) tuple) { throw null; }

        public static bool operator >(ProjectionId left, ProjectionId right) { throw null; }

        public static bool operator >=(ProjectionId left, ProjectionId right) { throw null; }

        public static implicit operator string?(ProjectionId value) { throw null; }

        public static bool operator !=(ProjectionId left, ProjectionId right) { throw null; }

        public static bool operator <(ProjectionId left, ProjectionId right) { throw null; }

        public static bool operator <=(ProjectionId left, ProjectionId right) { throw null; }

        public override readonly string? ToString() { throw null; }

        public readonly string ToString(string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
    }

    public sealed partial record ProjectionMetadata()
    {
        public ProjectionId Id { get { throw null; } init { } }

        public EventPosition Position { get { throw null; } init { } }

        public System.DateTimeOffset Timestamp { get { throw null; } init { } }
    }

    public readonly partial struct ProjectorId : System.IEquatable<ProjectorId>, System.IComparable<ProjectorId>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        public ProjectorId(string id, uint version) { }

        public ProjectorId(string id) { }

        public bool IsDefault { get { throw null; } }

        public readonly System.ReadOnlySpan<byte> AsSpan() { throw null; }

        public readonly int CompareTo(ProjectorId other) { throw null; }

        public readonly bool Equals(ProjectorId obj) { throw null; }

        public override readonly bool Equals(object? obj) { throw null; }

        public static ProjectorId FromIdentity(Identity rawIdentity) { throw null; }

        public static ProjectorId FromString(string id) { throw null; }

        public override readonly int GetHashCode() { throw null; }

        public static bool operator ==(ProjectorId left, ProjectorId right) { throw null; }

        public static explicit operator ProjectorId(Identity id) { throw null; }

        public static explicit operator Identity(ProjectorId projectorId) { throw null; }

        public static bool operator >(ProjectorId left, ProjectorId right) { throw null; }

        public static bool operator >=(ProjectorId left, ProjectorId right) { throw null; }

        public static implicit operator string?(ProjectorId projectorId) { throw null; }

        public static implicit operator ProjectorId(string id) { throw null; }

        public static bool operator !=(ProjectorId left, ProjectorId right) { throw null; }

        public static bool operator <(ProjectorId left, ProjectorId right) { throw null; }

        public static bool operator <=(ProjectorId left, ProjectorId right) { throw null; }

        public static Identity ToIdentity(ProjectorId projectorId) { throw null; }

        public override readonly string? ToString() { throw null; }

        public readonly string ToString(string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed partial class ProjectorIdAttribute : System.Attribute
    {
        public ProjectorIdAttribute(string name, uint version) { }

        public ProjectorIdAttribute(string name) { }

        public string Name { get { throw null; } }

        public uint Version { get { throw null; } set { } }
    }

    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed partial class ProviderBuilderAttribute : System.Attribute
    {
        public ProviderBuilderAttribute(string name, string kind, string target, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type) { }

        public string Kind { get { throw null; } }

        public string Name { get { throw null; } }

        public string Target { get { throw null; } }

        public System.Type Type { get { throw null; } }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed partial class QueryEventTypesAttribute : System.Attribute
    {
        public QueryEventTypesAttribute(int queryId, params string[] eventTypes) { }

        public QueryEventTypesAttribute(params string[] eventTypes) { }

        public System.Collections.Generic.IEnumerable<string> EventTypes { get { throw null; } }

        public int QueryId { get { throw null; } }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed partial class QueryTagParametersAttribute : System.Attribute
    {
        public QueryTagParametersAttribute(int queryId, params string[] tagParameters) { }

        public QueryTagParametersAttribute(params string[] tagParameters) { }

        public int QueryId { get { throw null; } }

        public System.Collections.Generic.IEnumerable<string> TagParameters { get { throw null; } }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed partial class QueryTagsAttribute : System.Attribute
    {
        public QueryTagsAttribute(int queryId, params string[] eventTags) { }

        public QueryTagsAttribute(params string[] eventTags) { }

        public System.Collections.Generic.IEnumerable<string> EventTags { get { throw null; } }

        public int QueryId { get { throw null; } }
    }

    public static partial class StableHash
    {
        public static int ComputeHash(System.ReadOnlySpan<byte> data) { throw null; }
    }

    public readonly partial struct DecideResult
    {
        readonly object? _value;
        readonly string? _errorMessage;

        public DecideResult(DcbEvent @event) { throw null; }

        public DecideResult(DcbEvent @event, params System.ReadOnlySpan<DcbEvent> events) { throw null; }

        public DecideResult(string errorCode, string errorMessage) { throw null; }

        public int Count { get { throw null; } }

        public DcbEvent this[int index] { get { throw null; } }

        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "_value")]
        public bool IsSuccess { get { throw null; } }

        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "_errorMessage", "_value")]
        public bool IsFailure { get { throw null; } }

        public Error Error { get { throw null; } }

        public Enumerator<DcbEvent> GetEnumerator() { throw null; }

        public static DecideResult Success(DcbEvent @event, params ReadOnlySpan<DcbEvent> events) { throw null; }

        public static DecideResult Failure(string code, string message) { throw null; }

        public static implicit operator DecideResult(DcbEvent @event) { throw null; }

        public static implicit operator DecideResult((DcbEvent, DcbEvent) events) { throw null; }

        public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent) events) { throw null; }

        public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) { throw null; }

        public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) { throw null; }

        public static implicit operator DecideResult(Error error) => new(error.Code, error.Message);

        public static implicit operator DecideResult((string Code, string Message) error) => new(error.Code, error.Message);
    }

    public readonly partial struct Tag : System.IEquatable<Tag>, System.IComparable<Tag>, System.ISpanFormattable, System.IFormattable, System.IUtf8SpanFormattable
    {
        public Tag(string name) { }

        public bool IsDefault { get { throw null; } }

        public readonly System.ReadOnlySpan<byte> AsSpan() { throw null; }

        public readonly int CompareTo(Tag other) { throw null; }

        public readonly bool Equals(Tag obj) { throw null; }

        public override readonly bool Equals(object? obj) { throw null; }

        public static Tag FromIdentity(Identity rawIdentity) { throw null; }

        public static Tag FromString(string name) { throw null; }

        public override readonly int GetHashCode() { throw null; }

        public static bool operator ==(Tag left, Tag right) { throw null; }

        public static explicit operator Tag(Identity id) { throw null; }

        public static explicit operator Identity(Tag tag) { throw null; }

        public static bool operator >(Tag left, Tag right) { throw null; }

        public static bool operator >=(Tag left, Tag right) { throw null; }

        public static implicit operator string?(Tag tag) { throw null; }

        public static implicit operator Tag(string name) { throw null; }

        public static bool operator !=(Tag left, Tag right) { throw null; }

        public static bool operator <(Tag left, Tag right) { throw null; }

        public static bool operator <=(Tag left, Tag right) { throw null; }

        public static Identity ToIdentity(Tag tag) { throw null; }

        public override readonly string? ToString() { throw null; }

        public readonly string ToString(string? format, System.IFormatProvider? formatProvider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }

        public readonly bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed partial class TypeManifestProviderAttribute : System.Attribute
    {
        public TypeManifestProviderAttribute(System.Type providerType) { }

        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public System.Type ProviderType { get { throw null; } }
    }
}

namespace Filin.Configuration
{
    public partial interface IStorageProviderSerializerOptions
    {
        Storage.IStorageSerializer StorageSerializer { get; set; }
    }

    public sealed partial class TypeManifestOptions
    {
        public System.Collections.Generic.Dictionary<(System.Type Command, System.Type State), System.Type> DeciderTypes { get { throw null; } }

        public System.Collections.Generic.Dictionary<EventType, System.Type> EventTypes { get { throw null; } }

        public System.Collections.Generic.Dictionary<ProjectorId, System.Type> ProjectionTypes { get { throw null; } }

        public System.Collections.Generic.Dictionary<System.Type, System.Type> ProjectorTypes { get { throw null; } }

        public System.Collections.Generic.HashSet<object> TypeManifestProviders { get { throw null; } }
    }
}

namespace Filin.Hosting
{
    public partial interface ITypeManifestProvider
    {
        void Configure(Configuration.TypeManifestOptions options);
    }

    public abstract partial class TypeManifestProviderBase : ITypeManifestProvider
    {
        public void Configure(Configuration.TypeManifestOptions options) { }

        protected abstract void ConfigureInner(Configuration.TypeManifestOptions options);
    }
}

namespace Filin.Storage
{
    public partial struct AppendResult
    {
        public static readonly AppendResult Conflict;

        public AppendResult(DcbEventSequenced @event) { throw null; }

        public AppendResult(DcbEventSequenced @event, params ReadOnlySpan<DcbEventSequenced> events) { throw null; }

        public DcbEventSequenced this[int index] { get { throw null; } }

        public readonly int Count { get { throw null; } }

        public readonly bool IsSuccess { get { throw null; } }

        public readonly bool IsConflict { get { throw null; } }

        public readonly Enumerator GetEnumerator() { throw null; }

        public ref struct Enumerator
        {
            public bool MoveNext() { throw null; }

            public readonly DcbEventSequenced Current { get { throw null; } }
        }
    }

    public partial interface IEventStore
    {
        System.Threading.Tasks.Task<EventPosition> AppendAsync(DcbEvent domainEvent, DcbQuery failIfEventsMatch, EventPosition after, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<EventPosition> AppendAsync(DcbEvent domainEvent, DcbQuery failIfEventsMatch, System.Threading.CancellationToken cancellationToken = default) => AppendAsync(domainEvent, failIfEventsMatch, default, cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<EventPosition>> AppendAsync(System.Collections.Generic.IEnumerable<DcbEvent> domainEvents, DcbQuery failIfEventsMatch, EventPosition after, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<EventPosition>> AppendAsync(System.Collections.Generic.IEnumerable<DcbEvent> domainEvents, DcbQuery failIfEventsMatch, System.Threading.CancellationToken cancellationToken = default) => AppendAsync(domainEvents, failIfEventsMatch, default, cancellationToken);
        System.Collections.Generic.IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition afterPosition);
        System.Collections.Generic.IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query) => ReadAsync(query, default);
        System.Collections.Generic.IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition afterPosition);
        System.Collections.Generic.IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query);
    }

    public partial interface IProjectionStore
    {
        System.Threading.Tasks.Task ClearAsync(ProjectionId projectionId, EventPosition position, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<Projection?> LoadAsync(ProjectionId projectionId, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task SaveAsync(Projection projection, bool create, System.Threading.CancellationToken cancellationToken = default);
    }

    public partial interface IStorageSerializer
    {
        object Deserialize(System.Type returnType, System.Span<byte> input);
        System.Span<byte> Serialize(object input);
    }

    public sealed partial record Projection()
    {
        public ProjectionId Id { get { throw null; } init { } }

        public required object Payload { get { throw null; } init { } }

        public EventPosition Position { get { throw null; } init { } }

        public System.DateTimeOffset Timestamp { get { throw null; } init { } }
    }
}
