using Microsoft.CodeAnalysis;

using SourceGenerators;

namespace Filin.Generators.Specs.Members;

sealed record ParameterSpec(IParameterSymbol parameter) : MemberSpec(parameter, new TypeRef(parameter.Type))
{
    public RefKind RefKind { get; } = parameter.RefKind;

    public override bool CanGet => Accessibility >= Accessibility.Internal;
}
