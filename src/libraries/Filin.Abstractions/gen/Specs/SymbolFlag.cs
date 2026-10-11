namespace Filin.Generators.Specs;

[Flags]
enum SymbolFlag : byte
{
    Ok = 0,
    IsStatic = 1,
    IsAbstract = 2,
    IsImplicitlyDeclared = 3,
}
