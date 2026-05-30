using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Placeholder that renders the matched child route inside its parent route's content.
/// Equivalent to React Router's <c>&lt;Outlet/&gt;</c> and Vue Router's <c>&lt;router-view/&gt;</c>.
/// </summary>
public class BrouterOutlet : ComponentBase, IDisposable
{
    [CascadingParameter(Name = "ParentRoute")] internal BrouterRoute? Parent { get; set; }


    private BrouterRoute? _matchedChild;
    private BrouterRouteParameters _parameters = BrouterRouteParameters.Empty;

    internal void Render(BrouterRoute route, BrouterRouteParameters parameters)
    {
        _matchedChild = route;
        _parameters = parameters;
        StateHasChanged();
    }


    protected override void OnInitialized()
    {
        if (Parent is null)
            throw new InvalidOperationException("An Outlet must be placed inside a Brouter route.");

        Parent.Outlet = this;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        // Also check Matched: when navigating from a child URL back to the parent (or to any
        // URL where no child of this outlet matches), the previously matched child Route never
        // calls Render() again because its renderer skips RenderRoute while Matched == false.
        // Without this guard the outlet would keep rendering the stale child. Brouter resets
        // Matched on all routes at the start of every navigation and only the winning chain
        // is set back to true, so Matched is the authoritative "is this still selected" flag.
        if (_matchedChild is null || _matchedChild.Matched is false) return;

        builder.OpenComponent<CascadingValue<BrouterOutlet>>(0);
        builder.AddAttribute(1, "Name", "Outlet");
        builder.AddAttribute(2, "Value", this);

        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            // Re-establish ParentRoute for any nested routes declared inside the matched child's content,
            // so they can register themselves and recurse correctly.
            b.OpenComponent<CascadingValue<BrouterRoute>>(0);
            b.AddAttribute(1, "Name", "ParentRoute");
            b.AddAttribute(2, "Value", _matchedChild);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(b2 =>
            {
                if (_matchedChild.Content is not null)
                {
                    b2.AddContent(0, _matchedChild.Content(_parameters));
                }
                else if (_matchedChild.Component is not null)
                {
                    b2.OpenComponent(0, _matchedChild.Component);
                    BrouterRouteRenderer.ApplyTypedParameters(b2, _matchedChild.Component, _parameters, _matchedChild.Brouter?.CurrentLocation);
                    b2.CloseComponent();
                }

                // Render any descendant routes declared as ChildContent.
                b2.AddContent(1, _matchedChild.ChildContent);
            }));
            b.CloseComponent();
        }));

        builder.CloseComponent();
    }


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _matchedChild = null;
        // Only detach from the parent if it still points at *this* instance. A newer Outlet may
        // have already taken our place (e.g. after a re-render that recreates the component),
        // and we must not unregister it.
        if (Parent is not null && ReferenceEquals(Parent.Outlet, this)) Parent.Outlet = null;
    }

    private bool _disposed;
}
