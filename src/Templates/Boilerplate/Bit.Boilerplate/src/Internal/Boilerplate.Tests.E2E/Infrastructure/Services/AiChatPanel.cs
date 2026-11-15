namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// The app's AI chat panel (AppAiChatPanel.razor, around AppAiChat's BitAiChat), driven the way a user drives it. Every
/// app in <see cref="DeployedApps"/> renders the same component, so one page object serves web, Android and Windows
/// alike. Its controls are found by the names the app gives them (See AiChatStrings), and its messages by the role
/// Bit.BlazorUI.AI marks each with, rather than by the library's own class names.
/// </summary>
public sealed class AiChatPanel
{
    /// <summary>How long a model call plus however many tools it runs gets, over a real connection.</summary>
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromMinutes(3);

    private readonly IPage page;

    private AiChatPanel(IPage page) => this.page = page;

    /// <summary>Every locator is scoped to the panel: the app behind it has its own buttons and spinners.</summary>
    private ILocator Panel => page.Locator(".panel-cnt");

    private ILocator Messages => Panel.Locator("[data-message-id][data-role]");

    private ILocator Answers => Panel.Locator("[data-message-id][data-role=assistant]");

    /// <summary>Stop takes Send's place for exactly as long as an answer is on its way.</summary>
    private ILocator AnswerInFlight => Button(AppStrings.AiChatStop);

    public ILocator MessageBox => Panel.Locator("textarea").First;

    public ILocator SendButton => Button(AppStrings.Send);

    /// <summary>Speech in. Absent where the browser cannot record.</summary>
    public ILocator DictateButton => Button(AppStrings.AiChatPanelDictate);

    /// <summary>What <see cref="DictateButton"/> turns into while the microphone is open.</summary>
    public ILocator StopDictationButton => Button(AppStrings.AiChatPanelStopDictation);

    /// <summary>The image picker's own input, for <c>SetInputFilesAsync</c>; the visible control is the paperclip.</summary>
    public ILocator AttachmentInput => Panel.Locator("input[type=file]").First;

    /// <summary>A file the composer took, shown by its name until the message goes with it.</summary>
    public ILocator PendingAttachment(string fileName) => Panel.GetByText(fileName);

    /// <summary>
    /// Opens the panel and waits for its message box, which is there once the lazily loaded chat is - so it means the
    /// chat rendered, not just that the click landed. The launcher is in the prerendered html too and a click before
    /// the app takes over is swallowed, so it retries against a deadline (like <c>SmokeTestsBase</c>).
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
                await Assertions.Expect(panel.MessageBox).ToBeVisibleAsync(new() { Timeout = 5_000 });
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
        var answersBefore = await Answers.CountAsync();

        await MessageBox.FillEnsuringStable(message);

        // Disabled until the images picked for it have gone up (See AppAiChat.UploadImage).
        await Assertions.Expect(SendButton).ToBeEnabledAsync(new() { Timeout = (float)TimeSpan.FromMinutes(1).TotalMilliseconds });
        await SendButton.ClickAsync();

        // The answer is added as it starts, before it has any content.
        await Assertions.Expect(Answers).ToHaveCountAsync(answersBefore + 1);

        return await ReadLastAnswer();
    }

    /// <summary>
    /// Waits for the answer, then reads it. On the stop button, not the text: an answer streams in word by word, so
    /// text that pauses isn't text that finished - and an empty answer never grows at all.
    /// </summary>
    public async Task<string> ReadLastAnswer()
    {
        await Assertions.Expect(AnswerInFlight).ToHaveCountAsync(0, new() { Timeout = (float)AnswerTimeout.TotalMilliseconds });

        return (await Answers.Last.InnerTextAsync()).Trim();
    }

    /// <summary>The names on the product cards of the conversation (See ProductsCard), in the order they are numbered.</summary>
    public async Task<string[]> ReadProductCards()
    {
        // Each title carries its number: "2. EQE SUV".
        return [.. (await Panel.Locator(".products .product-name").AllInnerTextsAsync()).Select(title => title.Trim().Split(". ", 2) is [_, var name] ? name : title.Trim())];
    }

    /// <summary>
    /// Throws the conversation away and starts a new one - worth doing between questions, since an assistant that
    /// has gone quiet in a conversation stays quiet.
    /// </summary>
    public async Task Clear()
    {
        await Button(AppStrings.Clear).ClickAsync();

        await Assertions.Expect(Messages).ToHaveCountAsync(0);
    }

    private ILocator Button(string name) => Panel.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
}
