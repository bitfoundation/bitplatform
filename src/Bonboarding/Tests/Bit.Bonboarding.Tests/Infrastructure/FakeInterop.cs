using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding.Tests.Infrastructure;

/// <summary>
/// A scripted stand-in for the browser layer. Because every DOM interaction in the library goes
/// through <see cref="IBonboardingInterop"/>, the entire engine -- waiting, missing targets, focus,
/// keyboard, persistence -- is testable without a browser.
/// </summary>
internal sealed class FakeInterop : IBonboardingInterop
{
    private readonly Dictionary<string, IBonboardingInteropCallbacks> _callbacks = new(StringComparer.Ordinal);

    private long _sequence;

    public bool IsAvailable { get; set; } = true;

    public BonboardingBrowserEnvironment Environment { get; set; } = new();

    /// <summary>Selectors that currently "exist" in the fake document.</summary>
    public HashSet<string> ExistingSelectors { get; } = new(StringComparer.Ordinal);

    /// <summary>Element reference ids that currently "exist".</summary>
    public HashSet<string> ExistingElements { get; } = new(StringComparer.Ordinal);

    public BonboardingRect TargetRect { get; set; } = new(100, 100, 120, 40);

    public BonboardingRect Viewport { get; set; } = new(0, 0, 1024, 768);

    public BonboardingSize PopoverSize { get; set; } = new(320, 200);

    public List<BonboardingStepActivation> Activations { get; } = [];

    public int DeactivateCount { get; private set; }

    public int DetachCount { get; private set; }

    public int FocusPopoverCount { get; private set; }

    public int CaptureFocusCount { get; private set; }

    public int RestoreFocusCount { get; private set; }

    public int MeasureCount { get; private set; }

    public ElementReference? Popover { get; private set; }

    public bool? TrapFocus { get; private set; }

    public Dictionary<string, string> Storage { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// When set, every browser call throws this. Simulates a dropped Blazor Server circuit or a
    /// page being unloaded mid-tour.
    /// </summary>
    public Func<Exception>? Failure { get; set; }

    public IReadOnlyCollection<string> AttachedSessions => _callbacks.Keys;

    public BonboardingStepActivation? LastActivation => Activations.Count == 0 ? null : Activations[^1];

    public ValueTask<BonboardingBrowserEnvironment> InitializeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Environment);

    public ValueTask AttachAsync(string sessionId, IBonboardingInteropCallbacks callbacks, CancellationToken cancellationToken = default)
    {
        _callbacks[sessionId] = callbacks;
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _callbacks.Remove(sessionId);
        DetachCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask<BonboardingStepGeometry> ActivateStepAsync(BonboardingStepActivation activation, CancellationToken cancellationToken = default)
    {
        Throw();
        Activations.Add(activation);
        return ValueTask.FromResult(Measure(activation.Target, activation.AdditionalTargets));
    }

    public ValueTask DeactivateStepAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Throw();
        DeactivateCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask<BonboardingStepGeometry> MeasureAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Throw();
        MeasureCount++;
        var activation = LastActivation;
        return ValueTask.FromResult(activation is null
            ? BonboardingStepGeometry.Missing
            : Measure(activation.Target, activation.AdditionalTargets));
    }

    public ValueTask SetPopoverAsync(string sessionId, ElementReference? popover, bool trapFocus, CancellationToken cancellationToken = default)
    {
        Throw();
        Popover = popover;
        TrapFocus = trapFocus;
        return ValueTask.CompletedTask;
    }

    public ValueTask CaptureFocusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        CaptureFocusCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask RestoreFocusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        RestoreFocusCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask FocusPopoverAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Throw();
        FocusPopoverCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> GetStorageAsync(string key, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Storage.GetValueOrDefault(key));

    public ValueTask SetStorageAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        Storage[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveStorageAsync(string key, CancellationToken cancellationToken = default)
    {
        Storage.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<string>> ListStorageKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<string>>(
            Storage.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray());

    private void Throw()
    {
        if (Failure is { } factory) throw factory();
    }

    // ---- Test-facing helpers ----------------------------------------------

    /// <summary>Marks a selector as present and pushes a fresh measurement to the session.</summary>
    public Task AppearAsync(string sessionId, string selector)
    {
        ExistingSelectors.Add(selector);
        return PushGeometryAsync(sessionId);
    }

    /// <summary>Removes every target and tells the session the element vanished.</summary>
    public async Task LoseTargetAsync(string sessionId)
    {
        ExistingSelectors.Clear();
        ExistingElements.Clear();

        if (_callbacks.TryGetValue(sessionId, out var callbacks))
        {
            await callbacks.OnTargetLostAsync();
        }
    }

    /// <summary>Pushes the current measurement, as the observers would.</summary>
    public async Task PushGeometryAsync(string sessionId)
    {
        if (!_callbacks.TryGetValue(sessionId, out var callbacks)) return;

        var activation = LastActivation;
        var geometry = activation is null
            ? BonboardingStepGeometry.Missing with { Sequence = ++_sequence }
            : Measure(activation.Target, activation.AdditionalTargets);

        await callbacks.OnGeometryChangedAsync(geometry);
    }

    public async Task PressKeyAsync(string sessionId, string key, bool shift = false)
    {
        if (_callbacks.TryGetValue(sessionId, out var callbacks)) await callbacks.OnKeyAsync(key, shift);
    }

    public async Task TriggerAdvanceAsync(string sessionId)
    {
        if (_callbacks.TryGetValue(sessionId, out var callbacks)) await callbacks.OnAdvanceTriggeredAsync();
    }

    public async Task TimeOutWaitAsync(string sessionId)
    {
        if (_callbacks.TryGetValue(sessionId, out var callbacks)) await callbacks.OnWaitTimedOutAsync();
    }

    public async Task ClickOverlayAsync(string sessionId)
    {
        if (_callbacks.TryGetValue(sessionId, out var callbacks)) await callbacks.OnOverlayClickAsync();
    }

    private BonboardingStepGeometry Measure(BonboardingTargetDescriptor target, IReadOnlyList<BonboardingTargetDescriptor>? additional)
    {
        var found = Exists(target);

        if (!found && additional is not null)
        {
            found = additional.Any(Exists);
        }

        return new BonboardingStepGeometry
        {
            Found = found,
            Target = found ? TargetRect : BonboardingRect.Empty,
            Viewport = Viewport,
            Popover = PopoverSize,
            TargetVisible = found,
            Sequence = ++_sequence,
        };
    }

    private bool Exists(BonboardingTargetDescriptor descriptor) => descriptor.Kind switch
    {
        BonboardingTargetKind.Selector => descriptor.Selector is not null && ExistingSelectors.Contains(descriptor.Selector),
        BonboardingTargetKind.Element => descriptor.Element is { } element && ExistingElements.Contains(element.Id),
        _ => false,
    };

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
