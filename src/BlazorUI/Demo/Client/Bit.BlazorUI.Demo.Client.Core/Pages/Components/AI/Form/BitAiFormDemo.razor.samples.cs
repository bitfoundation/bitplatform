namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Form;

public partial class BitAiFormDemo
{
    private const string ContactSampleCode = """
private readonly BitAiFormDefinition contactForm = new()
{
    Title = "Contact us",
    Description = "We answer within a working day.",
    Fields =
    [
        new("name") { Required = true, Placeholder = "Ada Lovelace" },
        new("email") { Type = BitAiFieldType.Email, Required = true },
        new("message") { Type = BitAiFieldType.MultilineText, Description = "What can we help with?" },
    ],
};
""";

    private const string EventSampleCode = """"
private readonly BitAiFormDefinition eventForm = BitAiFormDefinition.FromJsonSchema(JsonDocument.Parse("""
    {
      "title": "Plan an event",
      "type": "object",
      "properties": {
        "event": { "type": "string", "title": "Event" },
        "venue": {
          "type": "object",
          "title": "Venue",
          "description": "Where it takes place.",
          "required": ["city"],
          "properties": { "city": { "type": "string" }, "room": { "type": "string" } }
        },
        "tags": { "type": "array", "title": "Tags", "items": { "type": "string" }, "maxItems": 4, "default": ["blazor"] },
        "speakers": {
          "type": "array",
          "title": "Speakers",
          "minItems": 1,
          "items": {
            "type": "object",
            "required": ["name"],
            "properties": { "name": { "type": "string" }, "minutes": { "type": "integer", "minimum": 5 } }
          }
        }
      }
    }
    """).RootElement);
"""";

    private readonly string example1RazorCode = """
<BitAiForm Definition="contactForm" OnSubmit="values => contactValues = values" />

@if (contactValues is not null)
{
    <ul>
        @foreach (var (key, value) in contactValues)
        {
            <li><b>@key</b>: @value</li>
        }
    </ul>
}
""";
    private readonly string example1CsharpCode = ContactSampleCode + """

private IReadOnlyDictionary<string, string?>? contactValues;
""";

    private readonly string example2RazorCode = """
<BitAiForm Definition="typesForm" OnSubmit="values => typesValues = values" />

@if (typesValues is not null)
{
    <ul>
        @foreach (var (key, value) in typesValues)
        {
            <li><b>@key</b>: @value</li>
        }
    </ul>
}
""";
    private readonly string example2CsharpCode = """
private readonly BitAiFormDefinition typesForm = new()
{
    Fields =
    [
        new("title", "Text") { Placeholder = "A single line" },
        new("summary", "MultilineText") { Type = BitAiFieldType.MultilineText },
        new("quantity", "Number") { Type = BitAiFieldType.Number, Integer = true, Minimum = 1, Value = "2" },
        new("express", "Boolean") { Type = BitAiFieldType.Boolean, Description = "Ship it tomorrow." },
        new("size", "Select") { Type = BitAiFieldType.Select, Options = ["s", "m", "l"], OptionLabels = ["Small", "Medium", "Large"] },
        new("date", "Date") { Type = BitAiFieldType.Date },
        new("meeting", "DateTime") { Type = BitAiFieldType.DateTime },
        new("time", "Time") { Type = BitAiFieldType.Time },
        new("email", "Email") { Type = BitAiFieldType.Email, Placeholder = "name@example.com" },
        new("website", "Url") { Type = BitAiFieldType.Url, Placeholder = "https://" },
        new("password", "Password") { Type = BitAiFieldType.Password },
    ],
};

private IReadOnlyDictionary<string, string?>? typesValues;
""";

    private readonly string example3RazorCode = """
<BitAiForm Definition="bookingForm" OnSubmitJson="json => bookingJson = json" />

<BitAiJsonView Value="bookingJson" />
""";
    private readonly string example3CsharpCode = """"
private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiFormDefinition bookingForm = BitAiFormDefinition.FromJsonSchema(Parse("""
    {
      "title": "Book a table",
      "description": "Check the details I inferred before I book.",
      "type": "object",
      "required": ["restaurant", "guests", "date"],
      "properties": {
        "restaurant": { "type": "string", "title": "Restaurant" },
        "guests": { "type": "integer", "title": "Guests", "minimum": 1, "maximum": 12, "default": 2 },
        "date": { "type": "string", "format": "date", "title": "Date" },
        "time": { "type": "string", "format": "time", "title": "Time" },
        "seating": { "type": "string", "enum": ["inside", "terrace"], "enumNames": ["Inside", "On the terrace"] },
        "highChair": { "type": "boolean", "title": "A high chair" },
        "notes": { "type": "string", "format": "textarea", "title": "Notes for the restaurant" }
      }
    }
    """), Parse("""{ "restaurant": "Lucca", "date": "2026-10-23" }"""));

private JsonElement? bookingJson;
"""";

    private readonly string example4RazorCode = """
<BitAiForm Definition="seatForm" OnSubmitJson="json => seatJson = json" />

<BitAiJsonView Value="seatJson" />
""";
    private readonly string example4CsharpCode = """"
private readonly BitAiFormDefinition seatForm = BitAiFormDefinition.FromJsonSchema(JsonDocument.Parse("""
    {
      "title": "Reserve a seat",
      "type": "object",
      "$defs": {
        "contact": {
          "type": "object",
          "title": "Contact",
          "required": ["name"],
          "properties": { "name": { "type": "string" }, "email": { "type": "string", "format": "email" } }
        }
      },
      "required": ["event", "ticket", "phone"],
      "properties": {
        "event": { "const": "BlazorConf 2026", "title": "Event" },
        "ticket": {
          "title": "Ticket",
          "oneOf": [
            { "const": "standard", "title": "Standard - $99" },
            { "const": "vip", "title": "VIP - $249" }
          ]
        },
        "seat": { "type": "string", "title": "Seat", "readOnly": true, "default": "B12" },
        "arrival": { "type": "string", "format": "date-time", "title": "Arrival" },
        "pin": { "type": "string", "format": "password", "title": "Door PIN" },
        "phone": { "type": ["string", "null"], "title": "Phone", "examples": ["+1 555 0100"] },
        "contact": { "$ref": "#/$defs/contact", "description": "Who we write to about the seat." }
      }
    }
    """).RootElement);

private JsonElement? seatJson;
"""";

    private readonly string example5RazorCode = """
<BitAiForm Definition="signUpForm" OnSubmit="values => signUpValues = values" />

@if (signUpValues is not null)
{
    <div>Valid: <b>@string.Join(", ", signUpValues.Select(v => $"{v.Key} = {v.Value}"))</b></div>
}
""";
    private readonly string example5CsharpCode = """"
private readonly BitAiFormDefinition signUpForm = BitAiFormDefinition.FromJsonSchema(JsonDocument.Parse("""
    {
      "title": "Create an account",
      "type": "object",
      "required": ["username", "age"],
      "properties": {
        "username": { "type": "string", "minLength": 3, "maxLength": 16, "pattern": "^[a-z0-9_]+$", "description": "3 to 16 lowercase letters, digits or underscores." },
        "age": { "type": "integer", "minimum": 18, "maximum": 120 },
        "email": { "type": "string", "format": "email" },
        "website": { "type": "string", "format": "uri" },
        "donation": { "type": "number", "exclusiveMinimum": 0, "exclusiveMaximum": 1000, "multipleOf": 5, "description": "Optional, in steps of 5, under 1000." }
      }
    }
    """).RootElement) with { SubmitLabel = "Sign up" };

private IReadOnlyDictionary<string, string?>? signUpValues;
"""";

    private readonly string example6RazorCode = """
<BitAiForm Definition="eventForm" OnSubmitJson="json => eventJson = json" />

<BitAiJsonView Value="eventJson" />
""";
    private readonly string example6CsharpCode = EventSampleCode + """

private JsonElement? eventJson;
""";

    private readonly string example7RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="ProposeBookingAsync" Disabled="proposalSession.IsGenerating">Book a table for Friday</BitButton>

@if (ProposedForm is { } proposed)
{
    <BitAiForm Definition="proposed" Busy="proposalSession.IsGenerating" Submitted="proposalSubmitted" OnSubmit="values => proposalSubmitted = true" />
}
""";
    private readonly string example7CsharpCode = """
// A scripted transport that streams the JSON Schema of a booking form (see the next tab).
private readonly BitAiChatSession proposalSession = new(AiDemoInteractiveTransports.ProposesForm());
private BitAiRenderScheduler? renderScheduler;
private bool proposalSubmitted;

protected override void OnInitialized()
{
    // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    proposalSession.StateChanged += renderScheduler.Request;
}

// Rebuilt from the schema received so far on every render; the form keeps whatever the user already typed.
private BitAiFormDefinition? ProposedForm =>
    proposalSession.Conversation.LastMessage?.PartsOf<BitAiStructuredDataPart>().FirstOrDefault()?.Value is { } schema
        ? BitAiFormDefinition.FromJsonSchema(schema)
        : null;

private async Task ProposeBookingAsync()
{
    proposalSubmitted = false;

    await proposalSession.SubmitAsync("Book a table for Friday");
}

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await proposalSession.DisposeAsync();
}
""";
    private const string example7TransportCode = """"
public static IBitAiTransport ProposesForm() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        const string intro = "I can book that. Check the details first, since I inferred two of them.\n\n";
        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        // The two details the model inferred go in as the defaults the form starts from: two guests, and the coming Friday.
        var today = DateTime.Today;
        var friday = today.AddDays(((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7);
        var date = friday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var schema = $$"""
            {
              "title": "Book a table",
              "type": "object",
              "required": ["restaurant", "guests", "date"],
              "properties": {
                "restaurant": { "type": "string", "title": "Restaurant" },
                "guests": { "type": "integer", "title": "Guests", "minimum": 1, "maximum": 12, "default": 2 },
                "date": { "type": "string", "format": "date", "title": "Date", "default": "{{date}}" },
                "seating": { "type": "string", "enum": ["Inside", "Terrace", "No preference"] },
                "notes": { "type": "string", "format": "textarea", "title": "Notes for the restaurant" }
              }
            }
            """;

        // Thirty characters at a time: the schema is incomplete until the last slice.
        foreach (var slice in AiScript.Slice(schema, 30))
        {
            await AiScript.Pause(50, token);
            await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "booking_form" });
        }

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example7CodeFiles =
    [
        new("AiDemoInteractiveTransports.cs", example7TransportCode),
    ];

    private readonly string example8RazorCode = """
<BitAiForm @ref="feedbackFormRef"
           Definition="feedbackForm"
           Busy="feedbackBusy"
           Submitted="feedbackSubmitted"
           OnSubmit="SendFeedbackAsync"
           OnCancel="CancelFeedback" />

<BitButton Variant="BitVariant.Outline" OnClick="ReadValues">Read the values</BitButton>
<BitButton Variant="BitVariant.Outline" OnClick="StartOver">Start over</BitButton>

<div>@feedbackStatus</div>

<BitAiForm Definition="contactForm" Disabled />
""";
    private readonly string example8CsharpCode = ContactSampleCode + """

private readonly BitAiFormDefinition feedbackForm = new()
{
    Title = "Rate the answer",
    SubmitLabel = "Send feedback",
    CancelLabel = "Not now",
    Fields =
    [
        new("rating") { Type = BitAiFieldType.Select, Required = true, Options = ["5", "4", "3", "2", "1"], OptionLabels = ["Excellent", "Good", "Fair", "Poor", "Wrong"] },
        new("comment") { Type = BitAiFieldType.MultilineText, Placeholder = "What would have made it better?" },
    ],
};

private BitAiForm feedbackFormRef = default!;
private bool feedbackBusy;
private bool feedbackSubmitted;
private string feedbackStatus = string.Empty;

private async Task SendFeedbackAsync(IReadOnlyDictionary<string, string?> values)
{
    feedbackBusy = true;
    feedbackStatus = "Sending...";

    await Task.Delay(1500); // stands in for the call that stores the feedback

    feedbackBusy = false;
    feedbackSubmitted = true;
    feedbackStatus = $"Sent: a rating of {values["rating"]}.";
}

private void CancelFeedback() => feedbackStatus = "Cancelled: nothing was sent.";

private void ReadValues() =>
    feedbackStatus = "So far: " + string.Join(", ", feedbackFormRef.Values.Select(v => $"{v.Key} = {v.Value ?? "(empty)"}"));

private void StartOver()
{
    feedbackSubmitted = false;
    feedbackStatus = string.Empty;
}
""";

    private readonly string example9RazorCode = """
<BitParams Parameters="formParams">
    <BitAiForm Definition="contactForm" />

    <BitAiForm Definition="feedbackForm" Styles="@(new() { Title = "font-style: italic" })" />
</BitParams>
""";
    private readonly string example9CsharpCode = ContactSampleCode + """

private readonly BitAiFormDefinition feedbackForm = new()
{
    Title = "Rate the answer",
    SubmitLabel = "Send feedback",
    CancelLabel = "Not now",
    Fields =
    [
        new("rating") { Type = BitAiFieldType.Select, Required = true, Options = ["5", "4", "3", "2", "1"], OptionLabels = ["Excellent", "Good", "Fair", "Poor", "Wrong"] },
        new("comment") { Type = BitAiFieldType.MultilineText, Placeholder = "What would have made it better?" },
    ],
};

private readonly BitAiFormParams[] formParams =
[
    new() { Styles = new() { Root = "border-radius: 1rem", Title = "color: var(--bit-clr-pri)", SubmitButton = new() { Root = "border-radius: 1rem" } } },
];
""";

    private readonly string example10RazorCode = """
<style>
    .custom-form {
        border-width: 0;
        border-inline-start-width: 0.25rem;
        border-color: #8a2be2;
        border-radius: 0;
    }

    .custom-group {
        border-style: dashed;
        border-color: #8a2be2;
    }

    .custom-legend {
        color: #8a2be2;
        font-weight: bold;
    }
</style>

<BitAiForm Definition="contactForm"
           Style="max-width: 28rem"
           Styles="@(new() { Root = "background-color: #1e1e2e1a", Title = "color: #8a2be2", SubmitButton = new() { Root = "border-radius: 1rem" } })" />

<BitAiForm Definition="eventForm" Classes="@(new() { Root = "custom-form", Group = "custom-group", Legend = "custom-legend" })" />

<div style="--bit-AiForm-background: transparent; --bit-AiForm-border-color: #d2691e; --bit-AiForm-radius: 0; --bit-AiForm-actions-justify: flex-start; --bit-AiForm-title-font-size: 1.25rem">
    <BitAiForm Definition="contactForm" />
</div>
""";
    private readonly string example10CsharpCode = ContactSampleCode + "\n" + EventSampleCode;

    private readonly string example11RazorCode = """
<BitAiForm Dir="BitDir.Rtl" Definition="persianForm" />

<div dir="rtl">
    <BitAiForm Definition="persianForm" />
</div>
""";
    private readonly string example11CsharpCode = """
private readonly BitAiFormDefinition persianForm = new()
{
    Title = "تماس با ما",
    Description = "در کمتر از یک روز کاری پاسخ می‌دهیم.",
    SubmitLabel = "ارسال",
    CancelLabel = "انصراف",
    Fields =
    [
        new("name", "نام") { Required = true },
        new("subscribe", "عضویت در خبرنامه") { Type = BitAiFieldType.Boolean },
        new("message", "پیام") { Type = BitAiFieldType.MultilineText },
    ],
};
""";
}
