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

        var seq = 0;
        builder.OpenComponent<CascadingValue<Outlet>>(seq++);
        builder.AddAttribute(seq++, "Name", "Outlet");
        builder.AddAttribute(seq++, "Value", this);

        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b =>
        {
            // Re-establish ParentRoute for any nested routes declared inside the matched child's content,
            // so they can register themselves and recurse correctly.
            b.OpenComponent<CascadingValue<Route>>(seq++);
            b.AddAttribute(seq++, "Name", "ParentRoute");
            b.AddAttribute(seq++, "Value", _matchedChild);
            b.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 =>
            {
                if (_matchedChild.Content is not null)
                {
                    b2.AddContent(seq, _matchedChild.Content(_parameters));
                }
                else if (_matchedChild.Component is not null)
                {
                    b2.OpenComponent(seq++, _matchedChild.Component);
                    b2.CloseComponent();
                }

                // Render any descendant routes declared as ChildContent.
                b2.AddContent(seq, _matchedChild.ChildContent);
            }));
            b.CloseComponent();
        }));

        builder.CloseComponent();
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _matchedChild = null;
        if (Parent is not null) Parent.Outlet = null;

        _disposed = true;
    }
}
