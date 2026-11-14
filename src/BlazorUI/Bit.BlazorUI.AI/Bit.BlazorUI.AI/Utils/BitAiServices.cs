namespace Bit.BlazorUI;

/// <summary>
/// What <c>AddBitBlazorUIAIServices</c> would register, handed to the AI components on the page instead: cascaded (a
/// <c>CascadingValue</c> of it, or the <see cref="BitAiChat.Services"/> of a chat) to every AI component inside it, which
/// reads each service set here ahead of the registered one.
/// </summary>
/// <remarks>
/// For an app that registers nothing of the package at startup - one that lazy-loads its assemblies
/// (<c>BlazorWebAssemblyLazyLoad</c>), where calling <c>AddBitBlazorUIAIServices</c> in <c>Program.cs</c> would load
/// them there - or a page that wants services of its own beside the app-wide ones. A service left unset falls back to
/// the registered one, else to the built-in behaviour; a nested cascade replaces the outer one as a whole.
/// <br />
/// The UI options are not one of these: they are the <see cref="BitAiChat.UiOptions"/> of a chat, or a
/// <see cref="BitAiOptionsProvider"/> around the components.
/// </remarks>
public sealed class BitAiServices
{
    /// <summary>
    /// The transport a <see cref="BitAiChat"/> handed no <see cref="BitAiChat.Session"/> creates its session over, in
    /// place of the registered one (<c>UseTransport</c>, <c>UseSseTransport</c>, <c>UseWebSocketTransport</c>). The
    /// session is the chat's own, so it follows the chat's <see cref="BitAiChat.SessionOptions"/> and UI options.
    /// </summary>
    public IBitAiTransport? Transport { get; init; }

    /// <summary>
    /// The tools a session the chat creates may run (<c>AddTool</c>), and the ones whose definitions a tool call's card
    /// is headed by (<see cref="BitAiToolDefinition.DisplayName"/>, <see cref="BitAiToolDefinition.Icon"/>) until the
    /// session puts one on the call.
    /// </summary>
    public IBitAiToolRegistry? Tools { get; init; }

    /// <summary>
    /// What decides which calls of a session the chat creates need a human decision (<c>UseApprovalPolicy</c>).
    /// </summary>
    public IBitAiToolApprovalPolicy? ApprovalPolicy { get; init; }

    /// <summary>
    /// The renderers (<c>ConfigureRenderers</c>). A <c>CascadingValue</c> of a <see cref="BitAiRendererRegistry"/> of
    /// its own, nearer the component, still wins over this one.
    /// </summary>
    public BitAiRendererRegistry? Renderers { get; init; }

    /// <summary>
    /// The components a model may name in a generative UI part (<c>ConfigureGenerativeUi</c>).
    /// </summary>
    public IBitAiGenerativeUiRegistry? GenerativeUi { get; init; }

    /// <summary>
    /// The models (<c>AddModels</c>): what a <see cref="BitAiModelSelector"/> handed no <c>Models</c> or <c>Catalog</c>
    /// offers, and the names a reply's metadata shows its model by.
    /// </summary>
    public IBitAiModelCatalog? ModelCatalog { get; init; }

    /// <summary>
    /// The text-to-speech a chat reads its replies aloud through when handed no <see cref="BitAiChat.Synthesizer"/>
    /// (<c>UseTextToSpeech</c>).
    /// </summary>
    public IBitAiTextToSpeech? TextToSpeech { get; init; }

    /// <summary>
    /// The speech-to-text the composer's dictation transcribes through when handed no transcriber
    /// (<c>UseSpeechToText</c>).
    /// </summary>
    public IBitAiSpeechToText? SpeechToText { get; init; }



    // The service of the type a component looks up, if this sets it: the types the registration registers them as.
    internal object? Find(Type serviceType)
    {
        if (serviceType == typeof(IBitAiTransport)) return Transport;
        if (serviceType == typeof(IBitAiToolRegistry)) return Tools;
        if (serviceType == typeof(IBitAiToolApprovalPolicy)) return ApprovalPolicy;
        if (serviceType == typeof(BitAiRendererRegistry)) return Renderers;
        if (serviceType == typeof(IBitAiGenerativeUiRegistry)) return GenerativeUi;
        if (serviceType == typeof(IBitAiModelCatalog)) return ModelCatalog;
        if (serviceType == typeof(IBitAiTextToSpeech)) return TextToSpeech;
        if (serviceType == typeof(IBitAiSpeechToText)) return SpeechToText;

        return null;
    }
}
