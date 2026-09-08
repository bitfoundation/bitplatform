using Microsoft.JSInterop;

namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// A stand-in so the auth handler will refresh: it treats <c>UnsupportedJavaScriptRuntime</c> as pre-rendering and
/// then refuses to touch the refresh token.
/// </summary>
internal sealed class TestJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => throw new InvalidOperationException("JS interop is not available in the E2E DeployedApiClientProvider.");

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new InvalidOperationException("JS interop is not available in the E2E DeployedApiClientProvider.");
}
