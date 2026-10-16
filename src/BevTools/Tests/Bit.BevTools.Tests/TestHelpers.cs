using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

internal static class TestHelpers
{
    /// <summary>Creates an enabled BevTools session backed by a fresh DI scope (no renderer).</summary>
    public static (BevToolsSession Session, IServiceScope Scope) CreateSession(Action<BitBevToolsOptions>? configure = null, Action<IServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();
        services?.Invoke(collection);
        collection.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            configure?.Invoke(o);
        });
        var provider = collection.BuildServiceProvider();
        var scope = provider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();
        session.EnsureActivated();
        return (session, scope);
    }

    /// <summary>
    /// Tells bUnit which interactive renderer the components under test run on. <c>RendererInfo</c> arrived in .NET 9;
    /// on .NET 8 there is nothing to set, and the panel infers interactivity from its JS runtime instead.
    /// </summary>
    public static void UseRenderer(this BunitContext context, string name)
    {
#if NET9_0_OR_GREATER
        context.SetRendererInfo(new RendererInfo(name, isInteractive: true));
#endif
    }
}

public class ParentComponent : ComponentBase
{
    [Parameter] public bool ShowChild { get; set; } = true;

    [Parameter] public int Value { get; set; }

    public int RenderCalls { get; private set; }

    private int _counter;

    public void Increment()
    {
        _counter++;
        StateHasChanged();
    }

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderCalls++;
        builder.OpenElement(0, "div");
        builder.AddContent(1, "value:" + Value + " counter:" + _counter);
        if (ShowChild)
        {
            builder.OpenComponent<ChildComponent>(2);
            builder.AddComponentParameter(3, nameof(ChildComponent.Text), "child-" + Value + "-" + _counter);
            builder.CloseComponent();
        }

        builder.CloseElement();
    }
}

public class ChildComponent : ComponentBase
{
    [Parameter] public string? Text { get; set; }

    [Inject] public IJSRuntime? JS { get; set; }

    private readonly List<string> _history = [];

    protected override void OnParametersSet() => _history.Add(Text ?? "");

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, Text);
        builder.CloseElement();
    }
}

public class ThrowingComponent : ComponentBase
{
    [Parameter] public bool Throw { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Throw)
        {
            throw new InvalidOperationException("boom");
        }

        builder.AddContent(0, "fine");
    }
}

public sealed class FakeJSRuntime : IJSRuntime
{
    public List<string> Calls { get; } = [];

    public Func<string, object?[]?, object?> Handler { get; set; } = (_, _) => null;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public object?[]? LastArgs { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add(identifier);
        LastArgs = args;
        var result = Handler(identifier, args);
        return ValueTask.FromResult(result is TValue typed ? typed : default!);
    }
}

public sealed class FakeJSObjectReference : IJSObjectReference
{
    public List<string> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
