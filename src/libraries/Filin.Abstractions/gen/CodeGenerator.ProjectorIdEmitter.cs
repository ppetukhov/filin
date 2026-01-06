using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Filin.Generators.Extensions;
using Filin.Generators.Specs.Types;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed partial class ProjectorIdEmitter(TypeDefinitions typeDefinitions, GenerationOptions options)
    {
        readonly MetadataEmitter _metadataEmitter = new(typeDefinitions);

        readonly SyntaxToken ProjectorIdIdentifierToken = Identifier(options.ProjectorIdProperty);

        internal void EmitProjectIds(SourceProductionContext context, ImmutableArray<ProjectorId> projectorIds)
        {
            if (projectorIds is { IsDefaultOrEmpty: true })
            {
                return;
            }

            var props = GenerateClassHeirerarhy(context, projectorIds.Where(x => x.TargetType.type.Interfaces.Any(typeDefinitions.IsProjector)).Select(static x => (x.TargetType, x))).ToList();

            context.CancellationToken.ThrowIfCancellationRequested();

            if (props is { Count: > 0 })
            {
                var metadataClass = _metadataEmitter.GenerateMetadata(projectorIds);
                var metadataAttribute = AttributeList()
                    .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)))
                    .WithAttributes(
                        SingletonSeparatedList(
                            Attribute(ParseName(Constants.TypeManifestProviderAttribute))
                                .AddArgumentListArguments(AttributeArgument(TypeOfExpression(QualifiedName(IdentifierName(options.EmittingNamespace), IdentifierName(metadataClass.Identifier.Text)))))
                    ));

                var assemblyAttributes = Emitter.GenerateApplicationPartsAttributes(typeDefinitions);
                assemblyAttributes.Add(metadataAttribute);

                var cu = CompilationUnit()
                    .WithAttributeLists(List(assemblyAttributes))
                    .AddMembers([.. props])
                    .AddMembers(NamespaceDeclaration(ParseName(options.EmittingNamespace)).AddMembers(metadataClass))
                    .NormalizeWhitespace();

                var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

                context.AddSource("Projectors.g.cs", sourceText);
            }
        }

        IEnumerable<NamespaceDeclarationSyntax> GenerateClassHeirerarhy(SourceProductionContext ctx, IEnumerable<(ObjectSpec TargetType, ProjectorId ProjectorId)> tuples)
        {
            IEnumerable<MemberDeclarationSyntax> MembersFactory((ObjectSpec TargetType, ProjectorId ProjectorId) tuple)
            {
                return [
                    PropertyDeclaration(
                        attributeLists: default,
                        modifiers: TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PublicKeyword)),
                        type: Emitter.ProjectoIdTypeSyntax,
                        explicitInterfaceSpecifier: null,
                        identifier: ProjectorIdIdentifierToken,
                        accessorList: AccessorList(SingletonList(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))),
                        expressionBody: null,
                        initializer: EqualsValueClause(tuple.ProjectorId.Id.GetLiteralExpression()),
                        semicolonToken: Token(SyntaxKind.SemicolonToken))
                ];
            }

            return Emitter.GenerateClassHeirerarhyCore(tuples.Select(static x => (x.TargetType, x.ProjectorId)), MembersFactory, ctx.CancellationToken);
        }
    }
}
