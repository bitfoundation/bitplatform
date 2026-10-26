using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Form;

public partial class BitAiFormDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Busy",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the actions off while something else is in flight - the submission being saved, a reply being generated - while the inputs stay editable. A press that still reaches a button submits or cancels nothing.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiFormClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the form.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Definition",
            Type = "BitAiFormDefinition",
            DefaultValue = "null",
            Description = "The form to render (required): its title, description, fields and button labels. Each field is seeded once with its Value; a definition handed in again - rebuilt as a schema streams in - keeps what the user typed in every field whose control did not change (a bound, a choice or a nested field streaming in changes nothing typed, and a prefill streaming in fills a scalar field only while it still holds the previous prefill, so what the user typed wins; an object or a list is seeded again with a new prefill), seeds the new ones, and drops the values of fields that are gone.",
            LinkType = LinkType.Link,
            Href = "#form-definition",
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback raised when the user presses Cancel. The button is shown only while it is set, and unless the CancelLabel of the definition is an empty string.",
        },
        new()
        {
            Name = "OnSubmit",
            Type = "EventCallback<IReadOnlyDictionary<string, string?>>",
            DefaultValue = "",
            Description = "The callback raised with the collected values when the user submits a valid form: a copy of the flat values, as invariant text keyed by field path (name, venue.city, tags[0], speakers[1].name). A form with errors shows them on its fields and raises nothing. See OnSubmitJson for the typed, nested shape.",
        },
        new()
        {
            Name = "OnSubmitJson",
            Type = "EventCallback<JsonElement>",
            DefaultValue = "",
            Description = "The callback raised with the values as JSON in the shape of the schema the form came from (BitAiFormDefinition.ToJson): nested objects and arrays rebuilt from the paths, numbers and booleans encoded as such. Raised after OnSubmit.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiFormClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the form.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Submitted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the form has already been submitted, which replaces the actions with a confirmation and stops any further submission - set it once the submission has been handled, so the turn reads as done.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Values",
            Type = "IReadOnlyDictionary<string, string?>",
            DefaultValue = "",
            Description = "The values as they currently stand, keyed by field path, valid or not - what the user has typed so far, read without submitting (through a @ref).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiFormClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiForm."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAiForm."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description under the title of the BitAiForm."
                },
                new()
                {
                    Name = "Field",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of each single field (one input) of the BitAiForm."
                },
                new()
                {
                    Name = "Group",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each group of fields (the fieldset of a nested object or a list) of the BitAiForm."
                },
                new()
                {
                    Name = "Legend",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the legend of each group of fields of the BitAiForm."
                },
                new()
                {
                    Name = "Hint",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the help text of each group of fields of the BitAiForm."
                },
                new()
                {
                    Name = "Item",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each entry of a list of the BitAiForm."
                },
                new()
                {
                    Name = "ItemBody",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the fields of each entry of a list of the BitAiForm."
                },
                new()
                {
                    Name = "RemoveButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button removing an entry of a list of the BitAiForm.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "AddButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button adding an entry to a list of the BitAiForm.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "Error",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the error messages the BitAiForm renders itself: the one of a list, and the one of a checkbox. The other inputs show their errors through their own ErrorMessage."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions of the BitAiForm."
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the cancel button of the BitAiForm.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "SubmitButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the submit button of the BitAiForm.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "Submitted",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the confirmation shown in place of the actions once the BitAiForm is submitted."
                },
            ]
        },
        new()
        {
            Id = "form-definition",
            Title = "BitAiFormDefinition",
            Description = "A form the model asked for, which the user fills in and submits back: a record of its fields and labels, built in code or from a JSON Schema.",
            Parameters =
            [
                new()
                {
                    Name = "Fields",
                    Type = "IReadOnlyList<BitAiFormField>",
                    DefaultValue = "[]",
                    Description = "The fields, in display order.",
                    LinkType = LinkType.Link,
                    Href = "#form-field"
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A heading above the form."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A line of explanation under the heading."
                },
                new()
                {
                    Name = "SubmitLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the submit button; the Submit text of the AI options' string table while it is null, so a form a model proposes is labelled in the language of the app."
                },
                new()
                {
                    Name = "CancelLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the cancel button; the Cancel text of the string table while it is null, and no cancel button at all when it is an empty string. The button is shown only on a form with an OnCancel."
                },
                new()
                {
                    Name = "FromJsonSchema(schema, values?)",
                    Type = "BitAiFormDefinition (static)",
                    DefaultValue = "",
                    Description = "Builds a form from a JSON Schema object (its title and description become the form's): type - including [\"string\", \"null\"], which makes a field optional -, format (date, date-time, time, email, uri, password, textarea), enum with enumNames, oneOf / anyOf of const values, const and readOnly (read-only fields), default, required, numeric and length bounds, multipleOf, pattern, examples (a placeholder), nested object properties, array items with minItems / maxItems, and local $refs into $defs or definitions. The optional values, keyed by property name, prefill it ahead of the defaults. Never throws on a malformed schema: a keyword of the wrong kind is ignored and an unknown shape falls back to a text box."
                },
                new()
                {
                    Name = "ToJson(values)",
                    Type = "JsonElement",
                    DefaultValue = "",
                    Description = "Builds JSON in the shape of the form from the flat values a BitAiForm collects - nested objects and arrays rebuilt from the paths, numbers and booleans encoded as such - with one member per field that has a value. What OnSubmitJson is handed."
                },
                new()
                {
                    Name = "Path(parent, name) / IndexPath(arrayPath, index) / CountItems(values, arrayPath)",
                    Type = "string / string / int (static)",
                    DefaultValue = "",
                    Description = "The paths the flat values are keyed by - venue.city, tags[0] - and the number of entries a list holds in them, for an app reading the values of OnSubmit or Values."
                },
            ]
        },
        new()
        {
            Id = "form-field",
            Title = "BitAiFormField",
            Description = "One field of a form: a record created as new BitAiFormField(name, label?) with its other members set by initializer.",
            Parameters =
            [
                new() { Name = "Name", Type = "string", DefaultValue = "", Description = "The key the value is submitted under." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "The visible label. Falls back to the name written as a label (DisplayLabel): firstName reads First Name." },
                new() { Name = "Type", Type = "BitAiFieldType", DefaultValue = "BitAiFieldType.Text", Description = "The control the field renders as.", LinkType = LinkType.Link, Href = "#field-type-enum" },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Help text under the control (under the legend of a group)." },
                new() { Name = "Placeholder", Type = "string?", DefaultValue = "null", Description = "Placeholder text inside the control." },
                new() { Name = "Value", Type = "string?", DefaultValue = "null", Description = "The initial value, which a model may prefill, as invariant text. For an Array field, the JSON text of the initial array. A Boolean field without one starts, and is submitted, as false - the unchecked box it shows." },
                new() { Name = "Required", Type = "bool", DefaultValue = "false", Description = "Whether the field must be filled in (an Array field: hold at least one entry). A checkbox is never required to be checked." },
                new() { Name = "ReadOnly", Type = "bool", DefaultValue = "false", Description = "Whether the field is shown but cannot be edited, as for a schema const or readOnly. A read-only list can be neither added to nor removed from." },
                new() { Name = "Options", Type = "IReadOnlyList<string>?", DefaultValue = "null", Description = "The choices of a Select field, as the values that are submitted. A value outside them fails validation." },
                new() { Name = "OptionLabels", Type = "IReadOnlyList<string>?", DefaultValue = "null", Description = "Display labels for Options, in the same order, when they differ from the values." },
                new() { Name = "Minimum / Maximum", Type = "double?", DefaultValue = "null", Description = "The smallest and the largest value a number accepts." },
                new() { Name = "ExclusiveMinimum / ExclusiveMaximum", Type = "double?", DefaultValue = "null", Description = "Values a number must be strictly greater, or less, than." },
                new() { Name = "MultipleOf", Type = "double?", DefaultValue = "null", Description = "A step a number must be a multiple of, which is also the step of its spin buttons." },
                new() { Name = "Integer", Type = "bool", DefaultValue = "false", Description = "Whether a number accepts whole numbers only." },
                new() { Name = "MinLength / MaxLength", Type = "int?", DefaultValue = "null", Description = "The fewest and the most characters a text value may have." },
                new() { Name = "MinItems / MaxItems", Type = "int?", DefaultValue = "null", Description = "The fewest and the most entries an Array field may hold; the add button turns off at the most." },
                new() { Name = "Pattern", Type = "string?", DefaultValue = "null", Description = "A regular expression the value must match, run under a timeout: one that takes too long fails the field." },
                new() { Name = "Fields", Type = "IReadOnlyList<BitAiFormField>?", DefaultValue = "null", Description = "The nested fields of an Object field." },
                new() { Name = "Items", Type = "BitAiFormField?", DefaultValue = "null", Description = "The template of each entry of an Array field: a scalar field for a list of inputs, an Object field for repeatable groups." },
                new() { Name = "JsonType", Type = "string?", DefaultValue = "null", Description = "The JSON Schema primitive the value is submitted as by ToJson - string, integer, number, boolean, object or array; null means string." },
                new() { Name = "DisplayLabel", Type = "string", DefaultValue = "", Description = "The label shown for the field: Label, else the name written as a label." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "field-type-enum",
            Name = "BitAiFieldType",
            Description = "The input control a form field renders as. FromJsonSchema picks it from the type and format of a property.",
            Items =
            [
                new() { Name = "Text", Value = "0", Description = "A single-line text box (BitTextField); a string with no format." },
                new() { Name = "MultilineText", Value = "1", Description = "A multi-line text box; format textarea, or an object or array too deep to follow." },
                new() { Name = "Number", Value = "2", Description = "A numeric input (BitNumberField); type integer or number." },
                new() { Name = "Boolean", Value = "3", Description = "A checkbox (BitCheckbox); type boolean." },
                new() { Name = "Select", Value = "4", Description = "A dropdown over Options (BitDropdown); an enum, or a oneOf / anyOf of const values." },
                new() { Name = "Date", Value = "5", Description = "A date picker (BitDatePicker); format date." },
                new() { Name = "DateTime", Value = "6", Description = "A date and time picker; format date-time." },
                new() { Name = "Email", Value = "7", Description = "An email input; format email or idn-email." },
                new() { Name = "Url", Value = "8", Description = "A URL input; format uri, url, uri-reference or iri." },
                new() { Name = "Password", Value = "9", Description = "A masked input; format password." },
                new() { Name = "Time", Value = "10", Description = "A time-of-day picker (BitTimePicker); format time." },
                new() { Name = "Object", Value = "11", Description = "A group of nested fields, from an object with properties." },
                new() { Name = "Array", Value = "12", Description = "A list the user adds to and removes from: scalar Items as a list of inputs, object Items as repeatable groups." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiForm-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the form." },
        new() { Name = "--bit-AiForm-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Border color of the form." },
        new() { Name = "--bit-AiForm-border-width", DefaultValue = "var(--bit-shp-brd-width)", Description = "Border width of the form." },
        new() { Name = "--bit-AiForm-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the form." },
        new() { Name = "--bit-AiForm-padding", DefaultValue = "var(--bit-spa-card-md)", Description = "Inset of the form." },
        new() { Name = "--bit-AiForm-gap", DefaultValue = "spacing(1.5)", Description = "Room between the title, the fields and the actions, and between the fields of a group." },
        new() { Name = "--bit-AiForm-title-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the title." },
        new() { Name = "--bit-AiForm-title-font-size", DefaultValue = "var(--bit-tpg-fs-md)", Description = "Size of the title." },
        new() { Name = "--bit-AiForm-title-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the title." },
        new() { Name = "--bit-AiForm-description-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the description and of the help text of a group of fields." },
        new() { Name = "--bit-AiForm-description-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the description and of the help text of a group." },
        new() { Name = "--bit-AiForm-group-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Border color of a group of fields (a nested object, a list)." },
        new() { Name = "--bit-AiForm-group-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corner radius of a group of fields." },
        new() { Name = "--bit-AiForm-group-padding", DefaultValue = "spacing(1.5)", Description = "Inset of a group of fields." },
        new() { Name = "--bit-AiForm-legend-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the legend of a group of fields." },
        new() { Name = "--bit-AiForm-legend-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the legend of a group of fields." },
        new() { Name = "--bit-AiForm-required-color", DefaultValue = "var(--bit-clr-req)", Description = "Color of the marker of a required list (the inputs mark themselves)." },
        new() { Name = "--bit-AiForm-error-color", DefaultValue = "var(--bit-clr-err)", Description = "Color of the border of an invalid list and of the error messages the form renders itself." },
        new() { Name = "--bit-AiForm-error-font-size", DefaultValue = "var(--bit-tpg-fs-2xs)", Description = "Size of the error messages the form renders itself." },
        new() { Name = "--bit-AiForm-actions-gap", DefaultValue = "spacing(1)", Description = "Room between the actions." },
        new() { Name = "--bit-AiForm-actions-justify", DefaultValue = "flex-end", Description = "How the actions, and the confirmation that replaces them, sit along their row." },
    ];



    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

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

    private IReadOnlyDictionary<string, string?>? contactValues;

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

    private readonly BitAiFormDefinition signUpForm = BitAiFormDefinition.FromJsonSchema(Parse("""
        {
          "title": "Create an account",
          "type": "object",
          "required": ["username", "age"],
          "properties": {
            "username": { "type": "string", "minLength": 3, "maxLength": 16, "pattern": "^[a-z0-9_]+$", "description": "3 to 16 lowercase letters, digits or underscores." },
            "age": { "type": "integer", "minimum": 18, "maximum": 120 },
            "email": { "type": "string", "format": "email" },
            "website": { "type": "string", "format": "uri" },
            "donation": { "type": "number", "exclusiveMinimum": 0, "multipleOf": 5, "description": "Optional, in steps of 5." }
          }
        }
        """)) with { SubmitLabel = "Sign up" };

    private IReadOnlyDictionary<string, string?>? signUpValues;

    private readonly BitAiFormDefinition eventForm = BitAiFormDefinition.FromJsonSchema(Parse("""
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
        """));

    private JsonElement? eventJson;

    // A scripted transport (Services/AI) that streams the JSON Schema of a booking form.
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
}
