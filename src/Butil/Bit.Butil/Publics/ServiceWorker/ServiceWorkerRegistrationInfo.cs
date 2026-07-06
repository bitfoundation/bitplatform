namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration">ServiceWorkerRegistration</see>.
/// </summary>
public class ServiceWorkerRegistrationInfo
{
    /// <summary>True when a registration was found / created.</summary>
    public bool IsRegistered { get; set; }

    /// <summary>Scope URL the registration applies to.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The active worker's state: <c>"installing"</c>, <c>"installed"</c>, <c>"activating"</c>, <c>"activated"</c>, <c>"redundant"</c>, or null when none.</summary>
    public string? ActiveState { get; set; }

    /// <summary>The installing worker's state, when one is being installed.</summary>
    public string? InstallingState { get; set; }

    /// <summary>The waiting worker's state, when an update is queued.</summary>
    public string? WaitingState { get; set; }

    /// <summary>Update via cache strategy: <c>"imports"</c>, <c>"all"</c>, or <c>"none"</c>.</summary>
    public string? UpdateViaCache { get; set; }
}
