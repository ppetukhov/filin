using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

using Filin.Generators.Extensions;
using Filin.Generators.Specs.Members;
using Filin.Generators.Specs.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class EventTagsEmitter(TypeDefinitions typeDefinitions, GenerationOptions options)
    {
        static readonly TypeSyntax ReturnTagTypeCollectionsSyntax = ParseTypeName(Constants.TagCollectionFullName);

        readonly SyntaxToken GetTagMethodIdentifierToken = Identifier(options.GetTagMethod);
        readonly SyntaxToken GetTagsMethodIdentifierToken = Identifier(options.GetTagsMethod);
        readonly FileScopedNamespaceDeclarationSyntax NamespaceSyntax = FileScopedNamespaceDeclaration(ParseName(options.EmittingNamespace));
        readonly ClassDeclarationSyntax TagsGeneratedExtensionsClassNameSyntax = ClassDeclaration(
            attributeLists: SingletonList(Emitter.GeneratedCodeAttributeSyntax),
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword)),
            identifier: Identifier(options.TagExtensionsClassName),
            typeParameterList: null,
            parameterList: null,
            baseList: null,
            constraintClauses: default,
            members: default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static InterpolatedStringExpressionSyntax TagByMemberToStringExpression(string tag, MemberSpec member, bool withThis = true)
            => InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
                .AddContents(
                    InterpolatedStringText().WithTextToken(Token(
                        TriviaList(),
                        SyntaxKind.InterpolatedStringTextToken,
                        tag + Constants.TagSuffixSeparator,
                        string.Empty,
                        TriviaList())),
                    Interpolation(withThis ? Emitter.ThisIdentifierNameSyntax.Member(member.Name) : IdentifierName(Identifier(member.Name)))
                );

        internal void EmitEventTags(SourceProductionContext context, ImmutableArray<EventTag> eventTags)
        {
            if (eventTags is { IsDefaultOrEmpty: true })
            {
                return;
            }

            var targetTypeGroupedCollection = eventTags.GroupBy(static x => x.TargetType, ObjectSpecComparer.Instance).ToList();

            var extensionsMethods = new List<MemberDeclarationSyntax>(capacity: eventTags.Length);

            foreach (var targetTypeGrouped in targetTypeGroupedCollection)
            {
                if (targetTypeGrouped.Key.type.Interfaces.Any(IWithQueryTags))
                {
                    continue;
                }

                context.CancellationToken.ThrowIfCancellationRequested();

                var groupedList = targetTypeGrouped.OrderBy(x => x.Tag).ToList();

                if (groupedList.Count == 1)
                {
                    extensionsMethods.Add(CreateGetEventTagMethod(targetType: targetTypeGrouped.Key, eventTag: groupedList[0]));
                }

                extensionsMethods.Add(CreateGetEventTagsMethod(targetType: targetTypeGrouped.Key, eventTags: groupedList));
            }

            var instanceMethods = GenerateClassHeirerarhy(context, targetTypeGroupedCollection.Where(x => x.Key.type.Interfaces.Any(IWithQueryTags)).Select(static x => (x.Key, (IEnumerable<EventTag>)x))).ToList();

            context.CancellationToken.ThrowIfCancellationRequested();


            if (extensionsMethods is { Count: > 0 })
            {
                var cu = CompilationUnit().AddMembers(
                    NamespaceSyntax,
                    TagsGeneratedExtensionsClassNameSyntax.AddMembers([.. extensionsMethods])
                ).NormalizeWhitespace();

                context.CancellationToken.ThrowIfCancellationRequested();

                var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

                context.AddSource("TagExtensions.g.cs", sourceText);
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            if (instanceMethods is { Count: > 0 })
            {
                var cu = CompilationUnit().AddMembers([.. instanceMethods]).NormalizeWhitespace();

                var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

                context.AddSource("Tags.g.cs", sourceText);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        MethodDeclarationSyntax CreateGetEventTagMethod(TypeSpec targetType, EventTag eventTag)
            => eventTag is EventTagMember itemWithMember
                ? CreateGetEventTagMethod(targetType: targetType, tag: itemWithMember.Tag, member: itemWithMember.Member)
                : CreateGetEventTagMethod(targetType: targetType, tag: eventTag.Tag);

        MethodDeclarationSyntax CreateGetEventTagMethod(TypeSpec targetType, string tag) =>
            MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(targetType.Accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.TagTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: GetTagMethodIdentifierToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SingletonSeparatedList(Parameter(
                        attributeLists: default,
                        modifiers: TokenList(Token(SyntaxKind.ThisKeyword)),
                        ParseTypeName(targetType.FullName),
                        Emitter.DiscardIdentifierToken,
                        null))),
                body: null,
                expressionBody: ArrowExpressionClause(tag.GetLiteralExpression()),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        MethodDeclarationSyntax CreateGetEventTagMethod(TypeSpec targetType, string tag, MemberSpec member) =>
            MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(targetType.Accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.TagTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: GetTagMethodIdentifierToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SingletonSeparatedList(Parameter(
                        attributeLists: default,
                        modifiers: TokenList(Token(SyntaxKind.ThisKeyword)),
                        ParseTypeName(targetType.FullName),
                        Emitter.ThisIdentifierToken,
                        null))),
                body: null,
                expressionBody: ArrowExpressionClause(TagByMemberToStringExpression(tag, member)),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        bool IWithQueryTags(INamedTypeSymbol symbol) => !SymbolEqualityComparer.Default.Equals(symbol, typeDefinitions.IWithQueryTags) ? symbol.Interfaces.Any(IWithQueryTags) : true;

        static (ArrowExpressionClauseSyntax expressionBody, bool hasMember) CreateGetTagsExpression(IEnumerable<EventTag> eventTags, bool withThis = true)
        {
            var collectionElemetList = new List<CollectionElementSyntax>();

            var hasMember = false;

            foreach (var evt in eventTags)
            {
                if (evt is EventTagMember withProperty)
                {
                    hasMember = true;
                    collectionElemetList.Add(ExpressionElement(TagByMemberToStringExpression(withProperty.Tag, withProperty.Member, withThis: withThis)));
                }
                else
                {
                    collectionElemetList.Add(ExpressionElement(evt.Tag.GetLiteralExpression()));
                }
            }

            return (ArrowExpressionClause(CollectionExpression(SeparatedList(collectionElemetList))), hasMember);
        }

        MethodDeclarationSyntax CreateGetEventTagsMethod(ObjectSpec targetType, List<EventTag> eventTags)
        {
            var (expressionBody, hasMember) = CreateGetTagsExpression(eventTags);

            return MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(Emitter.MethodAccessibility(targetType.Accessibility)), Token(SyntaxKind.StaticKeyword)),
                returnType: ReturnTagTypeCollectionsSyntax,
                explicitInterfaceSpecifier: null,
                identifier: GetTagsMethodIdentifierToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SingletonSeparatedList(Parameter(
                        attributeLists: default,
                        modifiers: TokenList(Token(SyntaxKind.ThisKeyword)),
                        ParseTypeName(targetType.FullName),
                        hasMember ? Emitter.ThisIdentifierToken : Emitter.DiscardIdentifierToken,
                        null))),
                body: null,
                expressionBody: expressionBody,
                semicolonToken: Token(SyntaxKind.SemicolonToken));
        }

        IEnumerable<NamespaceDeclarationSyntax> GenerateClassHeirerarhy(SourceProductionContext ctx, IEnumerable<(ObjectSpec TargetType, IEnumerable<EventTag> EventTags)> tuples)
        {
            IEnumerable<MemberDeclarationSyntax> MembersFactory((ObjectSpec TargetType, IEnumerable<EventTag> EventTags) tuple)
            {
                return [MethodDeclaration(
                    attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                    modifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
                    returnType: ReturnTagTypeCollectionsSyntax,
                    explicitInterfaceSpecifier: null,
                    identifier: GetTagsMethodIdentifierToken,
                    typeParameterList: default,
                    constraintClauses: default,
                    parameterList: ParameterList(),
                    body: null,
                    expressionBody: CreateGetTagsExpression(tuple.EventTags, withThis: false).expressionBody,
                    semicolonToken: Token(SyntaxKind.SemicolonToken))];
            }

            return Emitter.GenerateClassHeirerarhyCore(tuples.Select(static x => (x.TargetType, x.EventTags)), MembersFactory, ctx.CancellationToken);
        }
    }
}
