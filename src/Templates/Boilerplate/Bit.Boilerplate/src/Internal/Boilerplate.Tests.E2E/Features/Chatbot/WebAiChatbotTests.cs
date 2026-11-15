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
    /// An answer reaches the panel with what the server wrote on it alongside its words - the signature it is sent back
    /// with, what the model is to read of it later (See <c>AiChatProtocol</c>). Only the words are the user's to read,
    /// so none of that may reach the screen.
    /// </summary>
    [TestMethod]
    public async Task Assistant_Should_NotRenderWhatTheServerWroteOnAnAnswerIntoIt()
    {
        var panel = await OpenChatPanel(App.Sales);

        var answer = await panel.Ask(PwaQuestion);

        // CfDJ8 is how every ASP.NET Core data protection payload, a signature included, starts.
        foreach (var ofTheMetadata in new[] { "CfDJ8", "replaySignature" })
        {
            Assert.DoesNotContain(ofTheMetadata, answer, StringComparison.OrdinalIgnoreCase,
                $"""
                The answer shows what the server wrote on it, rather than only its words.

                The answer: {answer}
                """);
        }
    }
}
