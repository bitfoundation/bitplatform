using Bit.BlazorUI;
using Microsoft.Extensions.AI;
using Boilerplate.Shared.Features.Chatbot;
using static Boilerplate.Tests.Features.Chatbot.TestAiChatPanel;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// An answer that does two or more things the panel shows as a line - changing the theme, opening a page - shows them as
/// one timeline of steps instead, while a single one stays a line (See <c>AppChatbot.AnswerSteps</c>).
/// <para>
/// Both tools are called with arguments they turn down before reaching the user's device, so each step ends at once,
/// with no browser on the other end.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class AiChatAnswerStepsTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnAnswerOfTwoSteps_Should_ShowThemAsItsSteps_InTheOrderTheyRan()
    {
        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => conversation.SelectMany(message => message.Contents.OfType<FunctionResultContent>()).Count() switch
            {
                0 => ToolCall("theme-call", AiChatProtocol.Tools.SetApplicationTheme, "theme", "purple"),
                1 => ToolCall("page-call", AiChatProtocol.Tools.NavigateToPage, "pageUrl", "https://example.com/elsewhere"),
                _ => [new(ChatRole.Assistant, "I couldn't do either of those.")]
            }
        };

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);
        var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<AppStrings>>();

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User);

        var reply = await panel.Ask("make it purple and open example.com", cancellationToken: TestContext.CancellationToken);

        Assert.IsNotNull(reply.Run, "An answer that did two things the panel shows as a line must show them as its steps.");
        Assert.AreEqual(BitAiAgentRunStatus.Completed, reply.Run.Status, "The steps must end with the answer.");

        // A step's id is its call's, which is how the panel knows which lines the steps stand in for (See AppAiChat.IsStep)
        Assert.AreSequenceEqual(["theme-call", "page-call"], reply.Run.Steps.Select(step => step.StepId),
            "Each call must be a step of its own, in the order they ran.");

        Assert.AreSequenceEqual([localizer[nameof(AppStrings.AiChatToolThemeChange)].Value, localizer[nameof(AppStrings.AiChatToolNavigation)].Value],
            reply.Run.Steps.Select(step => step.Title), "A step must be titled the way the panel titles its call's line.");

        Assert.IsTrue(reply.Run.Steps.All(step => step.Status is BitAiAgentStepStatus.Completed),
            $"Each step must end with its call. Got: {string.Join(", ", reply.Run.Steps.Select(step => step.Status))}");

        Assert.HasCount(2, reply.PartsOf<BitAiToolCallPart>(), "The calls must still be part of the answer, for the model's sake on the next turn.");
    }

    [TestMethod]
    public async Task AnAnswerOfOneStep_Should_KeepItsLine_WithNoSteps()
    {
        var chatClient = new TestChatClient
        {
            StreamingUpdates = (_, conversation) => conversation.Any(message => message.Contents.OfType<FunctionResultContent>().Any())
                ? [new(ChatRole.Assistant, "That theme doesn't exist.")]
                : ToolCall("theme-call", AiChatProtocol.Tools.SetApplicationTheme, "theme", "purple")
        };

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var httpContext = SetCurrentHttpContext(scope.ServiceProvider, server.WebAppAddress);

        await using var panel = new TestAiChatPanel(scope.ServiceProvider, httpContext.User);

        var reply = await panel.Ask("make it purple", cancellationToken: TestContext.CancellationToken);

        Assert.IsNull(reply.Run, "A single thing done must stay a line rather than a timeline of one step.");
        Assert.HasCount(1, reply.PartsOf<BitAiToolCallPart>(), "The call must be part of the answer.");
    }

    private static ChatResponseUpdate[] ToolCall(string callId, string toolName, string argument, string value)
        => [new(ChatRole.Assistant, (IList<AIContent>)[new FunctionCallContent(callId, toolName, new Dictionary<string, object?> { [argument] = value })])];
}
