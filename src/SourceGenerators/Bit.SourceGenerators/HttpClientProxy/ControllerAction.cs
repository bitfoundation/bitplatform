using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

public class ControllerAction
{
    public IMethodSymbol Method { get; set; } = default!;

    public ITypeSymbol ReturnType { get; set; } = default!;

    public bool DoesReturnSomething => ReturnType.ToDisplayString() is not "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";

    public bool DoesReturnString => DoesReturnSomething && ReturnType.ToDisplayString() is "System.Threading.Tasks.Task<string>" or "System.Threading.Tasks.ValueTask<string>";

    public bool DoesReturnIAsyncEnumerable => DoesReturnSomething && ReturnType.ToDisplayString().Contains("IAsyncEnumerable");

    public string HttpMethod { get; set; } = default!;

    public string Url { get; set; } = default!;

    public List<ActionParameter> Parameters { get; set; } = [];

    public ActionParameter? BodyParameter { get; set; }

    public bool HasCancellationToken => string.IsNullOrEmpty(CancellationTokenParameterName) is false;

    public string? CancellationTokenParameterName { get; set; }
}
