using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Answers the page visibility init call the way the browser would, or fails it the way a missing script does.
/// </summary>
public sealed class PageStateJsRuntime : IJSRuntime
{
    private readonly string? _json;
    private readonly Exception? _exception;

    public PageStateJsRuntime(string json) => _json = json;

    public PageStateJsRuntime(Exception exception) => _exception = exception;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (_exception is not null) return ValueTask.FromException<TValue>(_exception);

        return new ValueTask<TValue>(JsonSerializer.Deserialize<TValue>(_json!, JsonSerializerOptions.Web)!);
    }
}
