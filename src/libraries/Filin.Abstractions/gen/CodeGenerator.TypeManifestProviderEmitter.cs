using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SourceGenerators;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class TypeManifestProviderEmitter(TypeDefinitions typeDefinitions)
    {
        static readonly IdentifierNameSyntax OptionsParam = IdentifierName(Identifier("options"));
        static readonly MemberAccessExpressionSyntax AddEventTypesMethod = OptionsParam.Member("EventTypes").Member("Add");
        static readonly MemberAccessExpressionSyntax AddProjectorsMethod = OptionsParam.Member("ProjectorTypes").Member("Add");
        static readonly MemberAccessExpressionSyntax AddProjectionMethod = OptionsParam.Member("ProjectionTypes").Member("Add");
        static readonly MemberAccessExpressionSyntax AddDeciderTypesMethod = OptionsParam.Member("DeciderTypes").Member("Add");

        internal ClassDeclarationSyntax CreateTypeManifestProvider(ImmutableArray<EventType> eventTypes)
        {
            var body = new List<StatementSyntax>(capacity: eventTypes.Length);

            foreach (var evt in eventTypes)
            {
                body.Add(ExpressionStatement(InvocationExpression(AddEventTypesMethod,
                    ArgumentList(SeparatedList([
                        Argument(Emitter.EventTypesGeneratedNameSyntax.Member(evt.FieldName)),
                        Argument(TypeOfExpression(ParseTypeName(evt.TargetType.FullName)))
                    ])))));
            }

            return GenerateClass(CreateClassName("EventTypesTypeManifestProvider", typeDefinitions.AssemblyName), body);
        }

        internal ClassDeclarationSyntax CreateTypeManifestProvider(ImmutableArray<ProjectorId> projectorIds)
        {
            var body = new List<StatementSyntax>(capacity: projectorIds.Length);
            var body2 = new List<StatementSyntax>(capacity: projectorIds.Length);

            foreach (var item in projectorIds.Where(x => x.TargetType.Type.Interfaces.Any(typeDefinitions.IsProjector)))
            {
                var foundInterface = typeDefinitions.FindIProjector(item.TargetType.Type);

                if (foundInterface is null)
                {
                    continue;
                }

                var stateType = foundInterface.TypeArguments[0];

                body.Add(ExpressionStatement(InvocationExpression(AddProjectorsMethod,
                    ArgumentList(SeparatedList([
                        Argument(TypeOfExpression(ParseTypeName(stateType.GetFullyQualifiedName()))),
                                            Argument(TypeOfExpression(ParseTypeName(item.TargetType.FullName)))
                    ])))));

                body2.Add(ExpressionStatement(InvocationExpression(AddProjectionMethod,
                    ArgumentList(SeparatedList([
                        Argument(ParseTypeName(item.TargetType.FullName).Member("ProjectorId")),
                                            Argument(TypeOfExpression(ParseTypeName(stateType.GetFullyQualifiedName())))
                    ])))));
            }

            body.AddRange(body2);

            return GenerateClass(CreateClassName("ProjectorsTypeManifestProvider", typeDefinitions.AssemblyName), body);
        }

        internal ClassDeclarationSyntax CreateTypeManifestProvider(ImmutableArray<Decider> deciders)
        {
            var body = new List<StatementSyntax>(capacity: deciders.Length);

            foreach (var item in deciders.Where(x => x.TargetType.Type.Interfaces.Any(typeDefinitions.IsDecider)))
            {
                var foundInterface = typeDefinitions.FindIDecider(item.TargetType.Type);

                if (foundInterface is null)
                {
                    continue;
                }

                var commandType = foundInterface.TypeArguments[0];
                var stateType = foundInterface.TypeArguments[1];

                body.Add(ExpressionStatement(InvocationExpression(AddDeciderTypesMethod,
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

            return GenerateClass(CreateClassName("DecidersTypeManifestProvider", typeDefinitions.AssemblyName), body);
        }

        static ClassDeclarationSyntax GenerateClass(string className, List<StatementSyntax> body)
        {
            var configureInnerMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "ConfigureInner")
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .AddParameterListParameters(
                    Parameter(OptionsParam.Identifier).WithType(ParseTypeName(Constants.TypeManifestOptionsFullName)))
                .AddBodyStatements([.. body]);

            return ClassDeclaration(className)
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(Constants.TypeManifestProviderBaseFullName)))
                .AddModifiers(Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(Emitter.GeneratedCodeAttributeSyntax)
                .AddMembers(configureInnerMethod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string CreateClassName(string classPrefix, string assemblyName) => Emitter.SanitizeIdentifierNameByEmptyString(assemblyName) + classPrefix;
    }
}
