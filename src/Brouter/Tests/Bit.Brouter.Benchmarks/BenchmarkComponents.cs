using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter.Benchmarks;

/// <summary>
/// The trivial page each route renders when matched. Intentionally cheap so the benchmark measures
/// routing/instantiation overhead, not page content.
/// </summary>
public sealed class BenchPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, "page");
        builder.CloseElement();
    }
}

/// <summary>
/// Scenario A - the current Brouter model: every route is a live <see cref="Broute"/> component
/// mounted in the render tree. Emits <c>RouteCount</c> hand-declared <c>&lt;Broute&gt;</c> children,
/// which is the same shape (and the same per-route cost) as the synthetic Broute that Brouter emits
/// per attribute-discovered route - so measuring this measures the discovered-route cost too.
/// </summary>
public sealed class BrouterBenchHost : ComponentBase
{
    [Parameter] public int RouteCount { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Brouter>(0);
        builder.AddAttribute(1, nameof(Brouter.ChildContent), (RenderFragment)(b =>
        {
            var seq = 0;
            for (int i = 0; i < RouteCount; i++)
            {
                b.OpenComponent<Broute>(seq++);
                b.SetKey(i);
                b.AddAttribute(seq++, nameof(Broute.Path), $"/page/{i}");
                b.AddAttribute(seq++, nameof(Broute.Component), typeof(BenchPage));
                b.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

/// <summary>
/// Scenario B - the baseline: models the built-in Blazor <c>Router</c>'s architecture. The routes live
/// as plain data (a template-to-type table), and only the single matched component is instantiated on
/// render. No component is mounted per route. This is the same shape the "lazy discovered routes" design
/// would take, so the gap between this and <see cref="BrouterBenchHost"/> is the instantiation cost the
/// review flagged - measurable at any route count without needing hundreds of real <c>@page</c> types.
/// </summary>
public sealed class RouteTableHost : ComponentBase
{
    [Parameter] public int RouteCount { get; set; }
    [Inject] public NavigationManager Nav { get; set; } = default!;

    // The "RouteTable": route templates as data, not components. Built once per RouteCount.
    private Dictionary<string, Type> _table = new(StringComparer.Ordinal);
    private int _builtFor = -1;

    protected override void OnParametersSet()
    {
        if (_builtFor == RouteCount) return;
        _table = new Dictionary<string, Type>(StringComparer.Ordinal);
        for (int i = 0; i < RouteCount; i++) _table[$"/page/{i}"] = typeof(BenchPage);
        _builtFor = RouteCount;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var path = "/" + Nav.ToBaseRelativePath(Nav.Uri);
        var q = path.IndexOf('?');
        if (q >= 0) path = path[..q];

        // Instantiate only the matched component - exactly what RouteView does in the built-in Router.
        if (_table.TryGetValue(path, out var matched))
        {
            builder.OpenComponent(0, matched);
            builder.CloseComponent();
        }
    }
}
