using Microsoft.JSInterop;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// A stand-in so the app's auth handler will refresh: a named <c>UnsupportedJavaScriptRuntime</c> is treated
/// as pre-rendering and the handler then refuses to touch the refresh token.
/// </summary>
internal sealed class TestJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => throw new InvalidOperationException("JS interop is not available in the E2E TestHost.");

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new InvalidOperationException("JS interop is not available in the E2E TestHost.");
}
