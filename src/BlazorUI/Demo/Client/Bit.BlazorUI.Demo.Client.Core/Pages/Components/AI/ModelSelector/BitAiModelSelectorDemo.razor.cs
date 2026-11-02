namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ModelSelector;

public partial class BitAiModelSelectorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Catalog",
            Type = "IBitAiModelCatalog?",
            DefaultValue = "null",
            Description = "The catalog of the models to offer. Its providers name the groups of the models that have no Family, and its DefaultModel is selected while ModelId names none. When null, the IBitAiModelCatalog registered in the services (ai.AddModels of AddBitBlazorUIAIServices) is used.",
            LinkType = LinkType.Link,
            Href = "#model-catalog",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiModelSelectorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the model selector.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ModelId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the selected model. When it names no model on offer - because it is unset, deprecated or filtered out - the picker selects the default model of the catalog, or else the first one, and raises ModelIdChanged and OnModelSelected for it after the render, so a bound value never disagrees with what the picker shows. Use @bind-ModelId to keep it in step.",
        },
        new()
        {
            Name = "ModelIdChanged",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised when the selection changes - picked by the user, or made by the picker itself for a ModelId naming no model on offer - so the value can be bound.",
        },
        new()
        {
            Name = "Models",
            Type = "IReadOnlyList<BitAiModel>?",
            DefaultValue = "null",
            Description = "The models to offer, in their order, in place of those of the catalog - for an app that filters them by entitlement. The catalog still supplies the provider names and the default model.",
            LinkType = LinkType.Link,
            Href = "#model",
        },
        new()
        {
            Name = "OnModelSelected",
            Type = "EventCallback<BitAiModel>",
            DefaultValue = "",
            Description = "Raised with the full model record when the selection changes, whoever made it.",
            LinkType = LinkType.Link,
            Href = "#model",
        },
        new()
        {
            Name = "RequiredCapabilities",
            Type = "BitAiModelCapabilities",
            DefaultValue = "BitAiModelCapabilities.None",
            Description = "Offers only the models that declare every one of these capabilities, for example Vision for a composer that has images staged. None offers every model.",
            LinkType = LinkType.Link,
            Href = "#model-capabilities-enum",
        },
        new()
        {
            Name = "ShowDeprecated",
            Type = "bool",
            DefaultValue = "false",
            Description = "Offers the models marked IsDeprecated too, each labelled as such (the ModelDeprecated text of the strings in force, \"Name (deprecated)\").",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiModelSelectorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the model selector.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Selected",
            Type = "BitAiModel?",
            DefaultValue = "null",
            Description = "The model the picker shows, which may differ from ModelId until the change has been raised.",
            LinkType = LinkType.Link,
            Href = "#model",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiModelSelectorClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiModelSelector."
                },
                new()
                {
                    Name = "Dropdown",
                    Type = "BitDropdownClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitDropdown of the BitAiModelSelector that lists the models.",
                    LinkType = LinkType.Link,
                    Href = "/components/dropdown#class-styles"
                },
            ]
        },
        new()
        {
            Id = "model",
            Title = "BitAiModel",
            Description = "A model the user can choose between: new BitAiModel(modelId, name) { ... }. Purely descriptive - the package never calls a provider itself - it drives the model picker, the context meter (ContextWindow) and the cost estimates of the token usage (the prices), not a client.",
            Parameters =
            [
                new()
                {
                    Name = "ModelId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier sent to the provider, and what ModelId of the selector names. Required; it cannot be empty."
                },
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The display name, the text of its option in the picker."
                },
                new()
                {
                    Name = "ProviderId",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The identifier of the BitAiProvider that serves the model. The picker groups a model without a Family under the name of its provider (or this id, when the catalog does not know it)."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A short description, the tooltip of its option in the picker and of the picker while it is selected."
                },
                new()
                {
                    Name = "Family",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A family or tier label the picker groups the models under, ahead of the provider."
                },
                new()
                {
                    Name = "Capabilities",
                    Type = "BitAiModelCapabilities",
                    DefaultValue = "BitAiModelCapabilities.Streaming",
                    Description = "What the model can do. RequiredCapabilities of the selector filters on it, and a BitAiPromptInput given the model offers attachments and dictation by it.",
                    LinkType = LinkType.Link,
                    Href = "#model-capabilities-enum"
                },
                new()
                {
                    Name = "ContextWindow",
                    Type = "long?",
                    DefaultValue = "null",
                    Description = "The context window in tokens, used by the context meter."
                },
                new()
                {
                    Name = "MaxOutputTokens",
                    Type = "long?",
                    DefaultValue = "null",
                    Description = "The most tokens the model produces in one response."
                },
                new()
                {
                    Name = "InputPricePerMillion",
                    Type = "decimal?",
                    DefaultValue = "null",
                    Description = "The price of a million input tokens, in Currency."
                },
                new()
                {
                    Name = "OutputPricePerMillion",
                    Type = "decimal?",
                    DefaultValue = "null",
                    Description = "The price of a million output tokens, in Currency."
                },
                new()
                {
                    Name = "Currency",
                    Type = "string",
                    DefaultValue = "\"USD\"",
                    Description = "The ISO 4217 code of the price fields."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The icon shown beside the model in the picker: a Fluent UI (Fabric MDL2) icon name such as BitIconName.Robot, never an image URL. The core package ships only the glyphs its own components draw, so a name outside them needs the Bit.BlazorUI.Icons nuget package to be drawn.",
                    LinkType = LinkType.Link,
                    Href = "/iconography"
                },
                new()
                {
                    Name = "IsDeprecated",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Hides the model from the picker unless its ShowDeprecated is set, which labels it as deprecated."
                },
                new()
                {
                    Name = "Metadata",
                    Type = "IReadOnlyDictionary<string, object?>?",
                    DefaultValue = "null",
                    Description = "App-defined values carried through to custom model renderers."
                },
                new()
                {
                    Name = "Supports(BitAiModelCapabilities capability)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the model declares the capability - every one of them, for a combination.",
                },
                new()
                {
                    Name = "EstimateCost(BitAiTokenUsage usage)",
                    Type = "decimal?",
                    DefaultValue = "",
                    Description = "The cost of a generation from its token usage at the prices of the model, in Currency, or null for a model that carries no prices.",
                },
            ]
        },
        new()
        {
            Id = "provider",
            Title = "BitAiProvider",
            Description = "A vendor or gateway that serves models: new BitAiProvider(providerId, name) { ... }, handed to the catalog beside the models.",
            Parameters =
            [
                new()
                {
                    Name = "ProviderId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier the ProviderId of a BitAiModel references. Required; it cannot be empty."
                },
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The display name, which heads the group of its models (those without a Family) in the picker."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An icon name (a Fabric MDL2 name, never a URL) for the provider. No component of the package draws it today."
                },
                new()
                {
                    Name = "Website",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A link to the documentation or the console of the provider."
                },
            ]
        },
        new()
        {
            Id = "model-catalog",
            Title = "IBitAiModelCatalog",
            Description = "The models and providers a host offers. ai.AddModels(models, providers, defaultModelId) registers a BitAiModelCatalog - an in-memory one, new BitAiModelCatalog(models, providers, defaultModelId), whose default is the first model that is not deprecated unless one is named, and whose GroupByProvider() hands its models grouped by provider, in catalog order, to a picker of your own; a host that fetches its model list from a gateway implements this interface over that instead.",
            Parameters =
            [
                new()
                {
                    Name = "Models",
                    Type = "IReadOnlyList<BitAiModel>",
                    DefaultValue = "",
                    Description = "Every known model, deprecated ones included, in the order a picker shows them.",
                    LinkType = LinkType.Link,
                    Href = "#model"
                },
                new()
                {
                    Name = "Providers",
                    Type = "IReadOnlyList<BitAiProvider>",
                    DefaultValue = "",
                    Description = "Every known provider.",
                    LinkType = LinkType.Link,
                    Href = "#provider"
                },
                new()
                {
                    Name = "DefaultModel",
                    Type = "BitAiModel?",
                    DefaultValue = "",
                    Description = "The model to select when the user has not chosen one.",
                    LinkType = LinkType.Link,
                    Href = "#model"
                },
                new()
                {
                    Name = "FindModel(string modelId)",
                    Type = "BitAiModel?",
                    DefaultValue = "",
                    Description = "The model of that identifier, or null when it is not in the catalog (BitAiModelCatalog compares ids case-insensitively).",
                    LinkType = LinkType.Link,
                    Href = "#model"
                },
                new()
                {
                    Name = "FindProvider(string providerId)",
                    Type = "BitAiProvider?",
                    DefaultValue = "",
                    Description = "The provider of that identifier, or null when it is not in the catalog.",
                    LinkType = LinkType.Link,
                    Href = "#provider"
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "model-capabilities-enum",
            Name = "BitAiModelCapabilities",
            Description = "What a model can do, used to enable or hide what the UI offers, such as the attach button. A set of flags: combine them with |.",
            Items =
            [
                new() { Name = "None", Description = "No declared capabilities.", Value = "0" },
                new() { Name = "Streaming", Description = "Produces output incrementally.", Value = "1" },
                new() { Name = "Vision", Description = "Accepts images as input.", Value = "2" },
                new() { Name = "Audio", Description = "Accepts audio as input.", Value = "4" },
                new() { Name = "Tools", Description = "Can call tools.", Value = "8" },
                new() { Name = "StructuredOutput", Description = "Can be constrained to a JSON schema.", Value = "16" },
                new() { Name = "Reasoning", Description = "Emits visible reasoning before the answer.", Value = "32" },
                new() { Name = "Documents", Description = "Accepts documents such as PDFs as input.", Value = "64" },
                new() { Name = "Citations", Description = "Returns citations for retrieved sources.", Value = "128" },
            ]
        },
    ];



    private const string cssVariablesStyle = "--bit-Dropdown-radius:1rem;" +
                                             "--bit-Dropdown-border-color:var(--bit-clr-pri);" +
                                             "--bit-Dropdown-item-selected-background:var(--bit-clr-pri-hover);" +
                                             "--bit-Dropdown-item-selected-color:var(--bit-clr-pri-text);";

    private string? basicModelId = "demo-model-1";

    private string? recordModelId;
    private BitAiModel? selectedModel;

    private string? planModelId = "team-pro";
    private readonly IReadOnlyList<BitAiModel> planModels =
    [
        new("team-mini", "Team Mini")
        {
            Family = "Team plan",
            Icon = BitIconName.Robot,
            Description = "Quick answers for everyday questions.",
        },
        new("team-pro", "Team Pro")
        {
            Family = "Team plan",
            Icon = BitIconName.Robot,
            Description = "Longer reasoning, tools and images.",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision,
        },
        new("lab-preview", "Lab Preview")
        {
            Icon = BitIconName.TestBeaker,
            Description = "An experiment, with no group of its own.",
        },
    ];

    private string? catalogModelId;
    private BitAiModelSelector catalogSelector = default!;
    private static readonly BitAiModelCatalog teamCatalog = new(
        [
            new BitAiModel("team-chat", "Team Chat") { ProviderId = "gateway", Description = "Served by the company gateway." },
            new BitAiModel("team-vision", "Team Vision")
            {
                ProviderId = "gateway",
                Description = "Reads screenshots and photos.",
                Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Vision,
            },
            new BitAiModel("on-device", "On-device") { ProviderId = "device", Description = "Runs locally. Nothing leaves the machine." },
        ],
        [
            new BitAiProvider("gateway", "Company gateway"),
            new BitAiProvider("device", "This device"),
        ],
        defaultModelId: "team-vision");

    private string? visionModelId;
    private string? toolsModelId;

    private string? currentModelId;
    private string? legacyModelId = "assistant-v2";
    private readonly IReadOnlyList<BitAiModel> versionedModels =
    [
        new("assistant-v3", "Assistant v3") { Family = "Assistant", Description = "The current version." },
        new("assistant-v2", "Assistant v2") { Family = "Assistant", Description = "Retired next month.", IsDeprecated = true },
    ];

    private string? cascadeModelId = "assistant-v2";
    private string? ownModelId;
    private BitAiModelSelectorParams[] selectorParams => [new() { Models = versionedModels, ShowDeprecated = true }];

    private string? rtlModelId = "fa-large";
    private readonly IReadOnlyList<BitAiModel> persianModels =
    [
        new("fa-large", "مدل بزرگ") { Family = "ارائه‌دهنده نمونه", Description = "مدل عمومی، کندتر و دقیق‌تر." },
        new("fa-fast", "مدل سریع") { Family = "ارائه‌دهنده نمونه", Description = "ارزان‌تر و سریع‌تر." },
        new("fa-local", "مدل محلی") { Family = "اجرای محلی", Description = "روی همین دستگاه اجرا می‌شود." },
    ];

    private void HandleModelSelected(BitAiModel model)
    {
        selectedModel = model;
    }
}
