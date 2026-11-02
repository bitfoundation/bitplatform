using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// The demo's product list: 120 rows that re-render on every keystroke. Reported one finding per row and one per batch,
/// a few keystrokes buried the Diagnostics panel under two hundred cards that all said the same thing.
/// </summary>
[TestClass]
public class DiagnosticGroupingTests : BunitContext
{
    public DiagnosticGroupingTests() => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.Diagnostics.RenderCascadeSize = 10;
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void The_rows_of_a_list_that_follow_their_parent_are_one_finding()
    {
        var cut = Render<RowList>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        Session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.parent-driven", Session.Diagnostics.Findings);
        Assert.AreEqual("Row ×20 instances re-rendered 12 times each, every one of them because Parent RowList re-rendered", finding.Diagnostic.Title);
        Assert.Contains("across the 20 instances", finding.Diagnostic.Evidence, StringComparison.Ordinal);
        Assert.IsNotNull(finding.Diagnostic.ComponentInstanceId);
    }

    [TestMethod]
    public void Batches_of_the_same_cascade_are_one_finding()
    {
        var cut = Render<RowList>();
        for (var i = 0; i < 5; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        Session.RenderTracker.CloseBatch();
        Session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.cascade", Session.Diagnostics.Findings);
        Assert.AreEqual("6 render batches touched up to 21 components each", finding.Diagnostic.Title);
        Assert.Contains("Started with RowList", finding.Diagnostic.Evidence, StringComparison.Ordinal);
        Assert.HasCount(6, finding.Diagnostic.RelatedEventIds!);
    }

    public sealed class RowList : ComponentBase
    {
        private int _version;

        public void Bump()
        {
            _version++;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var i = 0; i < 20; i++)
            {
                builder.OpenComponent<Row>(0);
                builder.SetKey(i);
                builder.AddComponentParameter(1, nameof(Row.Item), new object());
                builder.AddComponentParameter(2, nameof(Row.Version), _version);
                builder.CloseComponent();
            }
        }
    }

    public sealed class Row : ComponentBase
    {
        [Parameter] public object? Item { get; set; }

        [Parameter] public int Version { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Version);
    }
}
