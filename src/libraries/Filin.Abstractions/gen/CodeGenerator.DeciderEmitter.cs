using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class DeciderEmitter(TypeDefinitions typeDefinitions, GenerationOptions options)
    {
        readonly TypeManifestProviderEmitter _metadataEmitter = new(typeDefinitions);
        readonly string _namespaceMetadata = Emitter.GetConfigurationNamespace(options);

        internal void EmitDeciders(SourceProductionContext context, ImmutableArray<Decider> deciders)
        {
            if (deciders is { IsDefaultOrEmpty: true })
            {
                return;
            }

            if (!deciders.Any(x => x.TargetType.Type.Interfaces.Any(typeDefinitions.IsDecider)))
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var metadataClass = _metadataEmitter.CreateTypeManifestProvider(deciders);
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
                .AddMembers(NamespaceDeclaration(ParseName(_namespaceMetadata)).AddMembers(metadataClass))
                .WithLeadingTrivia(Emitter.HeaderSyntaxTriviaList)
                .NormalizeWhitespace();

            var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

            context.AddSource("Deciders.g.cs", sourceText);
        }
    }
}
