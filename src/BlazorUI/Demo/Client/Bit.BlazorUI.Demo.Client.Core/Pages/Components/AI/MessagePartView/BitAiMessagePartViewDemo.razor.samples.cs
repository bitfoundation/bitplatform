namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessagePartView;

public partial class BitAiMessagePartViewDemo
{
    private readonly string example1RazorCode = """
@foreach (var part in basicMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="basicMessage" />
}
""";
    private readonly string example1CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private static BitAiToolCallPart WeatherCall()
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Oslo"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow"}""")) with
    {
        Duration = TimeSpan.FromMilliseconds(700),
    });

    return call;
}

private readonly BitAiMessage basicMessage = CreateBasicMessage();

private static BitAiMessage CreateBasicMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart("Let me check the weather in Oslo."));
    message.AddPart(WeatherCall());
    message.AddPart(new BitAiTextPart("It is **-3 °C** and snowing in Oslo - bring a warm coat."));

    return message;
}
"""";

    private readonly string example2RazorCode = """
<BitButton OnClick="StreamAsync" Disabled="isStreaming">Stream an answer</BitButton>

@foreach (var part in streamingMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="streamingMessage" />
}
""";
    private readonly string example2CsharpCode = """"
private const string packingList = """
    Pack for **-3 °C** and snow:

    - a warm coat and a hat
    - gloves and a scarf
    - boots with a good grip

    The days are short in winter, so plan to be indoors by four.
    """;
private BitAiMessage streamingMessage = CreateStreamingMessage();
private bool isStreaming;

// WeatherCall() as in the Basic example.
private static BitAiMessage CreateStreamingMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(WeatherCall());

    return message;
}

private async Task StreamAsync()
{
    isStreaming = true;
    streamingMessage = CreateStreamingMessage();

    // An empty text part expects more text, so it streams until Complete is called.
    var answer = streamingMessage.AddPart(new BitAiTextPart());
    StateHasChanged();

    foreach (var chunk in packingList.Chunk(4))
    {
        await Task.Delay(25);
        answer.Append(new string(chunk));
    }

    answer.Complete();
    isStreaming = false;
}
"""";

    private readonly string example3RazorCode = """
@foreach (var part in kindsMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="kindsMessage" />
}
""";
    private readonly string example3CsharpCode = """
private readonly BitAiMessage kindsMessage = CreateKindsMessage();

private static BitAiMessage CreateKindsMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiReasoningPart("The user wants the setup in one line. AddBitBlazorUIAIServices with a transport is all it takes."));
    message.AddPart(new BitAiCodePart("builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());", "csharp") { FileName = "Program.cs" });
    message.AddPart(new BitAiToolResultPart("call_logs", BitAiToolOutcome.FromText("No errors in the last hour.")) { ToolName = "read_logs" });
    message.AddPart(new BitAiErrorPart(new BitAiError("The second search timed out, so this answer is based on the first one only.")
    {
        Kind = BitAiErrorKind.Timeout,
    }));

    // No view knows this kind and no renderer is registered for it, so its Text is shown.
    message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["Tuesday", "Thursday"]) { Text = "Poll: which day suits you, Tuesday or Thursday?" });

    return message;
}
""";

    private readonly string example4RazorCode = """
@foreach (var part in citedMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="citedMessage" OnCitationClick="c => clickedCitation = c.Source.Title" />
}

<div>Activated: <b>@(clickedCitation ?? "none yet")</b></div>
""";
    private readonly string example4CsharpCode = """
private readonly BitAiMessage citedMessage = CreateCitedMessage();
private string? clickedCitation;

private static BitAiMessage CreateCitedMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    // Numbered 1 and 2 in the order they are added, which is what [1] and [2] in the text point at.
    message.AddCitation(new BitAiCitation("cit_modes", new BitAiCitationSource("render-modes", "Render modes")
    {
        Url = "https://example.com/docs/render-modes",
        SiteName = "Product documentation",
    }));
    var prerendering = message.AddCitation(new BitAiCitation("cit_prerender", new BitAiCitationSource("prerendering", "Prerendering")
    {
        Url = "https://example.com/docs/prerendering",
        SiteName = "Product documentation",
    })
    {
        Quote = "Prerendering renders the page on the server first, so it is painted before its components become interactive.",
        Locator = "Section 3: Prerendering",
    });
    message.AddPart(new BitAiTextPart("Interactive Server keeps the components on the server [1], and prerendering paints them once before they become interactive [2]."));

    // The second citation placed where it sits, between the parts: the card of its source, numbered 2 as the message
    // numbers it, with the quote and the locator under it.
    message.AddPart(new BitAiCitationPart(prerendering));

    return message;
}
""";

    private readonly string example5RazorCode = """
@foreach (var part in invoiceMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="invoiceMessage" />
}
""";
    private readonly string example5CsharpCode = """"
[Inject] private BitAiRendererRegistry Renderers { get; set; } = default!;

protected override void OnInitialized()
{
    // An app registers its renderers once, at startup (the Program.cs tab). The registry is shared by the whole app, so
    // this page registers its own kind as it loads - the same call, under a kind nothing else uses.
    Renderers.RegisterPart<InvoiceCard>("demo:invoice");
}

private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private const string octoberInvoice = """
    {"number":"INV-1042","currency":"EUR","lines":[{"item":"Design review","amount":1200},{"item":"Hosting, 3 months","amount":90}]}
    """;

private readonly BitAiMessage invoiceMessage = CreateInvoiceMessage();

private static BitAiMessage CreateInvoiceMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart("Here is the invoice for October:"));

    // A part of a kind of its own, carrying the JSON the model produced. Its Text is what a copy of the message holds.
    message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(octoberInvoice)) { Text = "Invoice INV-1042: 1,290.00 EUR" });

    return message;
}
"""";
    private const string example5ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport<MyTransport>();

    // Every part of the kind "demo:invoice", wherever it appears, is drawn by InvoiceCard.
    ai.ConfigureRenderers(renderers => renderers.RegisterPart<InvoiceCard>("demo:invoice"));

    // Or with a delegate, handed the part, its message, the conversation and the session:
    // renderers.RegisterPart("demo:invoice", context => builder =>
    // {
    //     builder.OpenComponent<InvoiceCard>(0);
    //     builder.AddComponentParameter(1, nameof(InvoiceCard.Part), context.Part);
    //     builder.CloseComponent();
    // });
});
""";
    private const string example5CardCode = """
@using System.Globalization
@using System.Text.Json

<div class="invoice-card">
    <div class="invoice-card-head">
        <BitIcon IconName="@BitIconName.Money" Size="BitSize.Small" />
        <span>Invoice @number</span>
    </div>

    <ul class="invoice-card-lines">
        @foreach (var (item, amount) in lines)
        {
            <li>
                <span>@item</span>
                <span>@Format(amount)</span>
            </li>
        }
    </ul>

    <div class="invoice-card-total">
        <span>Total</span>
        <span>@Format(lines.Sum(line => line.Amount))</span>
    </div>
</div>

@code {
    // Handed over by the registry, which renders the component with the part as its Part parameter.
    [Parameter, EditorRequired] public BitAiCustomJsonPart Part { get; set; } = default!;

    private string? number;
    private string? currency;
    private (string? Item, decimal Amount)[] lines = [];

    // Written for the shape the prompt asks the model for. A payload without "lines" throws here - which is what the
    // BitAiRendererBoundary the part view renders every registered renderer in contains.
    protected override void OnParametersSet()
    {
        var data = Part.Data;

        number = data.GetProperty("number").GetString();
        currency = data.GetProperty("currency").GetString();
        lines = [.. data.GetProperty("lines").EnumerateArray().Select(line => (line.GetProperty("item").GetString(), line.GetProperty("amount").GetDecimal()))];
    }

    private string Format(decimal amount) => $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {currency}";
}
""";
    private const string example5CardStyleCode = """
.invoice-card {
    gap: 0.5rem;
    display: flex;
    max-width: 22rem;
    flex-direction: column;
    box-sizing: border-box;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    color: var(--bit-clr-fg-pri);
    background-color: var(--bit-clr-bg-sec);
    border: 1px solid var(--bit-clr-brd-sec);
}

.invoice-card-head {
    gap: 0.5rem;
    display: flex;
    font-weight: 600;
    align-items: center;
}

.invoice-card-lines {
    margin: 0;
    padding: 0;
    list-style: none;
}

.invoice-card-lines > li,
.invoice-card-total {
    gap: 1rem;
    display: flex;
    justify-content: space-between;
    font-variant-numeric: tabular-nums;
}

.invoice-card-total {
    font-weight: 600;
    padding-block-start: 0.5rem;
    border-block-start: 1px solid var(--bit-clr-brd-sec);
}
""";
    private readonly DemoCodeFile[] example5CodeFiles =
    [
        new("Program.cs", example5ProgramCode),
        new("InvoiceCard.razor", example5CardCode),
        new("InvoiceCard.razor.scss", example5CardStyleCode),
    ];

    private readonly string example6RazorCode = """
@foreach (var part in brokenMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="brokenMessage" />
}
""";
    private readonly string example6CsharpCode = """"
// The model left the lines out of the second invoice, which the card was not written for.
private const string novemberInvoice = """{"number":"INV-1043","currency":"EUR","total":1290}""";

private readonly BitAiMessage brokenMessage = CreateBrokenMessage();

// Json(), octoberInvoice and the registration of InvoiceCard as in the previous example.
private static BitAiMessage CreateBrokenMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart("Here are the invoices for October and November:"));
    message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(octoberInvoice)) { Text = "Invoice INV-1042: 1,290.00 EUR" });
    message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(novemberInvoice)) { Text = "Invoice INV-1043: 1,290.00 EUR" });
    message.AddPart(new BitAiTextPart("Both are due at the end of the month."));

    return message;
}
"""";

    private readonly string example7RazorCode = """
<style>
    .custom-part {
        padding-inline-start: 1rem;
        border-inline-start: 4px solid #0b7a75;
    }

    .custom-part-root {
        padding: 0.5rem 1rem;
        border: 1px dashed #6d28d9;
        border-radius: 0.5rem;
    }

    .custom-part-fallback {
        color: #6d28d9;
        font-weight: bold;
    }
</style>

<BitAiMessagePartView Part="styleMessage.Parts[0]" Message="styleMessage" Style="padding: 0.75rem 1rem; border-radius: 0.5rem; background-color: #f5f3ff" />

<BitAiMessagePartView Part="styleMessage.Parts[0]" Message="styleMessage" Class="custom-part" />

<BitAiMessagePartView Part="styleMessage.Parts[1]" Message="styleMessage" Styles="@(new() { Fallback = "font-style: italic; color: #6d28d9" })" />

<BitAiMessagePartView Part="styleMessage.Parts[1]" Message="styleMessage" Classes="@(new() { Root = "custom-part-root", Fallback = "custom-part-fallback" })" />

<BitAiMessagePartView Part="styleMessage.Parts[2]" Message="styleMessage" Styles="@(new() { CitationQuote = "font-style: normal; color: #6d28d9", CitationLocator = "font-weight: bold" })" />

<div style="--bit-AiMessagePartView-citation-gap: 0.25rem; --bit-AiMessagePartView-quote-color: #0b7a75; --bit-AiMessagePartView-quote-border-color: #0b7a75; --bit-AiMessagePartView-locator-color: #0b7a75">
    <BitAiMessagePartView Part="styleMessage.Parts[2]" Message="styleMessage" />
</div>
""";
    private readonly string example7CsharpCode = """
private readonly BitAiMessage styleMessage = CreateStyleMessage();

private static BitAiMessage CreateStyleMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    var forecast = message.AddCitation(new BitAiCitation("cit_forecast", new BitAiCitationSource("forecast", "Oslo forecast")
    {
        Url = "https://example.com/weather/oslo",
        SiteName = "Weather service",
    })
    {
        Quote = "Snow showers through the afternoon, -3 °C.",
        Locator = "Today, 14:00",
    });
    message.AddPart(new BitAiTextPart("It is **-3 °C** and snowing in Oslo."));
    message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["Tuesday", "Thursday"]) { Text = "Poll: which day suits you, Tuesday or Thursday?" });
    message.AddPart(new BitAiCitationPart(forecast));

    return message;
}
""";

    private readonly string example8RazorCode = """
@foreach (var part in rtlMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Dir="BitDir.Rtl" Part="part" Message="rtlMessage" />
}
""";
    private readonly string example8CsharpCode = """
private readonly BitAiMessage rtlMessage = CreateRtlMessage();

private static BitAiMessage CreateRtlMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart("هوای تهران امروز **۱۸ درجه** و آفتابی است."));
    message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["سه‌شنبه", "پنج‌شنبه"]) { Text = "نظرسنجی: کدام روز برایتان مناسب است، سه‌شنبه یا پنج‌شنبه؟" });

    return message;
}
""";
}
