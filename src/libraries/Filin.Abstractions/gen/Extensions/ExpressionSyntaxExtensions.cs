using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators.Extensions;

static class ExpressionSyntaxExtensions
{
    /// <summary>
    /// Returns member access syntax.
    /// </summary>
    /// <param name="instance">
    /// The instance.
    /// </param>
    /// <param name="member">
    /// The member.
    /// </param>
    /// <returns>
    /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
    /// </returns>
    internal static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, string member) => instance.Member(member.ToIdentifierName());

    /// <summary>
    /// Returns member access syntax.
    /// </summary>
    /// <param name="instance">
    /// The instance.
    /// </param>
    /// <param name="member">
    /// The member.
    /// </param>
    /// <returns>
    /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
    /// </returns>
    internal static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, IdentifierNameSyntax member) => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
}
