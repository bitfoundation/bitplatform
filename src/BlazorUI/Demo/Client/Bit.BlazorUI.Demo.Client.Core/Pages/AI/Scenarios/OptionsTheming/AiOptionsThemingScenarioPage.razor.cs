using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.OptionsTheming;

public partial class AiOptionsThemingScenarioPage
{
    private static readonly BitAiStrings French = new FrenchAiStrings();

    private BitAiChatSession? _session;
    private BitAiUiOptions _options = BitAiUiOptions.Default;

    private string? _designSystem = "fluent";
    private string? _scheme = "light";
    private string? _density = "1";
    private bool _rtl;
    private bool _french;
    private bool _showUsage = true;
    private bool _showModelName = true;
    private bool _showTimestamps;
    private bool _showMessageActions = true;
    private bool _showLineNumbers;

    private readonly string[] _suggestions = ["Show me what changed"];

    [AutoInject] private BitAiChatSessionFactory _sessionFactory = default!;

    protected override Task OnInitAsync()
    {
        _session = _sessionFactory.Create(AiDemoConversations.Long(6));
        RebuildOptions();

        return base.OnInitAsync();
    }

    // A new record rather than a mutation: the options are immutable, and a new instance is what tells the provider to
    // hand the change down.
    private void RebuildOptions() => _options = BitAiUiOptions.Default with
    {
        Strings = _french ? French : BitAiStrings.Default,
        ShowUsage = _showUsage,
        ShowModelName = _showModelName,
        ShowTimestamps = _showTimestamps,
        ShowMessageActions = _showMessageActions,
        ShowLineNumbers = _showLineNumbers,
    };

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }

    /// <summary>
    /// A few of the strings in French: a subclass of <see cref="BitAiStrings"/> overrides what it translates and keeps
    /// the English of the rest.
    /// </summary>
    private sealed class FrenchAiStrings : BitAiStrings
    {
        public override string PromptPlaceholder => "Écrivez un message...";
        public override string Send => "Envoyer";
        public override string Stop => "Arrêter";
        public override string Copy => "Copier";
        public override string Copied => "Copié";
        public override string Regenerate => "Régénérer";
        public override string GoodResponse => "Bonne réponse";
        public override string BadResponse => "Mauvaise réponse";
        public override string ScrollToLatest => "Aller au plus récent";
        public override string Thinking => "Réflexion";
        public override string Sources => "Sources";
        public override string AttachFile => "Joindre des fichiers";
        public override string ShowDetails => "Afficher les détails";
    }
}
