using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests;

public class TestJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return new ValueTask<TValue>(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return new ValueTask<TValue>(default(TValue)!);
    }
}
