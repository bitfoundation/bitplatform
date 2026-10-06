using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Answers the page visibility init call the way the browser would, or fails it the way a missing script does.
/// Every other call (the dispose one, for one) is answered with nothing, the way a void call is.
/// </summary>
public sealed class PageStateJsRuntime : IJSRuntime
{
    private const string InitIdentifier = "BitBlazorUI.PageVisibility.init";

    private readonly string _json = """{"hidden":false,"blurred":false}""";

    public PageStateJsRuntime(string json) => _json = json;

    public PageStateJsRuntime(Exception exception) => Failure = exception;

    /// <summary>
    /// What the init call fails with while it is set; clearing it lets a later call through, the way a script that
    /// has finished loading does.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// While set, the init call is only answered once this completes, the way a slow round trip is.
    /// </summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>
    /// How many times the page has been asked for its state.
    /// </summary>
    public int InitCount { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != InitIdentifier) return new ValueTask<TValue>(default(TValue)!);

        InitCount++;

        if (Failure is not null) return ValueTask.FromException<TValue>(Failure);

        if (Gate is { } gate) return new ValueTask<TValue>(AnswerAfterAsync<TValue>(gate.Task));

        return new ValueTask<TValue>(Read<TValue>());
    }

    private async Task<TValue> AnswerAfterAsync<TValue>(Task gate)
    {
        await gate;

        return Read<TValue>();
    }

    private TValue Read<TValue>() => JsonSerializer.Deserialize<TValue>(_json, JsonSerializerOptions.Web)!;
}
