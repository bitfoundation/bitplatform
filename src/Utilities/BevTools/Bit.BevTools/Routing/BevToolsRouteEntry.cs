namespace Bit.BevTools;

/// <summary>One routable page: what the app's router can match.</summary>
/// <param name="Templates">Its <c>@page</c> templates, in declaration order.</param>
/// <param name="PageType">The page's full type name.</param>
/// <param name="PageDisplayName">The page's type name without its namespace.</param>
/// <param name="Assembly">The assembly the router found it in.</param>
/// <param name="Authorization">Its <c>[Authorize]</c> attributes as written ("Authorize(Roles = Admin)"); empty when it has none.</param>
/// <param name="AllowAnonymous">Whether it carries <c>[AllowAnonymous]</c>.</param>
/// <param name="RenderMode">Its <c>[RenderMode*]</c> attribute, or null.</param>
/// <param name="Layout">Its <c>@layout</c>, or null (the router's default layout applies).</param>
public sealed record BevToolsRouteEntry(
    IReadOnlyList<string> Templates,
    string PageType,
    string PageDisplayName,
    string Assembly,
    IReadOnlyList<string> Authorization,
    bool AllowAnonymous,
    string? RenderMode,
    string? Layout);
