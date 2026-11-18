using Microsoft.AspNetCore.Components;

namespace Bit.Blex;

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
