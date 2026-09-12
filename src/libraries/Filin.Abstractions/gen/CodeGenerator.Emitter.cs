using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using Filin.Generators.Extensions;
using Filin.Generators.Specs.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    static partial class Emitter
    {
        internal static readonly AttributeListSyntax GeneratedCodeAttributeSyntax =
            AttributeList().AddAttributes(
                Attribute(ParseName("global::System.CodeDom.Compiler.GeneratedCodeAttribute"))
                    .AddArgumentListArguments(
                        AttributeArgument(typeof(CodeGenerator).Assembly.GetName().Name.GetLiteralExpression()),
                        AttributeArgument((typeof(CodeGenerator).Assembly.GetName().Version?.ToString() ?? string.Empty).GetLiteralExpression())),
                Attribute(ParseName("global::System.ComponentModel.EditorBrowsableAttribute"))
                    .AddArgumentListArguments(
                        AttributeArgument(ParseName("global::System.ComponentModel.EditorBrowsableState").Member("Never")))
            );

        internal static readonly AttributeListSyntax AggressiveInliningAttributeSyntax =
            AttributeList(SingletonSeparatedList(Attribute(
                ParseName("global::System.Runtime.CompilerServices.MethodImplAttribute"))
                    .AddArgumentListArguments(
                        AttributeArgument(ParseName("global::System.Runtime.CompilerServices.MethodImplOptions").Member("AggressiveInlining")))));

        /// <summary>
        /// EventTypesGenerated
        /// </summary>
        internal static readonly SyntaxToken EventTypesGeneratedToken = Identifier("EventTypesGenerated");

        /// <summary>
        /// EventTypesGenerated
        /// </summary>
        internal static readonly IdentifierNameSyntax EventTypesGeneratedNameSyntax = IdentifierName(EventTypesGeneratedToken);

        internal static readonly TypeSyntax DcbEventTypeSyntax = ParseTypeName(Constants.DcbEventTypeFullName);
        internal static readonly TypeSyntax DateTimeOffsetTypeSyntax = ParseTypeName("global::System.DateTimeOffset");
        internal static readonly TypeSyntax IWithQueryTagsTypeSyntax = ParseTypeName(Constants.IWithQueryTags);
        internal static readonly TypeSyntax TagCollectionTypeSyntax = ParseTypeName(Constants.TagCollectionFullName);

        /// <summary>
        /// QueriesGenerated
        /// </summary>
        internal static readonly SyntaxToken QueriesGeneratedToken = Identifier("QueriesGenerated");
        internal static readonly TypeSyntax ProjectoIdTypeSyntax = ParseTypeName(Constants.ProjectorIdTypeFullName);
        internal static readonly TypeSyntax EventTypeClassTypeSyntax = ParseTypeName(Constants.EventTypeTypeFullName);
        internal static readonly TypeSyntax TagTypeSyntax = ParseTypeName(Constants.TagTypeFullName);
        internal static readonly TypeSyntax QueryTypeSyntax = ParseTypeName(Constants.QueryTypeFullName);
        internal static readonly TypeSyntax QueryItemTypeSyntax = ParseTypeName(Constants.QueryItemTypeFullName);

        internal static readonly TypeSyntax ImmutableArrayQueryItemTypeSyntax = ParseTypeName($"global::System.Collections.Immutable.ImmutableArray<{Constants.QueryItemTypeFullName}>");

        internal static readonly SyntaxToken DiscardIdentifierToken = Identifier("_");
        internal static readonly SyntaxToken ThisIdentifierToken = Identifier("@this");
        internal static readonly IdentifierNameSyntax ThisIdentifierNameSyntax = IdentifierName(ThisIdentifierToken);

        internal static readonly NameSyntax ApplicationPartAttributeNameSyntax = ParseName(Constants.ApplicationPartAttribute);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SyntaxKind MethodAccessibility(Accessibility accessibility) =>
            accessibility < Accessibility.Public
                ? SyntaxKind.InternalKeyword
                : SyntaxKind.PublicKeyword;

        static readonly Regex SanitizeIdentifierRegex = new("^([0-9]+)|([^0-9a-zA-Z_]+)", RegexOptions.Compiled);

        internal static string SanitizeIdentifierName(string input) => SanitizeIdentifierRegex.Replace(
            input,
            static match => match.Value switch
            {
                // Prefix leading digits with an '_' to make them a valid identifier.
                { Length: > 0 } value when char.IsDigit(value[0]) => $"_{value}",

                // Eliminate all other matches by replacing them with an empty string.
                _ => "_",
            });

        internal static IEnumerable<NamespaceDeclarationSyntax> GenerateClassHeirerarhyCore<T>(IEnumerable<(ObjectSpec TargetType, T Item)> queries, Func<(ObjectSpec TargetType, T Item), IEnumerable<MemberDeclarationSyntax>> membersFactory, CancellationToken token)
        {
            var sb = new StringBuilder(255);

            foreach (var query in queries.OrderBy(x => x.TargetType.IdentifierCompatibleSubstring, StringComparer.Ordinal))
            {
                var (targetType, _) = query;

                sb.Length = 0;

                foreach (var nsItem in targetType.DisplayParts.TakeWhile(static x => x.Kind is SymbolDisplayPartKind.Punctuation or SymbolDisplayPartKind.NamespaceName))
                {
                    sb.Append(nsItem);
                }

                if (sb is { Length: > 0 } & sb[sb.Length - 1] == '.')
                {
                    sb.Length -= 1; // drop last dot;
                }

                token.ThrowIfCancellationRequested();

                var nsMembers = new List<TypeDeclarationSyntax>();

                foreach (var clItem in targetType.DisplayParts.SkipWhile(static x => x.Kind is SymbolDisplayPartKind.Punctuation or SymbolDisplayPartKind.NamespaceName))
                {
                    if (clItem.Kind is SymbolDisplayPartKind.Punctuation || clItem.Symbol is not ISymbol symbol)
                    {
                        continue;
                    }

                    var attributeLists = SingletonList(GeneratedCodeAttributeSyntax);
                    var modifiers = TokenList(Token(SyntaxKind.PartialKeyword));
                    var identifier = Identifier(symbol.Name);

                    TypeDeclarationSyntax decl;

                    if (clItem.Kind == SymbolDisplayPartKind.ClassName)
                    {
                        decl = ClassDeclaration(
                                attributeLists: default,
                                modifiers: modifiers,
                                identifier: identifier,
                                typeParameterList: null,
                                parameterList: null,
                                baseList: null,
                                constraintClauses: default,
                                members: default);
                    }
                    else if (clItem.Kind is SymbolDisplayPartKind.RecordClassName or SymbolDisplayPartKind.RecordStructName)
                    {
                        decl = RecordDeclaration(
                                attributeLists: default,
                                modifiers: modifiers,
                                keyword: Token(clItem.Kind is SymbolDisplayPartKind.RecordClassName ? SyntaxKind.RecordKeyword : SyntaxKind.ClassKeyword),
                                identifier: identifier,
                                typeParameterList: null,
                                parameterList: null,
                                baseList: null,
                                constraintClauses: default,
                                openBraceToken: Token(SyntaxKind.OpenBraceToken),
                                members: default,
                                closeBraceToken: Token(SyntaxKind.CloseBraceToken),
                                semicolonToken: Token(SyntaxKind.None)
                                );
                    }
                    else
                    {
                        continue;
                    }

                    nsMembers.Add(decl);
                }

                token.ThrowIfCancellationRequested();

                if (nsMembers is { Count: 0 })
                {
                    continue;
                }

                nsMembers.Reverse();

                var rootNsMember = nsMembers.First().AddMembers([.. membersFactory(query)]);

                foreach (var nsMember in nsMembers.Skip(1))
                {
                    rootNsMember = nsMember.AddMembers(rootNsMember);
                }

                yield return NamespaceDeclaration(ParseName(sb.ToString())).AddMembers(rootNsMember);

                token.ThrowIfCancellationRequested();
            }
        }

        internal static List<AttributeListSyntax> GenerateApplicationPartsAttributes(TypeDefinitions typeDefinitions)
        {
            var attributes = new List<AttributeListSyntax>(capacity: typeDefinitions.ApplicationParts.Count);

            foreach (var assemblyName in typeDefinitions.ApplicationParts)
            {
                // Generate an assembly-level attribute with an instance of that class.
                var attribute = AttributeList(
                    AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)),
                    SingletonSeparatedList(
                        Attribute(ApplicationPartAttributeNameSyntax)
                            .AddArgumentListArguments(AttributeArgument(assemblyName.GetLiteralExpression()))));
                attributes.Add(attribute);
            }

            return attributes;
        }
    }
}
