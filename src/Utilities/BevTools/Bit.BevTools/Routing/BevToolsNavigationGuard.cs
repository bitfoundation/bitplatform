namespace Bit.BevTools;

/// <summary>Something that can stop or confirm a navigation before it happens.</summary>
/// <param name="Kind">"NavigationLock" or "LocationChanging handler".</param>
/// <param name="Description">What it does: the handler it runs, whether it confirms leaving the site.</param>
/// <param name="OwnerInstanceId">The component that registered it, when it is one BevTools tracks.</param>
/// <param name="OwnerName">That component's name.</param>
public sealed record BevToolsNavigationGuard(string Kind, string Description, long? OwnerInstanceId, string? OwnerName);
