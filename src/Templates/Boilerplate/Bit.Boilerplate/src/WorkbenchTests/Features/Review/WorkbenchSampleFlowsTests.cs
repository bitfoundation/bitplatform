using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>The flows every Workbench starts with, written by hand, so nothing but these tests keeps them in line with the pages.</summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchSampleFlowsTests
{
    [TestMethod]
    public async Task TheSamples_Should_ShowPagesTheAppHasJoinedByArrowsThatFindTheirScreens()
    {
        await using var ctx = WorkbenchBunit.Create();

        var flows = WorkbenchSampleFlows.Create(ctx.Services.GetRequiredService<IStringLocalizer<WorkbenchStrings>>());

        Assert.HasCount(2, flows);

        foreach (var flow in flows)
        {
            Assert.IsTrue(flow.IsSample);
            Assert.IsTrue(flow.Screens.All(screen => WorkbenchPageCatalog.Find(screen.PageKey) is not null), flow.Name);
            Assert.IsTrue(flow.Screens.All(screen => string.IsNullOrWhiteSpace(screen.Note) is false), flow.Name);
            Assert.IsTrue(flow.Links.All(link => flow.Screens.Any(screen => screen.Id == link.From) && flow.Screens.Any(screen => screen.Id == link.To)), flow.Name);
            Assert.HasCount(flow.Screens.Length, flow.Screens.Select(screen => (screen.X, screen.Y)).Distinct(), "No screen sits on another.");
        }
    }

    [TestMethod]
    public void TheirSavedStates_Should_BeOnesAPreviewCanRestore()
    {
        foreach (var id in WorkbenchSampleFlows.StateIds)
        {
            Assert.IsInstanceOfType<JsonObject>(JsonNode.Parse(WorkbenchSampleFlows.StateOf(id)!)!["buildings"], id);
        }
    }

    [TestMethod]
    public async Task ASampleWhosePageWasRemoved_Should_BeLeftOut()
    {
        await using var ctx = WorkbenchBunit.Create();

        var flows = WorkbenchSampleFlows.Create(ctx.Services.GetRequiredService<IStringLocalizer<WorkbenchStrings>>(), pageKey => pageKey != "buildings");

        Assert.DoesNotContain(flow => flow.Id == WorkbenchSampleFlows.BuildingsId, flows);
        Assert.HasCount(1, flows);
    }
}
