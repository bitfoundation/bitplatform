namespace Bit.Bonboarding;

/// <summary>Which browser storage the default store keeps progress in. See <see cref="BonboardingOptions.StorageLocation"/>.</summary>
public enum BonboardingStorageLocation
{
    /// <summary><c>localStorage</c>: progress survives closing the browser, and is shared by its tabs.</summary>
    Local,

    /// <summary>
    /// <c>sessionStorage</c>: progress lasts as long as the tab, so "once" means once per visit. Tabs do not share it,
    /// so there is nothing to keep consistent between them.
    /// </summary>
    Session,
}
