using Bit.Blex.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blex.Blazor;

/// <summary>
/// Renders a cached query from markup: its data once there is some, a loading state before, the error
/// when it failed -- re-rendering itself, and nothing around it, as the query changes.
/// </summary>
/// <typeparam name="TData">The data the query fetches.</typeparam>
/// <remarks>
/// <para>
/// Data wins over an error: a refetch that fails keeps showing what was fetched before, the way the
/// cache keeps it. Read <see cref="Query"/> (through a <c>@ref</c>) or render your own fragment around
/// <see cref="BlexQuery.IsRefetching"/> for a "refreshing" hint.
/// </para>
/// <para>
/// A new <see cref="Key"/> moves the view to that query; with <see cref="BlexQueryOptions.KeepPreviousData"/>
/// it keeps showing the previous key's data until the new one arrives.
/// </para>
/// </remarks>
/// <example><code>
/// &lt;BlexQueryView TData="Todo" Key="@(["todos", Id])" Fetch="ct =&gt; Api.GetTodoAsync(Id, ct)"&gt;
///     &lt;LoadingContent&gt;Loading…&lt;/LoadingContent&gt;
///     &lt;ErrorContent Context="error"&gt;@error.Message&lt;/ErrorContent&gt;
///     &lt;ChildContent Context="todo"&gt;@todo.Title&lt;/ChildContent&gt;
/// &lt;/BlexQueryView&gt;
/// </code></example>
public sealed class BlexQueryView<TData> : ComponentBase, IDisposable
{
    private BlexQuery<TData>? _query;
    private bool _disposed;

    [Inject] private BlexQueryClient Client { get; set; } = default!;

    /// <summary>Which query to render.</summary>
    [Parameter, EditorRequired] public BlexQueryKey Key { get; set; } = default!;

    /// <summary>Fetches the data; the closure from the latest render is the one used.</summary>
    [Parameter, EditorRequired] public Func<CancellationToken, Task<TData>> Fetch { get; set; } = default!;

    /// <summary>How to fetch it.</summary>
    [Parameter] public BlexQueryOptions? Options { get; set; }

    /// <summary>Rendered with the data, once there is some.</summary>
    [Parameter] public RenderFragment<TData>? ChildContent { get; set; }

    /// <summary>Rendered while there is neither data nor an error.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Rendered with the error when the query failed and has no data to show instead.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>The observer the view renders, for reading its status from the host.</summary>
    public BlexQuery<TData>? Query => _query;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Key is null)
            throw new InvalidOperationException($"{nameof(BlexQueryView<TData>)} requires a {nameof(Key)} parameter.");
        if (Fetch is null)
            throw new InvalidOperationException($"{nameof(BlexQueryView<TData>)} requires a {nameof(Fetch)} parameter.");

        if (_query is not null && _query.Key.Equals(Key))
        {
            _query.Update(Fetch, Options);
            return;
        }

        var previous = _query;
        var keep = previous is not null && (Options ?? previous.Options).KeepPreviousData && previous.HasData;
        _query = Client.Query(Key, Fetch, Options, keep, keep ? previous!.Data : default);
        _query.Changed += OnQueryChanged;

        if (previous is not null)
        {
            previous.Changed -= OnQueryChanged;
            previous.Dispose();
        }
    }

    private void OnQueryChanged()
    {
        if (_disposed)
            return;

        BlexRender.Observe(InvokeAsync(StateHasChanged), DispatchExceptionAsync);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_query is null)
            return;

        if (_query.HasData)
        {
            if (ChildContent is not null)
                builder.AddContent(0, ChildContent, _query.Data!);
        }
        else if (_query.Error is { } error)
        {
            if (ErrorContent is not null)
                builder.AddContent(1, ErrorContent, error);
        }
        else if (LoadingContent is not null)
        {
            builder.AddContent(2, LoadingContent);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_query is not null)
        {
            _query.Changed -= OnQueryChanged;
            _query.Dispose();
            _query = null;
        }
    }
}
