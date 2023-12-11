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

public class ControllerAction
{
    public IMethodSymbol Method { get; set; } = default!;

    public ITypeSymbol ReturnType { get; set; } = default!;

    public bool DoesReturnSomething => ReturnType.ToDisplayString() is not "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";

    public string HttpMethod { get; set; } = default!;

    public string Url { get; set; } = default!;

    public List<ActionParameter> Parameters { get; set; } = [];

    public ActionParameter? BodyParameter { get; set; }

    public bool HasCancellationToken { get; set; }
}

public class ActionParameter
{
    public string Name { get; set; } = default!;

    public ITypeSymbol Type { get; set; } = default!;
}

