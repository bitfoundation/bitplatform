namespace Bit.Bonboarding;

/// <summary>
/// What the guidance on the page (checklists, lists of tours) needs from the engine beyond <see cref="IBonboardingService"/>:
/// the checklists on screen, so a signal can tick their items off, and word of changes to stored records, registrations
/// and checklist progress, so what they show can follow.
/// </summary>
/// <remarks>
/// Injected on its own rather than reached through the service, so it keeps working when the application decorates
/// <see cref="IBonboardingService"/> (a logging or telemetry wrapper, say): the service underneath still reports here.
/// </remarks>
internal sealed class BonboardingGuidanceRegistry
{
    // Checklists register on the renderer while signals are raised from anywhere.
    private readonly object _sync = new();
    private readonly List<BonboardingChecklistDefinition> _checklists = [];

    /// <summary>Raised when stored records changed, here or elsewhere, so a list of tours on screen can follow.</summary>
    public event EventHandler? RecordsChanged;

    /// <summary>Raised when a tour is registered or unregistered, so a list of tours on screen can follow.</summary>
    public event EventHandler? RegistrationsChanged;

    /// <summary>Raised with a checklist id whenever its stored progress changes, so the checklists on screen refresh.</summary>
    public event EventHandler<string>? ChecklistChanged;

    /// <summary>The checklists on screen. A checklist rendered twice is here twice.</summary>
    public BonboardingChecklistDefinition[] Checklists
    {
        get { lock (_sync) return _checklists.ToArray(); }
    }

    public void RegisterChecklist(BonboardingChecklistDefinition checklist)
    {
        lock (_sync) _checklists.Add(checklist);
    }

    public void UnregisterChecklist(BonboardingChecklistDefinition checklist)
    {
        lock (_sync) _checklists.Remove(checklist);
    }

    public void RaiseRecordsChanged() => RecordsChanged?.Invoke(this, EventArgs.Empty);

    public void RaiseRegistrationsChanged() => RegistrationsChanged?.Invoke(this, EventArgs.Empty);

    public void RaiseChecklistChanged(string checklistId) => ChecklistChanged?.Invoke(this, checklistId);
}
