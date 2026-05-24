using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Placeholder that renders the matched child route inside its parent route's content.
/// Equivalent to React Router's <c>&lt;Outlet/&gt;</c> and Vue Router's <c>&lt;router-view/&gt;</c>.
/// </summary>
public class Outlet : ComponentBase, IDisposable
{
    [CascadingParameter(Name = "ParentRoute")] internal Route? Parent { get; set; }


    private Route? _matchedChild;
    private RouteParameters _parameters = RouteParameters.Empty;

    internal void Render(Route route, RouteParameters parameters)
    {
        _matchedChild = route;
        _parameters = parameters;
        StateHasChanged();
    }


    protected override Task OnInitializedAsync()
    {
        if (Parent is null)
            throw new InvalidOperationException("An Outlet must be placed inside a Brouter route.");

        Parent.Outlet = this;

        return base.OnInitializedAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        if (_matchedChild is null) return;

        builder.OpenComponent<CascadingValue<Outlet>>(0);
        builder.AddAttribute(1, "Name", "Outlet");
        builder.AddAttribute(2, "Value", this);

        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            // Re-establish ParentRoute for any nested routes declared inside the matched child's content,
            // so they can register themselves and recurse correctly.
            b.OpenComponent<CascadingValue<Route>>(0);
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
                    RouteRenderer.ApplyTypedParameters(b2, _matchedChild.Component, _parameters, _matchedChild.Brouter?.CurrentLocation);
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
