namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The shift planner sample.</summary>
[TestClass, TestCategory("UITest")]
public class ShiftsUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheShiftPlanner_Should_SumUpTheWeek()
    {
        await OpenPreview("shifts", "?device=desktop-1536");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading })).ToBeVisibleAsync();
        await Expect(Page.GetByText(new Regex(Regex.Escape(WorkbenchStrings.ShiftsSummary).Replace(@"\{0}", @"\d+").Replace(@"\{1}", @"[\d.,]+")))).ToBeVisibleAsync();
    }
}
