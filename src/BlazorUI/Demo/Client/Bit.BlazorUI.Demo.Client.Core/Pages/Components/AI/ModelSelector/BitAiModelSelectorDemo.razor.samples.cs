namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ModelSelector;

public partial class BitAiModelSelectorDemo
{
    private readonly string example1RazorCode = @"
<BitAiModelSelector @bind-ModelId=""basicModelId"" />

<div>ModelId: <b>@basicModelId</b></div>";
    private readonly string example1CsharpCode = @"
// The models come from the catalog registered in Program.cs:
// builder.Services.AddBitBlazorUIAIServices(ai => ai.AddModels(models, providers, defaultModelId: ""demo-model-1""));
private string? basicModelId = ""demo-model-1"";";

    private readonly string example2RazorCode = @"
<BitAiModelSelector @bind-ModelId=""recordModelId"" OnModelSelected=""HandleModelSelected"" />

@if (selectedModel is not null)
{
    <div><b>@selectedModel.Name</b> (@selectedModel.ModelId): @selectedModel.Description</div>
    <div>Context window: @selectedModel.ContextWindow tokens, up to @selectedModel.MaxOutputTokens out</div>
    <div>Per million tokens: @selectedModel.InputPricePerMillion in, @selectedModel.OutputPricePerMillion out (@selectedModel.Currency)</div>
    <div>Capabilities: @selectedModel.Capabilities</div>
}";
    private readonly string example2CsharpCode = @"
private string? recordModelId;
private BitAiModel? selectedModel;

private void HandleModelSelected(BitAiModel model)
{
    selectedModel = model;
}";

    private readonly string example3RazorCode = @"
<BitAiModelSelector @bind-ModelId=""planModelId"" Models=""planModels"" />

<div>ModelId: <b>@planModelId</b></div>";
    private readonly string example3CsharpCode = @"
private string? planModelId = ""team-pro"";
private readonly IReadOnlyList<BitAiModel> planModels =
[
    new(""team-mini"", ""Team Mini"")
    {
        Family = ""Team plan"",
        Icon = BitIconName.Robot,
        Description = ""Quick answers for everyday questions."",
    },
    new(""team-pro"", ""Team Pro"")
    {
        Family = ""Team plan"",
        Icon = BitIconName.Robot,
        Description = ""Longer reasoning, tools and images."",
        Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision,
    },
    new(""lab-preview"", ""Lab Preview"")
    {
        Icon = BitIconName.TestBeaker,
        Description = ""An experiment, with no group of its own."",
    },
];";

    private readonly string example4RazorCode = @"
<BitAiModelSelector @ref=""catalogSelector"" @bind-ModelId=""catalogModelId"" Catalog=""teamCatalog"" />

<div>ModelId: <b>@catalogModelId</b>, Selected: <b>@(catalogSelector?.Selected?.Name)</b></div>";
    private readonly string example4CsharpCode = @"
private string? catalogModelId;
private BitAiModelSelector catalogSelector = default!;
private static readonly BitAiModelCatalog teamCatalog = new(
    [
        new BitAiModel(""team-chat"", ""Team Chat"") { ProviderId = ""gateway"", Description = ""Served by the company gateway."" },
        new BitAiModel(""team-vision"", ""Team Vision"")
        {
            ProviderId = ""gateway"",
            Description = ""Reads screenshots and photos."",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Vision,
        },
        new BitAiModel(""on-device"", ""On-device"") { ProviderId = ""device"", Description = ""Runs locally. Nothing leaves the machine."" },
    ],
    [
        new BitAiProvider(""gateway"", ""Company gateway""),
        new BitAiProvider(""device"", ""This device""),
    ],
    defaultModelId: ""team-vision"");";

    private readonly string example5RazorCode = @"
<BitAiModelSelector @bind-ModelId=""visionModelId"" RequiredCapabilities=""BitAiModelCapabilities.Vision"" />

<BitAiModelSelector @bind-ModelId=""toolsModelId""
                    RequiredCapabilities=""@(BitAiModelCapabilities.Tools | BitAiModelCapabilities.StructuredOutput)"" />

<div>Vision: <b>@visionModelId</b>, tools and structured output: <b>@toolsModelId</b></div>";
    private readonly string example5CsharpCode = @"
private string? visionModelId;
private string? toolsModelId;";

    private readonly string example6RazorCode = @"
<BitAiModelSelector @bind-ModelId=""currentModelId"" Models=""versionedModels"" />

<BitAiModelSelector @bind-ModelId=""legacyModelId"" Models=""versionedModels"" ShowDeprecated />

<div>Without: <b>@currentModelId</b>, with deprecated models: <b>@legacyModelId</b></div>";
    private readonly string example6CsharpCode = @"
private string? currentModelId;
private string? legacyModelId = ""assistant-v2"";
private readonly IReadOnlyList<BitAiModel> versionedModels =
[
    new(""assistant-v3"", ""Assistant v3"") { Family = ""Assistant"", Description = ""The current version."" },
    new(""assistant-v2"", ""Assistant v2"") { Family = ""Assistant"", Description = ""Retired next month."", IsDeprecated = true },
];";

    private readonly string example7RazorCode = @"
<BitParams Parameters=""selectorParams"">
    <BitAiModelSelector @bind-ModelId=""cascadeModelId"" />

    <BitAiModelSelector @bind-ModelId=""ownModelId"" ShowDeprecated=""false"" />
</BitParams>

<div>First: <b>@cascadeModelId</b>, second (its own ShowDeprecated): <b>@ownModelId</b></div>";
    private readonly string example7CsharpCode = @"
private string? cascadeModelId = ""assistant-v2"";
private string? ownModelId;
private BitAiModelSelectorParams[] selectorParams => [new() { Models = versionedModels, ShowDeprecated = true }];

private readonly IReadOnlyList<BitAiModel> versionedModels =
[
    new(""assistant-v3"", ""Assistant v3"") { Family = ""Assistant"", Description = ""The current version."" },
    new(""assistant-v2"", ""Assistant v2"") { Family = ""Assistant"", Description = ""Retired next month."", IsDeprecated = true },
];";

    private readonly string example8RazorCode = @"
<BitAiModelSelector Disabled ModelId=""demo-model-1-fast"" />";

    private readonly string example9RazorCode = @"
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-container {
        border-width: 2px;
        border-color: var(--bit-clr-pri);
    }

    .custom-header {
        font-weight: bold;
        text-transform: uppercase;
        color: var(--bit-clr-pri);
    }
</style>

<BitAiModelSelector Style=""padding:0.25rem;border:1px dashed var(--bit-clr-brd-pri)"" />

<BitAiModelSelector Class=""custom-class"" />

<BitAiModelSelector Styles=""@(new() { Dropdown = new() { Container = ""border-radius:1rem"", ItemButton = ""font-style:italic"" } })"" />

<BitAiModelSelector Classes=""@(new() { Dropdown = new() { Container = ""custom-container"", ItemHeader = ""custom-header"" } })"" />

<div style=""@cssVariablesStyle"">
    <BitAiModelSelector />
</div>";
    private readonly string example9CsharpCode = @"
private const string cssVariablesStyle = ""--bit-Dropdown-radius:1rem;"" +
                                         ""--bit-Dropdown-border-color:var(--bit-clr-pri);"" +
                                         ""--bit-Dropdown-item-selected-background:var(--bit-clr-pri-hover);"" +
                                         ""--bit-Dropdown-item-selected-color:var(--bit-clr-pri-text);"";";

    private readonly string example10RazorCode = @"
<BitAiModelSelector Dir=""BitDir.Rtl"" Models=""persianModels"" @bind-ModelId=""rtlModelId"" />

<div dir=""rtl"">
    <BitAiModelSelector Models=""persianModels"" @bind-ModelId=""rtlModelId"" />
</div>";
    private readonly string example10CsharpCode = @"
private string? rtlModelId = ""fa-large"";
private readonly IReadOnlyList<BitAiModel> persianModels =
[
    new(""fa-large"", ""مدل بزرگ"") { Family = ""ارائه‌دهنده نمونه"", Description = ""مدل عمومی، کندتر و دقیق‌تر."" },
    new(""fa-fast"", ""مدل سریع"") { Family = ""ارائه‌دهنده نمونه"", Description = ""ارزان‌تر و سریع‌تر."" },
    new(""fa-local"", ""مدل محلی"") { Family = ""اجرای محلی"", Description = ""روی همین دستگاه اجرا می‌شود."" },
];";
}
