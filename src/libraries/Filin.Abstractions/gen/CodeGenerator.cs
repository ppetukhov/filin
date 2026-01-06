using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Filin.Generators.Specs.Members;
using Filin.Generators.Specs.Types;

namespace Filin.Generators;

#pragma warning disable RS1038
[Generator(LanguageNames.CSharp)]
#pragma warning restore RS1038
public sealed partial class CodeGenerator : IIncrementalGenerator
{
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

        var eventTypes = EventTypesParser.ParseEventTypes(context.SyntaxProvider);

        context.RegisterSourceOutput(eventTypes.Collect().Combine(typeDefsAndOptions), EmitEventTypes);

        var eventTags = EventTagsParser.ParseEventTags(context.SyntaxProvider);

        context.RegisterImplementationSourceOutput(eventTags.Collect().Combine(typeDefsAndOptions), EmitEventTags);

        var queries = QueryParser.ParseQueries(context.SyntaxProvider);

        context.RegisterImplementationSourceOutput(queries.Combine(generationOptions), EmitQueries);

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

    void EmitQueries(SourceProductionContext context, (ImmutableArray<Query>, GenerationOptions) input)
    {
        var (items, options) = input;

        if (!items.IsDefaultOrEmpty)
        {
            var emitter = new QueryEmitter(options);

            emitter.EmitQueries(context, items);
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
