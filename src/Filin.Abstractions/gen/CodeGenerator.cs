using System.Collections.Immutable;

using Filin.Generators.Specs;

using Microsoft.CodeAnalysis;

namespace Filin.Generators;

#pragma warning disable RS1038
[Generator(LanguageNames.CSharp)]
#pragma warning restore RS1038
sealed partial class CodeGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if LAUNCH_DEBUGGER
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Launch();
        }
#endif
        var generationOptions = context.AnalyzerConfigOptionsProvider.Select(GenerationOptions.GetGenerationOptions);

        var typeDefsAndOptions = context.CompilationProvider.Select(static (x, _) => new TypeDefinitions(x)).Combine(generationOptions);

        var eventTypes = EventTypesParser.ParseEventTypes(context.SyntaxProvider).Collect();

        context.RegisterSourceOutput(eventTypes.Combine(typeDefsAndOptions), EmitEventTypes);

        var eventTags = EventTagsParser.ParseEventTags(context.SyntaxProvider).Collect();

        context.RegisterImplementationSourceOutput(eventTags.Combine(typeDefsAndOptions), EmitEventTags);

        context.RegisterImplementationSourceOutput(eventTypes.Combine(eventTags).Combine(typeDefsAndOptions), EmitDcbEvents);

        var queries = QueryParser.ParseQueries(context.SyntaxProvider);

        context.RegisterImplementationSourceOutput(queries, EmitQueries);

        var projectorIds = ProjectorIdParser.ParseEventTags(context.SyntaxProvider);

        context.RegisterImplementationSourceOutput(projectorIds.Collect().Combine(typeDefsAndOptions), EmitProjectorIds);

        var deciders = DeciderParser.ParseDeciders(context.SyntaxProvider);

        context.RegisterImplementationSourceOutput(deciders.Collect().Combine(typeDefsAndOptions), EmitDeciders);
    }

    void EmitEventTags(SourceProductionContext context, (ImmutableArray<EventTag>, (TypeDefinitions, GenerationOptions)) input)
    {
        var (items, (typeDefinitions, options)) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new EventTagsEmitter(typeDefinitions, options);
            emitter.EmitEventTags(context, items);
        }
    }

    void EmitEventTypes(SourceProductionContext context, (ImmutableArray<EventType>, (TypeDefinitions, GenerationOptions)) input)
    {
        var (items, (typeDefinitions, options)) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new EventTypesEmitter(typeDefinitions, options);

            emitter.EmitEventTypes(context, items);
        }
    }

    void EmitDcbEvents(SourceProductionContext context, ((ImmutableArray<EventType>, ImmutableArray<EventTag>), (TypeDefinitions, GenerationOptions)) input)
    {
        var ((items, eventTags), (_, options)) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new DcbEventEmitter(options);

            emitter.Emit(context, items, eventTags);
        }
    }

    static void EmitQueries(SourceProductionContext context, ImmutableArray<Query> items)
    {
        if (!items.IsDefaultOrEmpty)
        {
            QueryEmitter.EmitQueries(context, items);
        }
    }

    void EmitProjectorIds(SourceProductionContext context, (ImmutableArray<ProjectorId>, (TypeDefinitions, GenerationOptions)) input)
    {
        var (items, (typeDefinitions, options)) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new ProjectorIdEmitter(typeDefinitions, options);
            emitter.EmitProjectIds(context, items);
        }
    }

    void EmitDeciders(SourceProductionContext context, (ImmutableArray<Decider>, (TypeDefinitions, GenerationOptions)) input)
    {
        var (items, (typeDefinitions, options)) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new DeciderEmitter(typeDefinitions, options);
            emitter.EmitDeciders(context, items);
        }
    }

    sealed record EventTagMember(string FieldName, ObjectSpec TargetType, string Tag, MemberSpec Member) : EventTag(FieldName: FieldName, TargetType: TargetType, Tag: Tag);

    record EventTag(string FieldName, ObjectSpec TargetType, string Tag);

    sealed record EventType(string FieldName, ObjectSpec TargetType, string EventTypeName, (string EventTypeName, uint Version) Raw);

    sealed record QueryItem(ImmutableArray<string> EventTypes, ImmutableArray<string> Tags);

    sealed record QueryParametrized(ObjectSpec TargetType, ImmutableArray<QueryItem> Items, ImmutableArray<TagParameter> TagParameters) : Query(TargetType, Items);

    record Query(ObjectSpec TargetType, ImmutableArray<QueryItem> Items);

    sealed record TagParameter(string TagParam, ImmutableArray<int> QueryItemIndex);

    sealed record ProjectorId(ObjectSpec TargetType, string Id, (string ProjectorName, uint Version) Raw);

    sealed record Decider(ObjectSpec TargetType);
}
