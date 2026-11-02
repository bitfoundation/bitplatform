namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The AI assistant sample, which answers from sample data.</summary>
[TestClass, TestCategory("UITest")]
public class AssistantUITests : WorkbenchPageTest
{
    [TestMethod]
    public async Task TheAssistant_Should_AnswerASuggestedQuestion()
    {
        await OpenPreview("assistant", "?device=desktop-1536");

        await Expect(Page.GetByText(WorkbenchStrings.AssistantWelcomeTitle)).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AssistantSalesPrompt }).First.ClickAsync();

        await Expect(Page.GetByText("34,820").First).ToBeVisibleAsync();
    }
}
