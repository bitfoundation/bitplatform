namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// The app's AI chat panel (AppAiChatPanel.razor), driven the way a user drives it. Every app in
/// <see cref="DeployedApps"/> renders the same component, so one page object serves web, Android and Windows alike.
/// </summary>
public sealed class AiChatPanel
{
    /// <summary>How long a model call plus however many tools it runs gets, over a real connection.</summary>
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromMinutes(3);

    private readonly IPage page;

    private AiChatPanel(IPage page) => this.page = page;

    /// <summary>Every locator is scoped to the panel: the app behind it has its own buttons and spinners.</summary>
    private ILocator Panel => page.Locator(".panel-cnt");

    private ILocator Messages => Panel.Locator(".message-row");

    /// <summary>The header's spinner, up for exactly as long as an answer is on its way.</summary>
    private ILocator AnswerInFlight => Panel.Locator(".bit-ldn-rsq");

    public ILocator MessageBox => Panel.Locator(".message-box textarea");

    public ILocator SendButton => Panel.Locator(".send-message-button");

    /// <summary>Speech in. Absent where the browser cannot record - see <c>isDictationSupported</c>.</summary>
    public ILocator DictateButton => Panel.Locator(".dictate-button");

    /// <summary>The image picker's own input, for <c>SetInputFilesAsync</c>; the visible control is the paperclip.</summary>
    public ILocator AttachmentInput => Panel.Locator(".composer input[type=file]");

    /// <summary>
    /// Opens the panel and waits for its greeting, which the panel writes itself - so it means the panel rendered,
    /// not just that the click landed. The launcher is in the prerendered html too and a click before the app takes
    /// over is swallowed, so it retries against a deadline (like <c>SmokeTestsBase</c>).
    /// </summary>
    public static async Task<AiChatPanel> Open(IPage page)
    {
        var panel = new AiChatPanel(page);
        var launcher = page.Locator(".open-panel-button");

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            await launcher.ClickAsync();

            try
            {
                await Assertions.Expect(panel.Messages).ToHaveCountAsync(1, new() { Timeout = 2_000 });
                return panel;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }

    /// <summary>
    /// Sends <paramref name="message"/> and returns what the assistant replied - empty string included, which is how
    /// it declines a question outside its scope.
    /// </summary>
    public async Task<string> Ask(string message)
    {
        var messagesBefore = await Messages.CountAsync();

        await MessageBox.FillEnsuringStable(message);
        await SendButton.ClickAsync();

        // The user's message and the assistant's reply are added together, before the reply has any content.
        await Assertions.Expect(Messages).ToHaveCountAsync(messagesBefore + 2);

        return await ReadLastAnswer();
    }

    /// <summary>
    /// Waits for the answer, then reads it. On the spinner, not the text: an answer streams in word by word, so text
    /// that pauses isn't text that finished - and an empty answer never grows at all.
    /// </summary>
    public async Task<string> ReadLastAnswer()
    {
        await Assertions.Expect(AnswerInFlight).ToHaveCountAsync(0, new() { Timeout = (float)AnswerTimeout.TotalMilliseconds });

        return (await Messages.Last.InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Throws the conversation away and starts a new one - worth doing between questions, since an assistant that
    /// has gone quiet in a conversation stays quiet.
    /// </summary>
    public async Task Clear()
    {
        await Panel.GetByRole(AriaRole.Button, new() { Name = AppStrings.Clear, Exact = true }).ClickAsync();

        await Assertions.Expect(Messages).ToHaveCountAsync(1);
    }
}
