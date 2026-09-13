namespace Bit.Brouter;

/// <summary>
/// Declares a named view fragment for the enclosing <see cref="Broute"/>: when the route matches,
/// each of its parent's same-named <c>&lt;BrouterOutlet Name="..."&gt;</c> outlets renders the
/// corresponding fragment - so one route can drive multiple regions of its parent layout (main +
/// sidebar + toolbar), Vue Router named-views style. The fragment receives the route's merged
/// parameters, and the route's data/meta cascades are available inside it. Renders nothing at its
/// own declaration site.
/// </summary>
/// <example>
/// <code>
/// &lt;Broute Path="/dashboard"&gt;
///     &lt;Content&gt;
///         &lt;main&gt;&lt;BrouterOutlet /&gt;&lt;/main&gt;
///         &lt;aside&gt;&lt;BrouterOutlet Name="sidebar" /&gt;&lt;/aside&gt;
///     &lt;/Content&gt;
///     &lt;ChildContent&gt;
///         &lt;Broute Path="/stats"&gt;
///             &lt;Content&gt;stats main&lt;/Content&gt;
///             &lt;ChildContent&gt;
///                 &lt;BrouterView Name="sidebar" Context="p"&gt;stats sidebar&lt;/BrouterView&gt;
///             &lt;/ChildContent&gt;
///         &lt;/Broute&gt;
///     &lt;/ChildContent&gt;
/// &lt;/Broute&gt;
/// </code>
/// </example>
public sealed class BrouterView : ComponentBase, IDisposable
{
    [CascadingParameter(Name = "ParentRoute")] internal Broute? Route { get; set; }

    /// <summary>The outlet name this view targets. Must be non-empty (the primary outlet renders the route's <c>Content</c>).</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    /// <summary>The fragment to render in the same-named parent outlet; receives the route's parameters.</summary>
    [Parameter] public RenderFragment<BrouterRouteParameters>? ChildContent { get; set; }

    private Broute? _registeredRoute;
    private string? _registeredName;

    protected override void OnParametersSet()
    {
        if (Route is null)
            throw new InvalidOperationException(
                "A BrouterView must be declared inside a Broute (its ChildContent), whose parent layout hosts the named outlet.");

        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException(
                "BrouterView requires a non-empty Name. The route's main content already renders in the primary (unnamed) outlet via Content/Component.");

        // Re-register every parameter pass: a host re-render produces a fresh fragment instance
        // that the outlet must pick up, and a changed Name - or a new cascaded Route instance -
        // must vacate the old slot on the route it was registered with.
        if (_registeredName is not null &&
            (ReferenceEquals(_registeredRoute, Route) is false
             || string.Equals(_registeredName, Name, StringComparison.Ordinal) is false))
        {
            _registeredRoute?.SetNamedView(_registeredName, null);
        }
        Route.SetNamedView(Name, ChildContent);
        _registeredRoute = Route;
        _registeredName = Name;
    }

    public void Dispose()
    {
        if (_registeredName is not null)
        {
            _registeredRoute?.SetNamedView(_registeredName, null);
            _registeredRoute = null;
            _registeredName = null;
        }
    }
}
