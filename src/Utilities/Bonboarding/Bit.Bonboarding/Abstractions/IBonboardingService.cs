namespace Bit.Bonboarding;

/// <summary>
/// The entry point for starting, querying and controlling tours. Registered as a scoped service by
/// <c>AddBitBonboardingServices</c>, so it is per-circuit on Blazor Server and per-app on WebAssembly.
/// </summary>
public interface IBonboardingService
{
    /// <summary>The session a host should currently render, if any.</summary>
    IBonboardingTourSession? Active { get; }

    /// <summary>Every session created in this scope that has not been discarded.</summary>
    IReadOnlyList<IBonboardingTourSession> Sessions { get; }

    /// <summary>
    /// Every tour registered with <see cref="Register"/> or by a <c>BonboardingTour</c> component.
    /// Starting a tour does not register it.
    /// </summary>
    IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours { get; }

    /// <summary>Raised for every lifecycle moment; the simplest analytics hook.</summary>
    event EventHandler<BonboardingEvent>? EventRaised;

    /// <summary>Raised when <see cref="Active"/> or <see cref="Sessions"/> changes.</summary>
    event EventHandler? SessionsChanged;

    /// <summary>
    /// Makes a tour startable by id, e.g. from a help menu. Declarative
    /// <c>&lt;BonboardingTour&gt;</c> components register themselves automatically.
    /// </summary>
    void Register(BonboardingTourDefinition tour);

    /// <summary>Removes a registration.</summary>
    void Unregister(string tourId);

    /// <summary>Looks up a registered tour.</summary>
    BonboardingTourDefinition? GetTour(string tourId);

    /// <summary>Starts a tour and returns its session.</summary>
    /// <remarks>
    /// A tour started while prerendering runs without a browser: nothing is measured or shown, but
    /// its hooks run, its events are published and its progress is written to the store's
    /// in-memory mirror, and the interactive render then starts it again in a new scope. Start
    /// tours from <c>OnAfterRenderAsync</c>, which never runs while prerendering, from a user
    /// action, or with <c>BonboardingTour.AutoStart</c>.
    /// </remarks>
    Task<IBonboardingTourSession> StartAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Starts a previously registered tour by id.</summary>
    Task<IBonboardingTourSession> StartAsync(string tourId, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the tour only if the user has not already completed or dismissed it. Returns
    /// <see langword="null"/> when it was skipped. This is the call most applications want on first
    /// login.
    /// </summary>
    Task<IBonboardingTourSession?> StartOnceAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Clears persisted progress and starts the tour from the beginning.</summary>
    Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default);

    /// <summary>Ends every running session.</summary>
    Task StopAllAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Cancelled, CancellationToken cancellationToken = default);

    /// <summary>The running session for a tour id, if there is one.</summary>
    IBonboardingTourSession? FindSession(string tourId);

    /// <summary>True when the tour was run to the end at some point.</summary>
    Task<bool> IsCompletedAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>True when the tour was completed, skipped or closed, so it should not auto-start.</summary>
    Task<bool> IsClosedAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Reads the persisted record for a tour.</summary>
    Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Forgets a tour, so it can run again.</summary>
    Task ResetAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Forgets every tour, and every checklist's ticked items and dismissal.</summary>
    Task ResetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells the running tours that something happened in the application, e.g. <c>"project-created"</c>. A step on
    /// screen whose <see cref="BonboardingStepDefinition.AdvanceOnSignal"/> is that signal moves on (if its
    /// <see cref="BonboardingStepDefinition.CanAdvance"/> agrees), and an item of a checklist on screen whose
    /// <see cref="BonboardingChecklistItem.CompleteOnSignal"/> is that signal is ticked off for good. Signals are
    /// compared ordinally and are not remembered: one raised while nothing waits for it does nothing. Returns true
    /// when a tour moved on or an item was ticked off.
    /// </summary>
    Task<bool> SignalAsync(string signal, CancellationToken cancellationToken = default);

    // ---- Checklists -------------------------------------------------------------

    /// <summary>
    /// Ticks a checklist item off (or, with <paramref name="done"/> false, back on), for good: the application's way of
    /// saying the task is done. Stored in the <see cref="IBonboardingStore"/> next to the tours' records.
    /// </summary>
    Task SetChecklistItemAsync(string checklistId, string itemId, bool done = true, CancellationToken cancellationToken = default);

    /// <summary>The items of a checklist ticked off with <see cref="SetChecklistItemAsync"/> (or a signal).</summary>
    Task<IReadOnlyCollection<string>> GetCompletedChecklistItemsAsync(string checklistId, CancellationToken cancellationToken = default);

    /// <summary>Hides a checklist for good, as its dismiss button does.</summary>
    Task DismissChecklistAsync(string checklistId, CancellationToken cancellationToken = default);

    /// <summary>True once the user dismissed the checklist.</summary>
    Task<bool> IsChecklistDismissedAsync(string checklistId, CancellationToken cancellationToken = default);

    /// <summary>Forgets a checklist's ticked items and its dismissal.</summary>
    Task ResetChecklistAsync(string checklistId, CancellationToken cancellationToken = default);
}
