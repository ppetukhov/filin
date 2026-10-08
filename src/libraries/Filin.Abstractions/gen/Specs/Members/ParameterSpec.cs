using Microsoft.CodeAnalysis;

using SourceGenerators;

namespace Filin.Generators.Specs.Members;

sealed record ParameterSpec(IParameterSymbol Parameter) : MemberSpec(Parameter, new TypeRef(Parameter.Type))
{
    public override bool CanGet => Accessibility >= Accessibility.Internal;
}
