namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiOptionsProvider at /e2e/aioptionsprovider in a real browser: new options handed to a provider re-render
/// every AI component inside it in the live DOM - the strings of a chat (the transcript's name, the composer, the
/// actions), a list of conversations, an empty state and a scroll-to-latest button switch to German and back - an inner
/// provider overriding the outer one, a provider with no options handing on the outer ones (or the registered ones), the
/// UiOptions of a chat winning over its provider, and the provider adding no element of its own.
/// </summary>
[TestClass]
public class BitAiOptionsProviderE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aioptionsprovider";

    private ILocator Chat => Page.Locator("#opt-chat");

    private ILocator Answer => Chat.Locator("article[data-role='assistant']");

    [TestMethod]
    public async Task Strings_NewOptionsHandedToTheProvider_RelabelEveryComponentInsideIt()
    {
        await OpenAsync(Route);

        await ExpectEnglishAsync();

        await Page.Locator("#opt-toggle").ClickAsync();
        await Expect(Page.Locator("#opt-language")).ToHaveTextAsync("German");

        await Expect(Chat.Locator(".bit-aimsl")).ToHaveAttributeAsync("aria-label", "Unterhaltung");
        await Expect(Page.GetByRole(AriaRole.Log, new() { Name = "Unterhaltung" })).ToHaveCountAsync(1);
        await Expect(Chat.Locator("textarea.bit-aipri-inp")).ToHaveAttributeAsync("placeholder", "Nachricht senden...");
        await Expect(Chat.Locator("button.bit-aipri-snd")).ToHaveAttributeAsync("aria-label", "Senden");
        await Expect(Answer.Locator("button[aria-label='Kopieren']")).ToHaveCountAsync(1);
        await Expect(Answer.Locator("button[aria-label='Neu erzeugen']")).ToHaveCountAsync(1);
        await Expect(Answer.Locator("button[aria-label='Gute Antwort']")).ToHaveAttributeAsync("title", "Gute Antwort");
        await Expect(Answer.Locator("button[aria-label='Schlechte Antwort']")).ToHaveCountAsync(1);
        await Expect(Chat.Locator("article[data-role='user'] button[aria-label='Bearbeiten']")).ToHaveCountAsync(1);

        var list = Page.Locator("#opt-list");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Unterhaltungen");
        await Expect(list.Locator(".bit-acb")).ToHaveTextAsync("Neue Unterhaltung");
        await Expect(list.Locator(".bit-aicvl-grp")).ToHaveTextAsync("Heute");
        await Expect(list.Locator(".bit-aicvl-ttl")).ToHaveTextAsync("Ohne Titel");
        await Expect(list.Locator("button[aria-label='Löschen Ohne Titel']")).ToHaveCountAsync(1);
        await Expect(list.GetByRole(AriaRole.List, new() { Name = "Heute" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("#opt-empty .bit-aiems-ttl")).ToHaveTextAsync("Wie kann ich helfen?");
        await Expect(Page.Locator("#opt-empty .bit-aiems-bdy")).ToHaveTextAsync("Stellen Sie eine Frage, um zu beginnen.");
        await Expect(Page.Locator("#opt-stl button")).ToHaveTextAsync("Zur neuesten");

        // And back again.
        await Page.Locator("#opt-toggle").ClickAsync();
        await Expect(Page.Locator("#opt-language")).ToHaveTextAsync("English");
        await ExpectEnglishAsync();
    }

    [TestMethod]
    public async Task Strings_ChangedWhileTheChatIsInUse_KeepTheConversationAndTheDraft()
    {
        await OpenAsync(Route);

        var composer = Chat.Locator("textarea.bit-aipri-inp");
        await composer.FillAsync("a draft in progress");
        await Expect(composer).ToHaveValueAsync("a draft in progress");

        await Page.Locator("#opt-toggle").ClickAsync();

        await Expect(composer).ToHaveAttributeAsync("placeholder", "Nachricht senden...");
        await Expect(composer).ToHaveValueAsync("a draft in progress");
        await Expect(Chat.Locator("article.bit-aicms")).ToHaveCountAsync(2);

        // The chat still answers, in the new language.
        await composer.PressAsync("Enter");
        await Expect(Chat.Locator("article[data-role='assistant']").Last.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: a draft in progress");
        await Expect(Chat.Locator("article[data-role='assistant']").Last.Locator("button[aria-label='Neu erzeugen']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Nested_AnInnerProvider_OverridesTheOuterOneForItsOwnContent()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#opt-outer .bit-aiems-ttl")).ToHaveTextAsync("Wie kann ich helfen?");
        await Expect(Page.Locator("#opt-inner .bit-aiems-ttl")).ToHaveTextAsync("Wie kann ich helfen?");

        // The inner options are the outer ones with the actions switched off; outside it they are still shown.
        await Expect(Page.Locator("#opt-inner-message .bit-aimac-act")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#opt-outer-message .bit-aimac-act button[aria-label='Kopieren']")).ToHaveCountAsync(1);

        // A provider given no options hands on the outer ones.
        await Expect(Page.Locator("#opt-handed-on .bit-aiems-ttl")).ToHaveTextAsync("Wie kann ich helfen?");
    }

    [TestMethod]
    public async Task UiOptions_OfAChat_WinOverItsProvider()
    {
        await OpenAsync(Route);

        var chat = Page.Locator("#opt-own");
        await Expect(chat.Locator(".bit-aimsl")).ToHaveAttributeAsync("aria-label", "Conversation");
        await Expect(chat.Locator(".bit-aiems-ttl")).ToHaveTextAsync("How can I help?");
        await Expect(chat.Locator("textarea.bit-aipri-inp")).ToHaveAttributeAsync("placeholder", "Send a message...");
    }

    [TestMethod]
    public async Task Registered_AProviderWithNothingAround_HandsOnTheRegisteredOptions()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#opt-registered .bit-aiems-ttl")).ToHaveTextAsync("How can I help?");

        // The provider adds no element of its own: what it holds is a child of the element around it.
        Assert.IsTrue(await Page.Locator("#opt-registered").EvaluateAsync<bool>("e => e.parentElement.id === 'opt-plain'"));
    }

    private async Task ExpectEnglishAsync()
    {
        await Expect(Chat.Locator(".bit-aimsl")).ToHaveAttributeAsync("aria-label", "Conversation");
        await Expect(Chat.Locator("textarea.bit-aipri-inp")).ToHaveAttributeAsync("placeholder", "Send a message...");
        await Expect(Chat.Locator("button.bit-aipri-snd")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(Answer.Locator("button[aria-label='Copy']")).ToHaveCountAsync(1);
        await Expect(Answer.Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(1);
        await Expect(Answer.Locator("button[aria-label='Good response']")).ToHaveAttributeAsync("title", "Good response");
        await Expect(Chat.Locator("article[data-role='user'] button[aria-label='Edit']")).ToHaveCountAsync(1);

        var list = Page.Locator("#opt-list");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Conversations");
        await Expect(list.Locator(".bit-acb")).ToHaveTextAsync("New conversation");
        await Expect(list.Locator(".bit-aicvl-grp")).ToHaveTextAsync("Today");
        await Expect(list.Locator(".bit-aicvl-ttl")).ToHaveTextAsync("Untitled");
        await Expect(list.Locator("button[aria-label='Delete Untitled']")).ToHaveCountAsync(1);

        await Expect(Page.Locator("#opt-empty .bit-aiems-ttl")).ToHaveTextAsync("How can I help?");
        await Expect(Page.Locator("#opt-empty .bit-aiems-bdy")).ToHaveTextAsync("Ask a question to get started.");
        await Expect(Page.Locator("#opt-stl button")).ToHaveTextAsync("Scroll to latest");
    }
}
