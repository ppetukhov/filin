using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed partial class EventTypesEmitter(TypeDefinitions typeDefinitions, GenerationOptions options)
    {
        static readonly ClassDeclarationSyntax EventTypesGeneratedSyntax = ClassDeclaration(
            attributeLists: SingletonList(Emitter.GeneratedCodeAttributeSyntax),
            modifiers: TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword)),
            identifier: Emitter.EventTypesGeneratedToken,
            typeParameterList: null,
            parameterList: null,
            baseList: null,
            constraintClauses: default,
            members: default);

        static readonly SyntaxToken GetEventTypeMethod = Identifier(Constants.GetEventTypeMethod);

        readonly TypeManifestProviderEmitter _metadataEmitter = new(typeDefinitions);
        readonly string _namespaceMetadata = Emitter.GetConfigurationNamespace(options);

        readonly ClassDeclarationSyntax _eventTypesGeneratedExtensionsClassNameSyntax = ClassDeclaration(
            attributeLists: SingletonList(Emitter.GeneratedCodeAttributeSyntax),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword)),
            identifier: Identifier(options.EventTypeExtensionsClass),
            typeParameterList: null,
            parameterList: null,
            baseList: null,
            constraintClauses: default,
            members: default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static FieldDeclarationSyntax CreateEventTypeField(string fieldName, string eventType) =>
            FieldDeclaration(
                attributeLists: default,
                modifiers: TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)),
                declaration: VariableDeclaration(
                    Emitter.EventTypeClassTypeSyntax,
                    SingletonSeparatedList(
                        VariableDeclarator(
                                identifier: Identifier(fieldName),
                                argumentList: null,
                                initializer: EqualsValueClause(eventType.GetLiteralExpression()))
                        )
                    )
                );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax CreateGetEventTypeMethod(string fieldName, Accessibility accessibility, string classFullName) =>
            MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.EventTypeClassTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: GetEventTypeMethod,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SingletonSeparatedList(Parameter(attributeLists: default, modifiers: TokenList(Token(SyntaxKind.ThisKeyword)), ParseTypeName(classFullName), Emitter.DiscardIdentifierToken, null))),
                body: null,
                expressionBody: ArrowExpressionClause(Emitter.EventTypesGeneratedNameSyntax.Member(fieldName)),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        internal void EmitEventTypes(SourceProductionContext ctx, ImmutableArray<EventType> eventTypes)
        {
            if (eventTypes is { IsDefaultOrEmpty: true })
            {
                return;
            }

            var fieldMembers = new List<MemberDeclarationSyntax>(capacity: eventTypes.Length);

            foreach (var (fieldName, _, eventTypeName, _) in eventTypes)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                fieldMembers.Add(CreateEventTypeField(fieldName: fieldName, eventType: eventTypeName));
            }

            var methodsMembers = new List<MemberDeclarationSyntax>(capacity: eventTypes.Length);

            foreach (var (fieldName, targetType, _, _) in eventTypes.OrderBy(static x => x.TargetType.FullName, StringComparer.Ordinal).Distinct(EventTypeNameComparer.Instance))
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                methodsMembers.Add(CreateGetEventTypeMethod(fieldName, targetType.Accessibility, targetType.FullName));
            }

            if (fieldMembers is { Count: 0 } && methodsMembers is { Count: 0 })
            {
                return;
            }

            var metadataClass = _metadataEmitter.CreateTypeManifestProvider(eventTypes);
            var metadataAttribute = AttributeList()
                .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)))
                .WithAttributes(
                    SingletonSeparatedList(
                        Attribute(ParseName(Constants.TypeManifestProviderAttribute))
                            .AddArgumentListArguments(AttributeArgument(TypeOfExpression(QualifiedName(IdentifierName(_namespaceMetadata), IdentifierName(metadataClass.Identifier.Text)))))
                ));

            var assemblyAttributes = Emitter.GenerateApplicationPartsAttributes(typeDefinitions);
            assemblyAttributes.Add(metadataAttribute);

            var cu = CompilationUnit()
                .WithAttributeLists(List(assemblyAttributes))
                .AddMembers(
                    EventTypesGeneratedSyntax.AddMembers([.. fieldMembers]),
                    NamespaceDeclaration(ParseName(options.RootNamespace)).AddMembers(
                        _eventTypesGeneratedExtensionsClassNameSyntax.AddMembers([.. methodsMembers])),
                    NamespaceDeclaration(ParseName(_namespaceMetadata)).AddMembers(metadataClass)
                )
                .WithLeadingTrivia(Emitter.HeaderSyntaxTriviaList)
                .NormalizeWhitespace();

            ctx.CancellationToken.ThrowIfCancellationRequested();

            var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

            ctx.AddSource($"EventTypes.g.cs", sourceText);
        }
    }
}
