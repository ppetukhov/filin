using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Filin.Generators.Specs;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filin.Generators;

partial class CodeGenerator
{
    static class QueryParser
    {
        internal static IncrementalValueProvider<ImmutableArray<Query>> ParseQueries(SyntaxValueProvider syntaxValueProvider)
        {
            var queryEventTypes = ParseQueryEventTypes(syntaxValueProvider).Collect();

            var queryTags = ParseQueryTags(syntaxValueProvider).Collect();

            var queryTagParameters = ParseQueryTagParameters(syntaxValueProvider).Collect();

            return queryEventTypes.Combine(queryTags).Combine(queryTagParameters).Select(ConvertToQuery);
        }

        static IncrementalValuesProvider<QueryEventTypes> ParseQueryEventTypes(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(Constants.QueryEventTypesAttribute, IsValidNode, ConvertToQueryEventTypes)
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; });

        static IncrementalValuesProvider<QueryTags> ParseQueryTags(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(Constants.QueryTagsAttribute, IsValidNode, ConvertToQueryTags)
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; });

        static IncrementalValuesProvider<QueryTagParameters> ParseQueryTagParameters(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(Constants.QueryTagParametersAttribute, IsValidNode, ConvertToQueryTagParameters)
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; });

        static ImmutableArray<Query> ConvertToQuery(((ImmutableArray<QueryEventTypes>, ImmutableArray<QueryTags>), ImmutableArray<QueryTagParameters>) input, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var ((eventTypes, tags), tagParams) = input;

            var dict = new Dictionary<(ObjectSpec TargetType, int QueryId), (ImmutableArray<string>.Builder EventTypes, ImmutableArray<string>.Builder Tags, ImmutableArray<string>.Builder TagParams)>(TypeSpecWithQueryIdEqualityComparer.Instance);

            foreach (var (targetType, queryId, values) in eventTypes)
            {
                var key = (targetType, queryId);

                if (dict.TryGetValue(key, out var exist))
                {
                    exist.EventTypes.AddRange(values);
                }
                else
                {
                    dict[key] = (EventTypes: values.ToBuilder(), Tags: ImmutableArray.CreateBuilder<string>(), TagParams: ImmutableArray.CreateBuilder<string>());
                }
            }

            token.ThrowIfCancellationRequested();

            foreach (var (targetType, queryId, values) in tags)
            {
                var key = (targetType, queryId);

                if (dict.TryGetValue(key, out var exist))
                {
                    exist.Tags.AddRange(values);
                }
                else
                {
                    dict[key] = (EventTypes: ImmutableArray.CreateBuilder<string>(), Tags: values.ToBuilder(), TagParams: ImmutableArray.CreateBuilder<string>());
                }
            }

            token.ThrowIfCancellationRequested();

            foreach (var (targetType, queryId, values) in tagParams)
            {
                var key = (targetType, queryId);

                if (dict.TryGetValue(key, out var exist))
                {
                    exist.TagParams.AddRange(values);
                }
                else
                {
                    dict[key] = (EventTypes: ImmutableArray.CreateBuilder<string>(), Tags: ImmutableArray.CreateBuilder<string>(), TagParams: values.ToBuilder());
                }
            }

            token.ThrowIfCancellationRequested();

            var arr = ImmutableArray.CreateBuilder<Query>();

            foreach (var items in dict.GroupBy(static kv => kv.Key.TargetType, ObjectSpecEqualityComparer.Instance).OrderBy(x => x.Key.FullName, StringComparer.Ordinal))
            {
                var targetType = items.Key;

                var queryItems = items.Select(x => new QueryItem(EventTypes: x.Value.EventTypes.ToImmutable(), Tags: x.Value.Tags.ToImmutable())).ToImmutableArray();

                Query query;

                if (items.All(static x => x.Value.TagParams is { Count: 0 }))
                {
                    query = new Query(TargetType: targetType, Items: queryItems);
                }
                else
                {
                    var tagParameters = items.Aggregate(seed: (Dict: new Dictionary<string, ImmutableArray<int>.Builder>(StringComparer.Ordinal), Index: 0), func: static (result, item) =>
                    {
                        var (dict, index) = result;

                        foreach (var tagParam in item.Value.TagParams)
                        {
                            if (dict.TryGetValue(tagParam, out var listIdx))
                            {
                                listIdx.Add(index);
                            }
                            else
                            {
                                var builder = ImmutableArray.CreateBuilder<int>();

                                builder.Add(index);

                                dict[tagParam] = builder;
                            }
                        }

                        return (Dict: dict, Index: index + 1);
                    }).Dict
                        .Select(static x => new TagParameter(TagParam: x.Key, QueryItemIndex: x.Value.ToImmutable()))
                        .OrderBy(static x => x.TagParam) // for stable ProjectionId
                        .ToImmutableArray();

                    query = new QueryParametrized(TargetType: targetType, Items: queryItems, TagParameters: tagParameters);
                }

                arr.Add(query);
            }

            return arr.ToImmutable();
        }

        static bool IsValidNode(SyntaxNode node, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

#pragma warning disable IDE0072 // Add missing cases
            return node.Kind() switch
            {
                SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration => true,
                _ => false
            };
#pragma warning restore IDE0072 // Add missing cases
        }

        static ImmutableArray<QueryEventTypes> ConvertToQueryEventTypes(GeneratorAttributeSyntaxContext ctx, CancellationToken token)
        {
            static QueryEventTypes Factory(ObjectSpec targetType, int queryId, ImmutableArray<string> eventTypes)
                => new(targetType, queryId, eventTypes);

            return ConvertCore(ctx, Factory, token);
        }

        static ImmutableArray<QueryTags> ConvertToQueryTags(GeneratorAttributeSyntaxContext ctx, CancellationToken token)
        {
            static QueryTags Factory(ObjectSpec targetType, int queryId, ImmutableArray<string> tags)
                => new(targetType, queryId, tags);

            return ConvertCore(ctx, Factory, token);
        }

        static ImmutableArray<QueryTagParameters> ConvertToQueryTagParameters(GeneratorAttributeSyntaxContext ctx, CancellationToken token)
        {
            static QueryTagParameters Factory(ObjectSpec targetType, int queryId, ImmutableArray<string> tagParameters)
                => new(targetType, queryId, tagParameters);

            return ConvertCore(ctx, Factory, token);
        }

        static ImmutableArray<T> ConvertCore<T>(GeneratorAttributeSyntaxContext ctx, Func<ObjectSpec, int, ImmutableArray<string>, T> factory, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var targetSymbol = ctx.TargetSymbol;

            ObjectSpec targetType;

            if (targetSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                targetType = new ObjectSpec(namedTypeSymbol);
            }
            else
            {
                return [];
            }

            var array = ImmutableArray.CreateBuilder<T>();

            foreach (var attr in ctx.Attributes)
            {
                token.ThrowIfCancellationRequested();

                var (queryId, stringArr) = attr.ConstructorArguments.Length switch
                {
                    1 => (0, attr.ConstructorArguments[0].Values),
                    _ => ((int)attr.ConstructorArguments[0].Value!, attr.ConstructorArguments[1].Values)
                };

                var items = stringArr
                    .Select(static x => x is { IsNull: false } tpConst ? ((string)tpConst.Value!).Trim() : string.Empty)
                    .Where(static x => !string.IsNullOrWhiteSpace(x))
                    .ToImmutableArray();

                if (items is { IsDefaultOrEmpty: true })
                {
                    continue;
                }

                array.Add(factory(targetType, queryId, items));
            }

            return array.ToImmutable();
        }

        sealed class ObjectSpecEqualityComparer : IEqualityComparer<ObjectSpec>
        {
            internal static readonly ObjectSpecEqualityComparer Instance = new();

            public bool Equals(ObjectSpec x, ObjectSpec y) => TypeSpecComparer.Instance.Equals(x, y);

            public int GetHashCode(ObjectSpec obj) => TypeSpecComparer.Instance.GetHashCode(obj);
        }

        sealed class TypeSpecWithQueryIdEqualityComparer : IEqualityComparer<(ObjectSpec TargetType, int QueryId)>
        {
            internal static readonly TypeSpecWithQueryIdEqualityComparer Instance = new();

            public bool Equals((ObjectSpec TargetType, int QueryId) x, (ObjectSpec TargetType, int QueryId) y)
                => x.QueryId == y.QueryId && TypeSpecComparer.Instance.Equals(x.TargetType, y.TargetType);

            public int GetHashCode((ObjectSpec TargetType, int QueryId) obj) => HashCode(TypeSpecComparer.Instance.GetHashCode(obj.TargetType), obj.QueryId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int HashCode<T1, T2>(T1 x, T2 y)
            where T1 : notnull
            where T2 : notnull
        {
            var hashCode = -2088324004;
            hashCode = (hashCode * -1521134295) + x.GetHashCode();
            hashCode = (hashCode * -1521134295) + y.GetHashCode();

            return hashCode;
        }

        sealed record QueryEventTypes(ObjectSpec TargetType, int QueryId, ImmutableArray<string> EventTypes);

        sealed record QueryTags(ObjectSpec TargetType, int QueryId, ImmutableArray<string> Tags);

        sealed record QueryTagParameters(ObjectSpec TargetType, int QueryId, ImmutableArray<string> TagParameters);
    }
}
