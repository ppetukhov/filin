namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class EventTypeNameComparer : IEqualityComparer<EventType>
    {
        internal static readonly EventTypeNameComparer Instance = new EventTypeNameComparer();

        public bool Equals(EventType x, EventType y) => string.Equals(x.TargetType.FullName, y.TargetType.FullName, StringComparison.Ordinal);

        public int GetHashCode(EventType obj) => obj.TargetType.FullName.GetHashCode();
    }
}
