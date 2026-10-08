using System.Collections.Immutable;
using System.Text;

using Filin.Generators.Extensions;
using Filin.Generators.Specs.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class DcbEventEmitter(GenerationOptions options)
    {
        static readonly SyntaxToken ToDcbEventMethodIdentifierToken = Identifier(Constants.ToDcbEventMethod);

        readonly ClassDeclarationSyntax _generatedExtensionsClassNameSyntax = ClassDeclaration(
            attributeLists: SingletonList(Emitter.GeneratedCodeAttributeSyntax),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword)),
            identifier: Identifier(options.EventTypeToDcbEventExtensionsClass),
            typeParameterList: null,
            parameterList: null,
            baseList: null,
            constraintClauses: default,
            members: default);

        internal void Emit(SourceProductionContext ctx, ImmutableArray<EventType> eventTypes, ImmutableArray<EventTag> eventTags)
        {
            if (eventTypes is { IsDefaultOrEmpty: true })
            {
                return;
            }

            var evtTagsCount = eventTags.Select(x => x.TargetType.FullName).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            var toDcbEventExtentions = new List<MemberDeclarationSyntax>(capacity: eventTypes.Length);

            foreach (var (_, targetType, _, _) in eventTypes.OrderBy(static x => x.TargetType.FullName, StringComparer.Ordinal).Distinct(EventTypeNameComparer.Instance))
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                toDcbEventExtentions.Add(CreateToDcbEvent_IWithQueryTags(targetType));

                _ = evtTagsCount.TryGetValue(targetType.FullName, out var tagsCount);

                toDcbEventExtentions.Add(CreateToDcbEvent_Tags(targetType, tagsCount));
            }

            if (toDcbEventExtentions is { Count: 0 })
            {
                return;
            }

            var cu = CompilationUnit()
                .AddMembers(
                    FileScopedNamespaceDeclaration(ParseName(options.RootNamespace)),
                    _generatedExtensionsClassNameSyntax.AddMembers([.. toDcbEventExtentions])
                )
                .WithLeadingTrivia(Emitter.HeaderSyntaxTriviaList)
                .NormalizeWhitespace();

            ctx.CancellationToken.ThrowIfCancellationRequested();

            var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

            ctx.AddSource($"DcbEvents.g.cs", sourceText);
        }


        #region Extentions ToDcbEvent()

        // static DcbEvent ToDcbEvent(this V1.CourseCreated evt, IWithQueryTags command) => evt.ToDcbEvent(command.GetTags());
        static MethodDeclarationSyntax CreateToDcbEvent_IWithQueryTags(ObjectSpec targetType) =>
            MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(targetType.Accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.DcbEventTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: ToDcbEventMethodIdentifierToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SeparatedList(
                        [
                            Parameter(
                                attributeLists: default,
                                modifiers: TokenList(Token(SyntaxKind.ThisKeyword)),
                                ParseTypeName(targetType.FullName),
                                Identifier("evt"),
                                null),
                            Parameter(
                                attributeLists: default,
                                modifiers: default,
                                Emitter.IWithQueryTagsTypeSyntax,
                                Identifier("command"),
                                null)
                        ]
                    )
                ),
                body: null,
                expressionBody: ArrowExpressionClause(
                    InvocationExpression(
                        IdentifierName(Identifier("evt")).Member(Constants.ToDcbEventMethod),
                        ArgumentList()
                            .AddArguments(
                                Argument(InvocationExpression(IdentifierName("command").Member(Constants.GetTagsMethod)))
                            )
                    )
                ),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        // static DcbEvent ToDcbEvent(this {targetType} evt, Tags tags)
        static MethodDeclarationSyntax CreateToDcbEvent_Tags(ObjectSpec targetType, int tagsCount)
        {
            // Tags = {tagsExpr}
            ExpressionSyntax tagsExpr = tagsCount switch
            {
                0 => IdentifierName(Identifier("tags")), // tags
                _ => ConditionalExpression(
                                IsPatternExpression(
                                    IdentifierName(Identifier("tags")),
                                    RecursivePattern()
                                        .AddPropertyPatternClauseSubpatterns(
                                            Subpattern(
                                                NameColon(IdentifierName(Identifier("IsEmpty"))),
                                                ConstantPattern(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                            )
                                        )
                                ),
                                InvocationExpression(IdentifierName(Identifier("evt")).Member(Constants.GetTagsMethod)),
                                CollectionExpression()
                                    .AddElements(
                                        SpreadElement(IdentifierName(Identifier("tags"))),
                                        SpreadElement(InvocationExpression(IdentifierName(Identifier("evt")).Member(Constants.GetTagsMethod)))
                                    )
                            ) // tags is { IsEmpty: true } ? evt.GetTags() : [.. tags, .. evt.GetTags()]
            };

            return MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(targetType.Accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.DcbEventTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: ToDcbEventMethodIdentifierToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SeparatedList(
                        [
                            Parameter(
                                attributeLists: default,
                                modifiers: TokenList(Token(SyntaxKind.ThisKeyword)),
                                ParseTypeName(targetType.FullName),
                                Identifier("evt"),
                                null),
                            Parameter(
                                attributeLists: default,
                                modifiers: default,
                                Emitter.TagsTypeSyntax,
                                Identifier("tags"),
                                null)
                        ]
                    )
                ),
                body: null,
                expressionBody: ArrowExpressionClause(
                    ObjectCreationExpression(
                        Emitter.DcbEventTypeSyntax,
                        null,
                        InitializerExpression(
                            SyntaxKind.ObjectInitializerExpression,
                            SeparatedList<ExpressionSyntax>(
                                [
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("EventType"),
                                        InvocationExpression(IdentifierName(Identifier("evt")).Member(Constants.GetEventTypeMethod))
                                    ),
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("Tags"),
                                        tagsExpr
                                    ),
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("Payload"),
                                        IdentifierName(Identifier("evt"))
                                    )
                                ]
                            )
                        )
                    )
                ),
                semicolonToken: Token(SyntaxKind.SemicolonToken));
        }
        #endregion
    }
}
