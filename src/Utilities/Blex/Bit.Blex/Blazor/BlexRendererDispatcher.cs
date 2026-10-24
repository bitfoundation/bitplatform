using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Blazor;

/// <summary>A Blazor renderer's <see cref="Dispatcher"/> as the <see cref="IBlexDispatcher"/> a manager keeps to.</summary>
/// <param name="dispatcher">The renderer's dispatcher.</param>
public sealed class BlexRendererDispatcher(Dispatcher dispatcher) : IBlexDispatcher
{
    /// <summary>The renderer's dispatcher.</summary>
    public Dispatcher Dispatcher { get; } = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    /// <inheritdoc />
    public bool CheckAccess() => Dispatcher.CheckAccess();

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> workItem) => Dispatcher.InvokeAsync(workItem);
}

/// <summary>
/// Binds the renderer's dispatcher to the manager, once. A child of <see cref="BlexProvider"/> rather
/// than part of it: only a component's <see cref="RenderHandle"/> carries the dispatcher, and
/// <see cref="ComponentBase"/> keeps its own to itself. Renders nothing.
/// </summary>
/// <remarks>
/// The binding is never undone. A circuit or a BlazorWebView has one renderer for as long as its scope
/// -- and so its manager -- lives, and several providers in one scope (one per interactive island) all
/// bind that same dispatcher; unbinding when one of them goes would leave the others unchecked.
/// </remarks>
internal sealed class BlexDispatcherBinding : IComponent
{
    /// <summary>The parameter carrying the manager to bind.</summary>
    public const string ManagerParameter = "Manager";

    private RenderHandle _renderHandle;

    public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<BlexManager>(ManagerParameter, out var manager) && manager is { Dispatcher: null })
            manager.Dispatcher = new BlexRendererDispatcher(_renderHandle.Dispatcher);

        return Task.CompletedTask;
    }
}
