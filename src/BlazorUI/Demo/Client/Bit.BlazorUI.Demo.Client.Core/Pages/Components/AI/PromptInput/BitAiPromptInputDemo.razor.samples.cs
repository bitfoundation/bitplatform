namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.PromptInput;

public partial class BitAiPromptInputDemo
{
    private readonly string example1RazorCode = @"
<BitAiPromptInput OnSubmit=""HandleBasicSubmit"" />

<BitAiPromptInput Placeholder=""Ask anything about your account..."" OnSubmit=""HandleBasicSubmit"" />

<div>Sent: <b>@basicSubmission</b></div>";
    private readonly string example1CsharpCode = @"
private string basicSubmission = ""nothing yet"";

private void HandleBasicSubmit(BitAiPromptSubmission submission)
{
    basicSubmission = submission.Text;
}";

    private readonly string example2RazorCode = @"
<BitAiPromptInput @ref=""valueComposer""
                  @bind-Value=""boundValue""
                  MaxLength=""120""
                  OnSubmit=""HandleValueSubmit"" />

<div>Value: <b>@boundValue</b></div>
<div>Sent: <b>@valueSubmission</b></div>

<BitButton OnClick=""FillSuggestion"">Fill in a suggestion</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""FocusComposerAsync"">Focus the composer</BitButton>";
    private readonly string example2CsharpCode = @"
private BitAiPromptInput? valueComposer;
private string boundValue = string.Empty;
private string valueSubmission = ""nothing yet"";

private void HandleValueSubmit(BitAiPromptSubmission submission)
{
    valueSubmission = submission.Text;
}

private void FillSuggestion()
{
    boundValue = ""Which of my invoices are still unpaid?"";
}

private async Task FocusComposerAsync()
{
    if (valueComposer is null) return;

    await valueComposer.FocusAsync();
}";

    private readonly string example3RazorCode = @"
@implements IAsyncDisposable

<div style=""height:24rem;display:flex;flex-direction:column;gap:0.5rem"">
    <BitAiMessageList Conversation=""session.Conversation"" Session=""session"" />
    <BitAiPromptInput Session=""session"" Placeholder=""Ask about Blazor render modes..."" />
</div>";
    private readonly string example3CsharpCode = @"
// Any IBitAiTransport will do; this one streams a long Markdown answer, slow enough to stop halfway.
// A session made by the BitAiChatSessionFactory service picks up the registered transport and tools instead.
private readonly BitAiChatSession session = new(AiDemoTransports.Markdown());

public async ValueTask DisposeAsync()
{
    await session.DisposeAsync();
}";

    private readonly string example4RazorCode = @"
<BitAiPromptInput Generating=""isGenerating""
                  OnSubmit=""StartGenerationAsync""
                  OnStop=""StopGeneration"" />

<div>@generationStatus</div>";
    private readonly string example4CsharpCode = @"
private bool isGenerating;
private CancellationTokenSource? generationCts;
private string generationStatus = ""Send something to start a turn of your own; it takes five seconds unless you stop it."";

private async Task StartGenerationAsync(BitAiPromptSubmission submission)
{
    generationCts?.Dispose();
    generationCts = new();
    isGenerating = true;
    generationStatus = $""Working on \""{submission.Text}\""..."";
    StateHasChanged();

    try
    {
        await Task.Delay(5000, generationCts.Token);
        generationStatus = $""Done with \""{submission.Text}\""."";
    }
    catch (OperationCanceledException)
    {
        generationStatus = $""Stopped \""{submission.Text}\""."";
    }
    finally
    {
        isGenerating = false;
    }
}

private void StopGeneration()
{
    generationCts?.Cancel();
}";

    private readonly string example5RazorCode = @"
<BitAiPromptInput OnSubmit=""HandleAttachmentsSubmit"" />

<BitAiPromptInput AllowAttachments=""false"" Placeholder=""No attachments here"" OnSubmit=""HandleAttachmentsSubmit"" />

<div>Sent: <b>@attachmentsSubmission</b></div>

<div style=""display:flex;flex-wrap:wrap;gap:0.5rem"">
    @foreach (var attachment in sentAttachments)
    {
        <BitAiAttachmentChip @key=""attachment.AttachmentId"" Attachment=""attachment"" />
    }
</div>";
    private readonly string example5CsharpCode = @"
private string attachmentsSubmission = ""nothing yet"";
private IReadOnlyList<BitAiAttachment> sentAttachments = [];

private void HandleAttachmentsSubmit(BitAiPromptSubmission submission)
{
    attachmentsSubmission = $""\""{submission.Text}\"" with {submission.Attachments.Count} file(s)"";
    sentAttachments = submission.Attachments;
}";

    private readonly string example6RazorCode = @"
<BitAiPromptInput Accept=""image/*,.pdf""
                  MaxAttachmentSize=""MaxSize""
                  MaxAttachments=""3""
                  OnAttachmentRejected=""HandleRejected""
                  OnSubmit=""HandleLimitsSubmit"" />

<div>Sent: <b>@limitsSubmission</b></div>

<div>Turned away:</div>
@if (rejections.Count == 0)
{
    <div>Nothing yet. Try a file over 1 MB, a fourth file, or a .txt file.</div>
}
else
{
    <ul>
        @foreach (var rejection in rejections)
        {
            <li><b>@rejection.FileName</b> (@rejection.Size bytes): @rejection.Reason</li>
        }
    </ul>
}";
    private readonly string example6CsharpCode = @"
private const long MaxSize = 1024 * 1024;

private string limitsSubmission = ""nothing yet"";
private readonly List<BitAiAttachmentRejection> rejections = [];

private void HandleLimitsSubmit(BitAiPromptSubmission submission)
{
    limitsSubmission = $""\""{submission.Text}\"" with {submission.Attachments.Count} file(s)"";
}

// The composer already tells the user; this is where an app would log or count it.
private void HandleRejected(BitAiAttachmentRejection rejection)
{
    rejections.Insert(0, rejection);
    if (rejections.Count > 5) rejections.RemoveAt(rejections.Count - 1);
}";

    private readonly string example7RazorCode = @"
<BitAiPromptInput AllowDictation DictationLanguage=""en-US"" OnSubmit=""HandleDictationSubmit"" />

<div>Sent: <b>@dictationSubmission</b></div>";
    private readonly string example7CsharpCode = @"
private string dictationSubmission = ""nothing yet"";

private void HandleDictationSubmit(BitAiPromptSubmission submission)
{
    dictationSubmission = submission.Text;
}";

    private readonly string example8RazorCode = @"
<BitAiModelSelector @bind-ModelId=""capabilityModelId"" />

<BitAiPromptInput Model=""FindModel(capabilityModelId)"" AllowDictation OnSubmit=""HandleCapabilitiesSubmit"" />

<BitAiPromptInput Capabilities=""@(BitAiModelCapabilities.Documents | BitAiModelCapabilities.Audio)""
                  AllowDictation
                  Placeholder=""Documents and audio, with dictation""
                  OnSubmit=""HandleCapabilitiesSubmit"" />

<div>Sent: <b>@capabilitiesSubmission</b></div>";
    private readonly string example8CsharpCode = @"
// The catalog AddBitBlazorUIAIServices registers with ai.AddModels(...).
[AutoInject] private IBitAiModelCatalog modelCatalog { get; set; } = default!;

private string? capabilityModelId = ""demo-model-1"";
private string capabilitiesSubmission = ""nothing yet"";

private BitAiModel? FindModel(string? modelId) => modelId is null ? null : modelCatalog.FindModel(modelId);

private void HandleCapabilitiesSubmit(BitAiPromptSubmission submission)
{
    capabilitiesSubmission = $""\""{submission.Text}\"" with {submission.Attachments.Count} file(s)"";
}";

    private readonly string example9RazorCode = @"
<div style=""min-height:20rem;display:flex;flex-direction:column;justify-content:flex-end;gap:1rem"">
    <ul>
        @foreach (var prompt in sentPrompts)
        {
            <li>@prompt</li>
        }
    </ul>
    <BitAiPromptInput Commands=""commands""
                      Mentions=""mentions""
                      Placeholder=""Type / for commands, @@ to mention a file or a person""
                      OnSubmit=""HandleCommandsSubmit"" />
</div>";
    private readonly string example9CsharpCode = @"
private IReadOnlyList<BitAiSlashCommand> commands = [];
private readonly List<string> sentPrompts = [];
private readonly IReadOnlyList<BitAiMentionItem> mentions =
[
    new(""file-roadmap"", ""roadmap.md"", ""Product roadmap, updated Monday"", BitIconName.Page),
    new(""file-pricing"", ""pricing.xlsx"", ""Pricing model, three tiers"", BitIconName.Page),
    new(""user-priya"", ""priya"", ""Priya Raman, product lead"", BitIconName.Contact),
    new(""user-tomas"", ""tomas"", ""Tomás Silva, engineering"", BitIconName.Contact),
];

protected override void OnInitialized()
{
    commands =
    [
        new(""summarize"", ""Summarize the conversation so far"", BitIconName.BulletedList)
        {
            Template = ""Summarize the conversation so far in three bullet points."",
        },
        new(""explain"", ""Explain something for a newcomer"", BitIconName.Lightbulb)
        {
            Template = ""Explain this as if I were new to the codebase: "",
        },
        new(""translate"", ""Translate the next text"", BitIconName.Translate)
        {
            Template = ""Translate into French: "",
        },
        new(""clear"", ""Clear the list of sent prompts"", BitIconName.Delete)
        {
            Handler = () =>
            {
                sentPrompts.Clear();
                StateHasChanged();
                return Task.CompletedTask;
            },
        },
    ];

    base.OnInitialized();
}

private void HandleCommandsSubmit(BitAiPromptSubmission submission)
{
    sentPrompts.Add(submission.Text);
}";

    private readonly string example10RazorCode = @"
<BitAiPromptInput DraftKey=""prompt-input-demo-draft"" OnSubmit=""HandleDraftSubmit"" />

<div>Sent: <b>@draftSubmission</b></div>";
    private readonly string example10CsharpCode = @"
private string draftSubmission = ""nothing yet"";

private void HandleDraftSubmit(BitAiPromptSubmission submission)
{
    draftSubmission = submission.Text;
}";

    private readonly string example11RazorCode = @"
<BitAiPromptInput @bind-Value=""templatesValue""
                  Model=""FindModel(templatesModelId)""
                  OnSubmit=""HandleTemplatesSubmit"">
    <PrefixTemplate>
        <BitAiModelSelector @bind-ModelId=""templatesModelId"" />
    </PrefixTemplate>
    <SuffixTemplate>
        <BitButton IconName=""@BitIconName.Lightbulb""
                   Variant=""BitVariant.Text""
                   Title=""Suggest a prompt""
                   AriaLabel=""Suggest a prompt""
                   OnClick=""SuggestPrompt"" />
    </SuffixTemplate>
    <HintTemplate>
        Enter to send, Shift+Enter for a new line. Answers can be wrong - check what matters.
    </HintTemplate>
</BitAiPromptInput>

<div>Sent: <b>@templatesSubmission</b></div>";
    private readonly string example11CsharpCode = @"
[AutoInject] private IBitAiModelCatalog modelCatalog { get; set; } = default!;

private string templatesValue = string.Empty;
private string? templatesModelId = ""demo-model-1"";
private string templatesSubmission = ""nothing yet"";

private BitAiModel? FindModel(string? modelId) => modelId is null ? null : modelCatalog.FindModel(modelId);

private void SuggestPrompt()
{
    templatesValue = ""Draft a friendly reminder for an overdue invoice."";
}

private void HandleTemplatesSubmit(BitAiPromptSubmission submission)
{
    templatesSubmission = submission.Text;
}";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""promptInputParams"">
    <BitAiPromptInput OnSubmit=""HandleCascadeSubmit"" />

    <BitAiPromptInput Placeholder=""Its own placeholder wins"" OnSubmit=""HandleCascadeSubmit"" />
</BitParams>

<div>Sent: <b>@cascadeSubmission</b></div>";
    private readonly string example12CsharpCode = @"
private string cascadeSubmission = ""nothing yet"";
private BitAiPromptInputParams[] promptInputParams =>
[
    new()
    {
        Placeholder = ""Ask the team assistant..."",
        Accept = ""image/*"",
        MaxAttachments = 2,
        MaxLength = 200,
    }
];

private void HandleCascadeSubmit(BitAiPromptSubmission submission)
{
    cascadeSubmission = submission.Text;
}";

    private readonly string example13RazorCode = @"
<BitAiPromptInput Disabled Value=""A disabled composer keeps its text"" />";

    private readonly string example14RazorCode = @"
<style>
    .custom-class {
        border-width: 2px;
        border-color: var(--bit-clr-pri);
    }

    .custom-root {
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-input {
        font-style: italic;
    }

    .custom-footer {
        font-weight: bold;
        color: var(--bit-clr-pri);
    }
</style>

<BitAiPromptInput Style=""max-width:30rem;border-style:dashed""
                  Placeholder=""A styled composer""
                  OnSubmit=""HandleStyleSubmit"" />

<BitAiPromptInput Class=""custom-class""
                  Placeholder=""A classy composer""
                  OnSubmit=""HandleStyleSubmit"" />

<BitAiPromptInput Styles=""@(new() { Input = ""font-family:monospace"",
                                    SendButton = new() { Root = ""border-radius:50%"" } })""
                  Placeholder=""Styles reach every part""
                  OnSubmit=""HandleStyleSubmit"" />

<BitAiPromptInput Classes=""@(new() { Root = ""custom-root"", Input = ""custom-input"", Footer = ""custom-footer"" })""
                  MaxLength=""80""
                  Placeholder=""Classes reach every part""
                  OnSubmit=""HandleStyleSubmit"" />

<div style=""@cssVariablesStyle"">
    <BitAiPromptInput Placeholder=""Restyled by the variables of its container"" OnSubmit=""HandleStyleSubmit"" />
</div>

<div>Sent: <b>@styleSubmission</b></div>";
    private readonly string example14CsharpCode = @"
private const string cssVariablesStyle = ""--bit-AiPromptInput-radius:1.5rem;"" +
                                         ""--bit-AiPromptInput-padding:0.5rem 0.75rem;"" +
                                         ""--bit-AiPromptInput-border-color:transparent;"" +
                                         ""--bit-AiPromptInput-background:var(--bit-clr-bg-sec);"" +
                                         ""--bit-AiPromptInput-shadow:0 0.25rem 1rem rgb(0 0 0 / 0.12);"";

private string styleSubmission = ""nothing yet"";

private void HandleStyleSubmit(BitAiPromptSubmission submission)
{
    styleSubmission = submission.Text;
}";

    private readonly string example15RazorCode = @"
<BitAiPromptInput Dir=""BitDir.Rtl""
                  MaxLength=""100""
                  Placeholder=""پیام خود را بنویسید...""
                  OnSubmit=""HandleRtlSubmit"">
    <HintTemplate>برای ارسال Enter و برای خط جدید Shift+Enter را بزنید.</HintTemplate>
</BitAiPromptInput>

<div dir=""rtl"">
    <BitAiPromptInput Placeholder=""پیامی برای دستیار بنویسید..."" OnSubmit=""HandleRtlSubmit"" />
</div>

<div>Sent: <b>@rtlSubmission</b></div>";
    private readonly string example15CsharpCode = @"
private string rtlSubmission = ""nothing yet"";

private void HandleRtlSubmit(BitAiPromptSubmission submission)
{
    rtlSubmission = submission.Text;
}";
}
