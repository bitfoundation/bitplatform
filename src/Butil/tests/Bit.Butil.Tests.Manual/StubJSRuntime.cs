using System.Diagnostics.CodeAnalysis;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

/// <summary>
/// An <see cref="IJSRuntime"/> that answers every call with <c>default</c>.
/// </summary>
/// <remarks>
/// This harness runs outside a browser, so there is no JS to call. Nothing here is exercising browser
/// behaviour - the services only have to be constructible and callable - and the E2E test project
/// covers the actual interop.
/// </remarks>
internal sealed class StubJSRuntime : IJSRuntime
{
    // The DynamicallyAccessedMembers annotations have to mirror IJSRuntime's exactly, or the trim
    // analyzer reports IL2095 on the override.
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
        => new(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => new(default(TValue)!);
}
