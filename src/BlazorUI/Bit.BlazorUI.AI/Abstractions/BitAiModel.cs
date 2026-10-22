namespace Bit.BlazorUI;

/// <summary>
/// What a model can do, used to enable or hide UI affordances such as the attachment button.
/// </summary>
[Flags]
public enum BitAiModelCapabilities
{
    /// <summary>No declared capabilities.</summary>
    None = 0,

    /// <summary>Produces output incrementally.</summary>
    Streaming = 1 << 0,

    /// <summary>Accepts images as input.</summary>
    Vision = 1 << 1,

    /// <summary>Accepts audio as input.</summary>
    Audio = 1 << 2,

    /// <summary>Can call tools.</summary>
    Tools = 1 << 3,

    /// <summary>Can be constrained to a JSON schema.</summary>
    StructuredOutput = 1 << 4,

    /// <summary>Emits visible reasoning before the answer.</summary>
    Reasoning = 1 << 5,

    /// <summary>Accepts documents such as PDFs as input.</summary>
    Documents = 1 << 6,

    /// <summary>Returns citations for retrieved sources.</summary>
    Citations = 1 << 7,
}

/// <summary>
/// A model the user can choose between.
/// </summary>
/// <remarks>
/// Purely descriptive. The framework never calls a provider itself, so this type exists to drive the
/// model picker, the context-usage meter, and cost estimates, not to configure a client.
/// </remarks>
public sealed record BitAiModel
{
    /// <summary>Creates a model description.</summary>
    /// <param name="modelId">The identifier sent to the provider.</param>
    /// <param name="name">The display name.</param>
    public BitAiModel(string modelId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentNullException.ThrowIfNull(name);
        ModelId = modelId;
        Name = name;
    }

    /// <summary>The identifier sent to the provider.</summary>
    public string ModelId { get; }

    /// <summary>The display name.</summary>
    public string Name { get; init; }

    /// <summary>Identifier of the provider that serves this model.</summary>
    public string? ProviderId { get; init; }

    /// <summary>A short description shown under the name in the picker.</summary>
    public string? Description { get; init; }

    /// <summary>A family or tier label used to group models in the picker.</summary>
    public string? Family { get; init; }

    /// <summary>What the model can do.</summary>
    public BitAiModelCapabilities Capabilities { get; init; } = BitAiModelCapabilities.Streaming;

    /// <summary>Context window in tokens, used by the context-usage meter.</summary>
    public long? ContextWindow { get; init; }

    /// <summary>Maximum tokens the model will produce in one response.</summary>
    public long? MaxOutputTokens { get; init; }

    /// <summary>Price per million input tokens, in <see cref="Currency"/>.</summary>
    public decimal? InputPricePerMillion { get; init; }

    /// <summary>Price per million output tokens, in <see cref="Currency"/>.</summary>
    public decimal? OutputPricePerMillion { get; init; }

    /// <summary>ISO 4217 code for the price fields.</summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// The name of the icon shown beside the model in <see cref="BitAiModelSelector"/>.
    /// </summary>
    /// <remarks>
    /// The name is drawn the way the <c>IconName</c> of a core component is (the selector hands it to the item of its
    /// <see cref="BitDropdown{TItem, TValue}"/>): a Fluent UI (Fabric MDL2) icon name (e.g., <c>BitIconName.Robot</c>),
    /// never an image URL. The core package ships only the glyphs its own components draw, so a name outside them needs
    /// the <c>Bit.BlazorUI.Icons</c> nuget package to be drawn. Browse the names in <c>BitIconName</c> of that package or
    /// the gallery: <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// </remarks>
    public string? Icon { get; init; }

    /// <summary>Whether the model should be hidden from pickers by default.</summary>
    public bool IsDeprecated { get; init; }

    /// <summary>Application-defined values carried through to custom model renderers.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <summary>Tests whether the model declares a capability.</summary>
    /// <param name="capability">The capability to test.</param>
    /// <returns><see langword="true"/> when the model declares it.</returns>
    public bool Supports(BitAiModelCapabilities capability) => (Capabilities & capability) == capability;

    /// <summary>
    /// Estimates the cost of a generation from its token usage.
    /// </summary>
    /// <param name="usage">The usage to price.</param>
    /// <returns>The cost, or <see langword="null"/> when this model carries no prices.</returns>
    public decimal? EstimateCost(BitAiTokenUsage usage)
    {
        if (InputPricePerMillion is null && OutputPricePerMillion is null) return null;
        var input = (usage.InputTokens ?? 0) / 1_000_000m * (InputPricePerMillion ?? 0m);
        var output = (usage.OutputTokens ?? 0) / 1_000_000m * (OutputPricePerMillion ?? 0m);
        return input + output;
    }
}

/// <summary>
/// A vendor or gateway that serves models.
/// </summary>
public sealed record BitAiProvider
{
    /// <summary>Creates a provider description.</summary>
    /// <param name="providerId">The identifier referenced by <see cref="BitAiModel.ProviderId"/>.</param>
    /// <param name="name">The display name.</param>
    public BitAiProvider(string providerId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(name);
        ProviderId = providerId;
        Name = name;
    }

    /// <summary>The identifier referenced by <see cref="BitAiModel.ProviderId"/>.</summary>
    public string ProviderId { get; }

    /// <summary>The display name.</summary>
    public string Name { get; init; }

    /// <summary>
    /// The name of an icon for the provider: a Fluent UI (Fabric MDL2) icon name, like <see cref="BitAiModel.Icon"/>.
    /// </summary>
    /// <remarks>
    /// Carried for a picker of the app's own: <see cref="BitAiModelSelector"/> draws no provider icon, and heads a group
    /// of models with the provider's <see cref="Name"/> alone (when the models name no family).
    /// </remarks>
    public string? Icon { get; init; }

    /// <summary>A link to the provider documentation or console.</summary>
    public string? Website { get; init; }
}

/// <summary>
/// The models and providers a host offers.
/// </summary>
public interface IBitAiModelCatalog
{
    /// <summary>Every known model, including deprecated ones.</summary>
    IReadOnlyList<BitAiModel> Models { get; }

    /// <summary>Every known provider.</summary>
    IReadOnlyList<BitAiProvider> Providers { get; }

    /// <summary>The model to select when the user has not chosen one.</summary>
    BitAiModel? DefaultModel { get; }

    /// <summary>
    /// Finds a model by identifier.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The model, or <see langword="null"/> when it is not in the catalog.</returns>
    BitAiModel? FindModel(string modelId);

    /// <summary>
    /// Finds a provider by identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The provider, or <see langword="null"/> when it is not in the catalog.</returns>
    BitAiProvider? FindProvider(string providerId);
}
