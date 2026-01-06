using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

public sealed partial class CodeGenerator
{
    sealed class DeciderEmitter(TypeDefinitions typeDefinitions, GenerationOptions options)
    {
        readonly MetadataEmitter _metadataEmitter = new(typeDefinitions);

        internal void EmitDeciders(SourceProductionContext context, ImmutableArray<Decider> deciders)
        {
            if (deciders is { IsDefaultOrEmpty: true })
            {
                return;
            }

            if (!deciders.Any(x => x.TargetType.type.Interfaces.Any(typeDefinitions.IsDecider)))
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var metadataClass = _metadataEmitter.GenerateMetadata(deciders);
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
                .AddMembers(NamespaceDeclaration(ParseName(options.EmittingNamespace)).AddMembers(metadataClass))
                .NormalizeWhitespace();

            var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

            context.AddSource("Deciders.g.cs", sourceText);
        }
    }
}
