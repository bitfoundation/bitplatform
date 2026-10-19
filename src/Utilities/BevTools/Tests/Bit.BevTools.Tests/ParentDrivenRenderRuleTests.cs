using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// The pattern a rate-based "render storm" rule cannot see: a component that re-renders steadily and only ever
/// because an ancestor did. This is the demo's first scenario and the most common avoidable cost in Blazor apps.
/// </summary>
[TestClass]
public class ParentDrivenRenderRuleTests : BunitContext
{
    public ParentDrivenRenderRuleTests() => Services.AddBitBevToolsServices(o => o.Enabled = true);

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_child_that_only_ever_follows_its_parent_is_reported_end_to_end()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));

        // Twelve parent re-renders: nowhere near a "storm" (30 in 2 s), but every child render is avoidable work.
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Increment());
        }

        Session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.parent-driven", Session.Diagnostics.Findings);
        Assert.Contains("ChildComponent", finding.Diagnostic.Title, StringComparison.Ordinal);
        Assert.Contains("ParentComponent", finding.Diagnostic.Title, StringComparison.Ordinal);
        Assert.Contains("ShouldRender", finding.Diagnostic.Suggestion!, StringComparison.Ordinal);
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Diagnostic.Severity);
    }

    [TestMethod]
    public void A_component_that_renders_for_its_own_reasons_is_not_reported()
    {
        var cut = Render<ParentComponent>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        Session.Diagnostics.Evaluate(force: true);

        // The parent itself renders because of StateHasChanged, not because of an ancestor.
        Assert.DoesNotContain(f =>
            f.Diagnostic.RuleId == "render.parent-driven" && f.Diagnostic.Title.StartsWith("ParentComponent", StringComparison.Ordinal), Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Framework_internals_are_never_reported_because_nobody_can_fix_them()
    {
        var cut = Render<FrameworkChildHost>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        Session.Components.Refresh(force: true);
        Session.Diagnostics.Evaluate(force: true);

        var hidden = Session.Components.All.Where(r => r.IsHidden).Select(r => r.DisplayName).ToHashSet();
        Assert.IsNotEmpty(hidden);
        Assert.DoesNotContain(f =>
            f.Diagnostic.RuleId == "render.parent-driven" && hidden.Any(h => f.Diagnostic.Title.StartsWith(h, StringComparison.Ordinal)), Session.Diagnostics.Findings);
    }
}

public class FrameworkChildHost : ComponentBase
{
    private int _counter;

    public void Bump()
    {
        _counter++;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // CascadingValue lives in Microsoft.AspNetCore.Components, which BevTools treats as a framework internal.
        builder.OpenComponent<CascadingValue<int>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<int>.Value), _counter);
        builder.AddComponentParameter(2, nameof(CascadingValue<int>.ChildContent), (RenderFragment)(b => b.AddContent(0, _counter)));
        builder.CloseComponent();
    }
}
