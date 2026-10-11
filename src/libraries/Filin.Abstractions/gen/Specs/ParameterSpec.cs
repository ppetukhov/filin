using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

sealed record ParameterSpec(IParameterSymbol Parameter) : MemberSpec(Parameter, new TypeRef(Parameter.Type))
{
    public override bool CanGet => Accessibility >= Accessibility.Internal;
}
