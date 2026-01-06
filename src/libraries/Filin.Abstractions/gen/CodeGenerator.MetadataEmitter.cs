using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Filin.Generators.Extensions;

using SourceGenerators;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

sealed partial class CodeGenerator
{
    sealed class MetadataEmitter(TypeDefinitions typeDefinitions)
    {
        internal ClassDeclarationSyntax GenerateMetadata(ImmutableArray<EventType> eventTypes)
        {
            var optionsParam = IdentifierName(Identifier("options"));
            var addEventTypesMethod = optionsParam.Member("EventTypes").Member("Add");

            var body = new List<StatementSyntax>();

            foreach (var evt in eventTypes)
            {
                body.Add(ExpressionStatement(InvocationExpression(addEventTypesMethod,
                    ArgumentList(SeparatedList([
                        Argument(Emitter.EventTypesGeneratedNameSyntax.Member(evt.FieldName)),
                        Argument(TypeOfExpression(ParseTypeName(evt.TargetType.FullName)))
                    ])))));
            }

            return GenerateClass("MetadataEventTypes_", optionsParam, body);
        }

        internal ClassDeclarationSyntax GenerateMetadata(ImmutableArray<ProjectorId> projectorIds)
        {
            var optionsParam = IdentifierName(Identifier("options"));
            var addProjectorsMethod = optionsParam.Member("ProjectorTypes").Member("Add");
            var addProjectionMethod = optionsParam.Member("ProjectionTypes").Member("Add");

            var body = new List<StatementSyntax>();
            var body2 = new List<StatementSyntax>();

            foreach (var item in projectorIds.Where(x => x.TargetType.type.Interfaces.Any(typeDefinitions.IsProjector)))
            {
                var foundInterface = typeDefinitions.FindIProjector(item.TargetType.Type);

                if (foundInterface is null)
                {
                    continue;
                }

                var stateType = foundInterface.TypeArguments[0];

                body.Add(ExpressionStatement(InvocationExpression(addProjectorsMethod,
                    ArgumentList(SeparatedList([
                        Argument(TypeOfExpression(ParseTypeName(stateType.GetFullyQualifiedName()))),
                                            Argument(TypeOfExpression(ParseTypeName(item.TargetType.FullName)))
                    ])))));

                body2.Add(ExpressionStatement(InvocationExpression(addProjectionMethod,
                    ArgumentList(SeparatedList([
                        Argument(ParseTypeName(item.TargetType.FullName).Member("ProjectorId")),
                                            Argument(TypeOfExpression(ParseTypeName(stateType.GetFullyQualifiedName())))
    ])))));
            }

            body.AddRange(body2);

            return GenerateClass("MetadataProjectors_", optionsParam, body);
        }

        internal ClassDeclarationSyntax GenerateMetadata(ImmutableArray<Decider> deciders)
        {
            var optionsParam = IdentifierName(Identifier("options"));
            var addDeciderTypesMethod = optionsParam.Member("DeciderTypes").Member("Add");

            var body = new List<StatementSyntax>();

            foreach (var item in deciders.Where(x => x.TargetType.type.Interfaces.Any(typeDefinitions.IsDecider)))
            {
                var foundInterface = typeDefinitions.FindIDecider(item.TargetType.Type);

                if (foundInterface is null)
                {
                    continue;
                }

                var commandType = foundInterface.TypeArguments[0];
                var stateType = foundInterface.TypeArguments[1];

                body.Add(ExpressionStatement(InvocationExpression(addDeciderTypesMethod,
                    ArgumentList(SeparatedList([
                        Argument(
                            TupleExpression(
                                SeparatedList([
                                    Argument(TypeOfExpression(ParseTypeName(commandType.GetFullyQualifiedName()))),
                                    Argument(TypeOfExpression(ParseTypeName(stateType.GetFullyQualifiedName())))
                                ])
                            )
                        ),
                        Argument(TypeOfExpression(ParseTypeName(item.TargetType.FullName)))
                    ])))));
            }

            return GenerateClass("MetadataDeciders_", optionsParam, body);
        }

        ClassDeclarationSyntax GenerateClass(string classPrefix, IdentifierNameSyntax optionsParam, List<StatementSyntax> body)
        {
            var configureMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "ConfigureInner")
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .AddParameterListParameters(
                    Parameter(optionsParam.Identifier).WithType(ParseTypeName(Constants.TypeManifestOptionsFullName)))
                .AddBodyStatements([.. body]);

            return ClassDeclaration(classPrefix + Emitter.SanitizeIdentifierName(typeDefinitions.AssemblyName))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(Constants.TypeManifestProviderBaseFullName)))
                .AddModifiers(Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(Emitter.GeneratedCodeAttributeSyntax)
                .AddMembers(configureMethod);
        }
    }
}
