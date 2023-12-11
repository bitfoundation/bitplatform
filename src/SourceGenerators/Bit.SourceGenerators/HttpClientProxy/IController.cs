using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public class IController
{
    public string Name { get; set; } = default!;

    public string ClassName { get; set; } = default!;

    public ITypeSymbol Symbol { get; set; } = default!;

    public InterfaceDeclarationSyntax Syntax { get; set; } = default!;

    public List<ControllerAction> Actions { get; set; } = [];
}

