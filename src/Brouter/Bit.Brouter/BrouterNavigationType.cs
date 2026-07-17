namespace Bit.Brouter;

/// <summary>
/// How the current navigation was initiated. Exposed on <see cref="BrouterNavigationContext.NavigationType"/>
/// so guards, loaders and hooks can tell a fresh push from a Back/Forward (which some scroll-restoration
/// and analytics logic needs to special-case). Mirrors Vue Router's navigation type ('push'/'replace'/'pop').
/// </summary>
public enum BrouterNavigationType
{
    /// <summary>
    /// A new history entry was pushed. This covers intercepted link clicks, a programmatic
    /// <see cref="IBrouter.Navigate"/> / <see cref="IBrouter.NavigateToName"/> without <c>replace</c>,
    /// and internal redirects. The initial page load is also reported as <see cref="Push"/>.
    /// </summary>
    Push = 0,

    /// <summary>
    /// The current history entry was replaced rather than a new one pushed: a programmatic navigation
    /// with <c>replace: true</c> (including <see cref="BrouterLink.Replace"/> links) or an internal
    /// address-bar restore after a cancelled navigation.
    /// </summary>
    Replace = 1,

    /// <summary>
    /// A history traversal - browser Back/Forward, or <see cref="IBrouter.Back"/> /
    /// <see cref="IBrouter.Forward"/>. This is the "Back navigation" case scroll restoration and
    /// analytics typically treat differently from a fresh navigation.
    /// </summary>
    /// <remarks>
    /// Detection relies on the navigation going through Brouter's own primitives (links, <see cref="IBrouter"/>).
    /// A raw <c>NavigationManager.NavigateTo</c> call that bypasses <see cref="IBrouter"/> is indistinguishable
    /// from a history traversal at the framework level and will be reported as <see cref="Pop"/>; route
    /// programmatic navigations through <see cref="IBrouter"/> to be classified correctly.
    /// </remarks>
    Pop = 2
}
