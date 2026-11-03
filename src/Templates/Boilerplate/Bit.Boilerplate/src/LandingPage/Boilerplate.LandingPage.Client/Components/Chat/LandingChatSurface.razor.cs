namespace Boilerplate.LandingPage.Client.Components.Chat;

public partial class LandingChatSurface : IAsyncDisposable
{
    [AutoInject] private ILandingChatClient chatClient = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    [Parameter] public string? InitialQuestion { get; set; }

    [Parameter] public string? Class { get; set; }

    private BitAiChatSession session = default!;
    private BitAiUiOptions uiOptions = default!;
    private readonly BitAiRendererRegistry renderers = new();

    protected override void OnInitialized()
    {
        var strings = new LandingAiStrings(Localizer);

        uiOptions = BitAiUiOptions.Default with { Strings = strings, ShowMessageActions = false };
        session = new BitAiChatSession(new LandingAiTransport(chatClient, Site.Key), options: new() { Strings = strings, AutoTitle = false });

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The inline chat's question was typed before Bit.BlazorUI.AI had loaded, so it's sent once the chat is on screen
        if (firstRender && string.IsNullOrWhiteSpace(InitialQuestion) is false)
        {
            await session.SubmitAsync(InitialQuestion);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public ValueTask DisposeAsync() => session.DisposeAsync();
}
