using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Chat;

/// <summary>
/// The chat itself, built on Bit.BlazorUI.AI. LandingChat renders it only once those assemblies are loaded, so nothing of
/// theirs appears in a public member here: only private ones, which the code that runs before the load never reaches.
/// </summary>
public partial class LandingChatSurface : IAsyncDisposable
{
    /// <summary>How many of the page's own suggestions are offered under each answer.</summary>
    private const int MaxFollowUps = 3;

    [AutoInject] private SessionStorage sessionStorage = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private ILogger<LandingChatSurface> logger = default!;

    /// <summary>The sample the chat answers about, or null for the gallery's chat, which knows them all.</summary>
    [Parameter] public LandingSite? Site { get; set; }

    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    [Parameter] public string? InitialQuestion { get; set; }

    [Parameter] public string? Class { get; set; }

    private BitAiChatSession? session;
    private BitAiUiOptions uiOptions = default!;
    private IBitComponentParams[] parameters = [];
    private readonly BitAiRendererRegistry renderers = new();

    private string SiteKey => Site?.Key ?? LandingChatContext.Gallery;

    // Per tab and per sample: a reload, a switch of language or a visit to the sample's other pages carries on the same conversation
    private string StorageKey => $"landing-chat-{SiteKey}";

    private string EmptyBody => Site is null
        ? Localizer[nameof(LandingStrings.ChatGalleryEmptyBody)]
        : Localizer[nameof(LandingStrings.ChatEmptyBody), Site.Name];

    protected override async Task OnInitializedAsync()
    {
        var strings = new LandingAiStrings(Localizer);

        uiOptions = BitAiUiOptions.Default with { Strings = strings };

        parameters =
        [
            new BitAiPromptInputParams
            {
                DictationLanguage = CultureInfo.CurrentUICulture.Name,
                // The microphone says that it listens in the page's own way: the browser's speech recognition, which this chat
                // dictates with, shows nothing else for it (See specs/workarounds.md)
                Classes = new() { VoiceInput = new() { Button = new() { Checked = "lp-chat-dictating" } } }
            },
            new BitAiMessageActionsParams { ShowFeedback = false },
            // What the model reasoned, as a line rather than a box: an answer that reasons between its calls shows several
            new BitAiReasoningPartViewParams
            {
                Classes = new()
                {
                    Root = "lp-chat-reasoning",
                    Icon = "lp-chat-reasoning-icon",
                    Body = "lp-chat-reasoning-body",
                    Collapsible = new() { Accordion = new() { Header = "lp-chat-reasoning-toggle" } }
                }
            }
        ];

        renderers.RegisterTool(LandingChatContext.ShowSamplesTool, context => builder =>
        {
            builder.OpenComponent<LandingChatSamples>(0);
            builder.AddComponentParameter(1, nameof(LandingChatSamples.Keys), ShownSamples(context.Call));
            builder.CloseComponent();
        });

        var transport = new LandingAiTransport(SiteKey, NavigationManager.ToAbsoluteUri("api/chat"), serviceProvider.GetService<ILandingChatBackend>());

        session = new BitAiChatSession(transport, await Restore(), new()
        {
            Strings = strings,
            AutoTitle = false,
            // A question asked while an answer is still coming stops that answer and is answered instead
            InterruptOnSubmit = true
        });

        session.TurnCompleted += OnTurnCompleted;

        // The inline chat's question was typed before Bit.BlazorUI.AI had loaded, so it's sent once the chat is here
        if (string.IsNullOrWhiteSpace(InitialQuestion) is false)
        {
            _ = session.SubmitAsync(InitialQuestion);
        }

        await base.OnInitializedAsync();
    }

    // The server checked the keys the model asked for, so the result is read rather than the arguments
    private static string[] ShownSamples(BitAiToolCallPart call)
        => call.Result?.Value is { ValueKind: JsonValueKind.Array } keys
            ? [.. keys.EnumerateArray().Where(key => key.ValueKind is JsonValueKind.String).Select(key => key.GetString()!)]
            : [];

    private void OnTurnCompleted(BitAiMessage answer)
    {
        _ = InvokeAsync(async () =>
        {
            OfferFollowUps(answer);
            await Save();
        });
    }

    // The page's suggestions the visitor hasn't asked yet, under the answer they just read
    private void OfferFollowUps(BitAiMessage answer)
    {
        if (session is null || answer.Status is not BitAiMessageStatus.Complete || answer.FollowUps is not null || Suggestions is not { Count: > 0 })
            return;

        var asked = session.Conversation.Messages.Where(message => message.Role.IsUser)
                                                 .Select(message => message.GetText().Trim())
                                                 .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        answer.SetFollowUps([.. Suggestions.Where(suggestion => asked.Contains(suggestion) is false).Take(MaxFollowUps)]);
    }

    private async Task<BitAiConversation?> Restore()
    {
        try
        {
            return await sessionStorage.GetItem(StorageKey) is { Length: > 0 } json ? BitAiConversationSerializer.FromJson(json) : null;
        }
        catch (Exception exp)
        {
            // Storage turned off, or a conversation an older version wrote: the chat starts over
            LogRestoreFailed(logger, exp, SiteKey);
            return null;
        }
    }

    private async Task Save()
    {
        if (session is null)
            return;

        try
        {
            await sessionStorage.SetItem(StorageKey, BitAiConversationSerializer.ToJson(session.Conversation));
        }
        catch (Exception exp)
        {
            // Keeping the conversation is a convenience; the chat itself goes on without it
            LogSaveFailed(logger, exp, SiteKey);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (session is not null)
        {
            session.TurnCompleted -= OnTurnCompleted;
            await session.DisposeAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The chat of {Site} couldn't read back its conversation")]
    private static partial void LogRestoreFailed(ILogger logger, Exception exception, string site);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The chat of {Site} couldn't keep its conversation")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception, string site);
}
