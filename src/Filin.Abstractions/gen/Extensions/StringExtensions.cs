using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Filin.Generators.Extensions;

/// <summary>
/// Extensions to the <see cref="string"/> class to support code generation.
/// </summary>
static class StringExtensions
{
    extension(string str)
    {
        /// <summary>
        /// Returns the provided string as a literal expression.
        /// </summary>
        /// <returns>
        /// The literal expression.
        /// </returns>
        internal LiteralExpressionSyntax GetLiteralExpression()
        {
            var syntaxToken = SyntaxFactory.Literal(
                SyntaxFactory.TriviaList(),
                @"""" + str + @"""",
                str,
                SyntaxFactory.TriviaList());

            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, syntaxToken);
        }

        internal string EscapeIdentifier() => IsCSharpKeyword(str) ? "@" + str : str;
    }

    extension(string identifier)
    {
        internal SyntaxToken ToIdentifier()
        {
            identifier = identifier.TrimStart('@');

            return IsCSharpKeyword(identifier)
                ? SyntaxFactory.VerbatimIdentifier(
                    SyntaxTriviaList.Empty,
                    identifier,
                    identifier,
                    SyntaxTriviaList.Empty)
                : SyntaxFactory.Identifier(SyntaxTriviaList.Empty, identifier, SyntaxTriviaList.Empty);
        }

        internal IdentifierNameSyntax ToIdentifierName() => SyntaxFactory.IdentifierName(identifier.ToIdentifier());
    }

    static bool IsCSharpKeyword(string identifier) =>
        identifier switch
        {
            "abstract" or "add" or "alias" or "as" or "ascending" or "async" or "await" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or "delegate" or "descending" or "do" or "double" or "dynamic" or "else" or "enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "from" or "get" or "global" or "goto" or "group" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "into" or "is" or "join" or "let" or "lock" or "long" or "nameof" or "namespace" or "new" or "null" or "object" or "operator" or "orderby" or "out" or "override" or "params" or "partial" or "private" or "protected" or "public" or "readonly" or "ref" or "remove" or "return" or "sbyte" or "sealed" or "select" or "set" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "value" or "var" or "virtual" or "void" or "volatile" or "when" or "where" or "while" or "yield" => true,
            _ => false,
        };
}
