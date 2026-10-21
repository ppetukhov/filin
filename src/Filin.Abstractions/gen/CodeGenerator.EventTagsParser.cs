using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Filin.Generators.Specs;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filin.Generators;

partial class CodeGenerator
{
    static class EventTagsParser
    {
        internal static IncrementalValuesProvider<EventTag> ParseEventTags(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(Constants.EventTagAttribute, IsValidEventTagNode, ConvertToEventTags)
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; })
                .Collect()
                .SelectMany(static (eventTags, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    return eventTags
                        .Sort(EventTagComparer.Instance)
                        .Distinct(EventTagComparer.Instance).ToImmutableArray();
                }).WithTrackingName($"select_{nameof(EventTag)}");

        static bool IsValidEventTagNode(SyntaxNode node, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

#pragma warning disable IDE0072 // Add missing cases
            return node.Kind() switch
            {
                SyntaxKind.Parameter when node.Parent?.Parent is SyntaxNode pNode && pNode.IsKind(SyntaxKind.RecordDeclaration) => true,
                SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.PropertyDeclaration or SyntaxKind.GetAccessorDeclaration => true,
                _ => false
            };
#pragma warning restore IDE0072 // Add missing cases
        }

        static ImmutableArray<EventTag> ConvertToEventTags(GeneratorAttributeSyntaxContext ctx, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var targetSymbol = ctx.TargetSymbol;

            ObjectSpec targetType;
            MemberSpec? member;

            if (targetSymbol is IParameterSymbol parameterSymbol)
            {
                targetType = new ObjectSpec(parameterSymbol.ContainingType);

                member = new ParameterSpec(parameterSymbol);
            }
            else if (targetSymbol is IPropertySymbol propertySymbol)
            {
                targetType = new ObjectSpec(propertySymbol.ContainingType);

                member = new PropertySpec(propertySymbol);
            }
            else if (targetSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                targetType = new ObjectSpec(namedTypeSymbol);
                member = null;
            }
            else
            {
                return [];
            }

            var array = ImmutableArray.CreateBuilder<EventTag>();

            foreach (var attr in ctx.Attributes)
            {
                token.ThrowIfCancellationRequested();

                var tag = attr.ConstructorArguments[0].Value is string str ? str.Trim() : string.Empty;

                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                var fieldName = Emitter.SanitizeIdentifierName(tag);

                var evt = member is null
                    ? new EventTag(FieldName: fieldName, TargetType: targetType, Tag: tag)
                    : new EventTagMember(FieldName: fieldName, TargetType: targetType, Tag: tag, Member: member);

                array.Add(evt);
            }

            return array.ToImmutable();
        }

        sealed class EventTagComparer : IEqualityComparer<EventTag>, IComparer<EventTag>
        {
            internal static readonly EventTagComparer Instance = new();

            public int Compare(EventTag x, EventTag y)
            {
                var tagCmp = string.CompareOrdinal(x.Tag, y.Tag);

                if (tagCmp is not 0)
                {
                    return tagCmp;
                }

                var targetCmp = string.CompareOrdinal(x.TargetType.IdentifierCompatibleSubstring, y.TargetType.IdentifierCompatibleSubstring);

                return targetCmp is 0
                    ? targetCmp
                    : (x, y) switch
                    {
                        (EventTagMember xMember, EventTagMember yMember) => string.CompareOrdinal(xMember.Member.Name, yMember.Member.Name),
                        (EventTagMember, _) => -1,
                        (_, EventTagMember) => 1,
                        _ => targetCmp
                    };
            }

            public bool Equals(EventTag x, EventTag y)
                => string.Equals(x.Tag, y.Tag, StringComparison.Ordinal) && (x, y) switch
                {
                    (EventTagMember xMember, EventTagMember yMember) => string.Equals(xMember.Member.Name, yMember.Member.Name, StringComparison.Ordinal) &&
                            string.Equals(xMember.TargetType.IdentifierCompatibleSubstring, yMember.TargetType.IdentifierCompatibleSubstring, StringComparison.Ordinal),
                    (EventTagMember, _) => false,
                    (_, EventTagMember) => false,
                    _ => string.Equals(x.TargetType.IdentifierCompatibleSubstring, x.TargetType.IdentifierCompatibleSubstring, StringComparison.Ordinal)
                };

            public int GetHashCode(EventTag obj)
                => obj is EventTagMember objMember
                    ? HashCode(objMember.Member.Name, obj.Tag, obj.TargetType.IdentifierCompatibleSubstring)
                    : HashCode(obj.Tag, obj.TargetType.IdentifierCompatibleSubstring);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int HashCode(string x, string y)
            {
                var hashCode = -2088324004;
                hashCode = (hashCode * -1521134295) + x.GetHashCode();
                hashCode = (hashCode * -1521134295) + y.GetHashCode();

                return hashCode;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int HashCode(string x, string y, string z)
            {
                var hashCode = -2088324004;
                hashCode = (hashCode * -1521134295) + x.GetHashCode();
                hashCode = (hashCode * -1521134295) + y.GetHashCode();
                hashCode = (hashCode * -1521134295) + z.GetHashCode();

                return hashCode;
            }
        }
    }
}
