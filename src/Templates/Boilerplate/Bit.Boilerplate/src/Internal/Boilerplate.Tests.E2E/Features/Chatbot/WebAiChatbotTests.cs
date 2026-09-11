using Microsoft.Playwright.TestAdapter;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAiChatbotTests : AiChatbotTestsBase
{
    private const string PwaQuestion = "How do I install the PWA version of this app?";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// The browser this run drives, as the assistant would name it. <c>BROWSER</c> in .runsettings picks it, so the
    /// same test asserts something different per engine.
    /// </summary>
    // Qualified because the inherited BrowserType property (an IBrowserType) shadows the type of the same name.
    private static string ExpectedBrowser => PlaywrightSettingsProvider.BrowserName switch
    {
        Microsoft.Playwright.BrowserType.Firefox => "Mozilla Firefox",
        Microsoft.Playwright.BrowserType.Webkit => "Safari",
        _ => "Google Chrome, or another Chromium based browser such as Edge",
    };

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public override Task Assistant_Should_SayAQuestionIsNotItsBusiness_WhenItIsNotAboutTheApp(App app)
        => base.Assistant_Should_SayAQuestionIsNotItsBusiness_WhenItIsNotAboutTheApp(app);

    /// <summary>
    /// Installing a PWA differs per browser, and the assistant is only ever given the device string the client built
    /// from the user agent (See <c>AppClientCoordinator</c> setting <c>TelemetryContext.Platform</c>, read by the
    /// system prompt as <c>{{DeviceInfo}}</c>). So the steps are right only if that string arrived and was understood.
    /// <para>
    /// Not firefox: desktop firefox has no install at all, so a correct answer either sends the user to another
    /// browser or gives steps that do not work - and there is no right answer left to assert.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task Assistant_Should_ExplainPwaInstallation_ForTheBrowserTheRunIsOn()
    {
        if (PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Firefox)
            Assert.Inconclusive("Desktop firefox cannot install a PWA, so there are no firefox steps for the assistant to get right.");

        var panel = await OpenChatPanel(App.Sales);

        var answer = await panel.Ask(PwaQuestion);

        await AiAnswerJudge.AssertAnswer(PwaQuestion,
            $"""
            The assistant explains how to install this web app to the device, and the steps it gives are the ones for
            {ExpectedBrowser} - it has recognised which browser the user is on. Steps for a different browser, or
            steps that avoid naming any browser and could be about any of them, fail.
            """,
            answer, TestContext.CancellationToken);
    }

    /// <summary>
    /// A turn reaches the panel as one json document carrying the answer, its suggestions and the signature (See
    /// <c>AssistantTurn</c>). Only the answer is the user's to read, so none of the document's own words may reach the
    /// screen - which is what a panel rendering the stream instead of reading it would put there.
    /// </summary>
    [TestMethod]
    public async Task Assistant_Should_NotRenderTheStreamsOwnDocumentIntoAnAnswer()
    {
        var panel = await OpenChatPanel(App.Sales);

        var answer = await panel.Ask(PwaQuestion);

        foreach (var ofTheDocument in new[] { "followUpSuggestions", "sentAt", "signature" })
        {
            Assert.DoesNotContain(ofTheDocument, answer, StringComparison.OrdinalIgnoreCase,
                $"""
                The answer carries part of the document it travelled in, so the panel is showing the stream rather
                than reading it.

                The answer: {answer}
                """);
        }
    }
}
