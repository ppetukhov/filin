using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators.Extensions;

static class ExpressionSyntaxExtensions
{
    extension(ExpressionSyntax instance)
    {
        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        internal MemberAccessExpressionSyntax Member(string member) => instance.Member(member.ToIdentifierName());

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        internal MemberAccessExpressionSyntax Member(IdentifierNameSyntax member) => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
    }
}
