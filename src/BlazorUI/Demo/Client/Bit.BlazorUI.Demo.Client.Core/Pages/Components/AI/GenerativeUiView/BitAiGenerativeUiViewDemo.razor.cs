using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.GenerativeUiView;

public partial class BitAiGenerativeUiViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiGenerativeUiViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the generative UI view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiGenerativeUiPart",
            DefaultValue = "null",
            Description = "The generative UI part to render (required): the name of the component the model chose and the JSON of its properties. The name is resolved through the IBitAiGenerativeUiRegistry, never as a type name, and the properties are bound onto the parameters that registration lists, again whenever the part changes - so a card streaming in fills in as far as its properties have arrived. A name nothing is registered under renders a warning tag once the part has finished streaming.",
            LinkType = LinkType.Link,
            Href = "#generative-ui-part",
        },
        new()
        {
            Name = "ShowBindingErrors",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lists, under the component, the properties that failed to bind (a value of the wrong shape, a required one missing) and the ones ignored because the component does not declare them, once the part has finished streaming. Useful while developing the prompt that produces them, noise in production; a BitAiGenerativeUiViewParams of a BitParams turns it on for a whole region.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiGenerativeUiViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the generative UI view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiGenerativeUiViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiGenerativeUiView."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the rendered component of the BitAiGenerativeUiView."
                },
                new()
                {
                    Name = "Unregistered",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag a component name nothing is registered under is shown as by the BitAiGenerativeUiView.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag/#tag-class-styles"
                },
                new()
                {
                    Name = "Diagnostics",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the binding diagnostics of the BitAiGenerativeUiView."
                },
                new()
                {
                    Name = "Error",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag of each property that failed to bind in the BitAiGenerativeUiView.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag/#tag-class-styles"
                },
                new()
                {
                    Name = "Ignored",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag of each property the component of the BitAiGenerativeUiView does not declare.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag/#tag-class-styles"
                },
            ]
        },
        new()
        {
            Id = "generative-ui-part",
            Title = "BitAiGenerativeUiPart",
            Description = "A part of a message naming a component the model chose, with the properties it produced as JSON. A session creates one for each BitAiGenerativeUiEvent of its transport and fills it from its BitAiGenerativeUiDeltaEvents; an app creates one to show a component outside a conversation. Safe to read on the renderer's thread while it is appended to on the transport's.",
            Parameters =
            [
                new()
                {
                    Name = "new(componentName, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a streaming part for the component registered under that name."
                },
                new()
                {
                    Name = "ComponentName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The registered component name, as the model wrote it (matched case-insensitively)."
                },
                new()
                {
                    Name = "Props",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "The properties once complete; until then a lenient parse of the JSON received so far, null while nothing parseable has arrived."
                },
                new()
                {
                    Name = "RawProps",
                    Type = "string",
                    DefaultValue = "\"\"",
                    Description = "The raw JSON property text received so far."
                },
                new()
                {
                    Name = "IsStreaming",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether more properties may still arrive; the view says nothing of an unregistered name or of the binding until it is false."
                },
                new()
                {
                    Name = "AppendProps(delta)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Appends raw JSON property text."
                },
                new()
                {
                    Name = "SetProps(props)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Sets the complete properties at once."
                },
                new()
                {
                    Name = "Complete()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Marks the properties complete, parsing the accumulated text strictly when SetProps did not set them."
                },
            ]
        },
        new()
        {
            Id = "generative-ui-registry",
            Title = "BitAiGenerativeUiRegistry",
            Description = "The components model output may render, registered as a singleton by AddBitBlazorUIAIServices (as IBitAiGenerativeUiRegistry too) and filled at startup through ConfigureGenerativeUi. Safe to read from any thread. The Register overloads read the parameter types by reflection, down to every type they reach, so they are marked [RequiresUnreferencedCode] and [RequiresDynamicCode]: an app that trims keeps those types itself and suppresses the warnings on the method that registers, with its reason.",
            Parameters =
            [
                new()
                {
                    Name = "Register<TComponent>(name, description?, params required)",
                    Type = "BitAiGenerativeComponent",
                    DefaultValue = "",
                    Description = "Registers a component under a name the model uses (keep it descriptive: the model reads it), replacing one registered under the same name. Its bindable properties are its public [Parameter] properties with a public setter, less callbacks, render fragments, CaptureUnmatchedValues, anything marked [BitAiGenerativeIgnore] and any type whose graph reaches a delegate, a Type or a component. [Description] on a parameter describes it to the model, and on the component is the description when none is given; a name in required, or a [Required] parameter, must be supplied.",
                    LinkType = LinkType.Link,
                    Href = "#generative-component"
                },
                new()
                {
                    Name = "Register(componentType, name, description?, params required)",
                    Type = "BitAiGenerativeComponent",
                    DefaultValue = "",
                    Description = "The same with the component type given as a Type; throws an ArgumentException for a type that is not a Blazor component.",
                    LinkType = LinkType.Link,
                    Href = "#generative-component"
                },
                new()
                {
                    Name = "Unregister(name)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Removes a registration; true when one was removed."
                },
                new()
                {
                    Name = "Find(name)",
                    Type = "BitAiGenerativeComponent?",
                    DefaultValue = "",
                    Description = "The registration of a name, matched case-insensitively, or null - the case of a model naming something it should not have, which renders a placeholder.",
                    LinkType = LinkType.Link,
                    Href = "#generative-component"
                },
                new()
                {
                    Name = "Components",
                    Type = "IReadOnlyCollection<BitAiGenerativeComponent>",
                    DefaultValue = "[]",
                    Description = "Every registration.",
                    LinkType = LinkType.Link,
                    Href = "#generative-component"
                },
                new()
                {
                    Name = "DescribeAll()",
                    Type = "JsonElement",
                    DefaultValue = "",
                    Description = "A JSON Schema of every registration - a oneOf of { component, props } objects - for a system prompt or as the schema of a render tool. Handing it to the model is what makes generative UI reliable: without it the model guesses names."
                },
                new()
                {
                    Name = "IsBindable(type)",
                    Type = "bool (static)",
                    DefaultValue = "",
                    Description = "Whether a type is plain data a model may supply: false for delegates, render fragments, event callbacks, Type and any graph containing one."
                },
            ]
        },
        new()
        {
            Id = "generative-component",
            Title = "BitAiGenerativeComponent",
            Description = "One registration: the component a name renders and the properties model output may set on it. It is the security boundary - a model can only name what is registered and set what is listed - and the prompting surface, through its Schema.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The name the model uses."
                },
                new()
                {
                    Name = "ComponentType",
                    Type = "Type",
                    DefaultValue = "",
                    Description = "The Blazor component type it renders."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What the component is for, included in the schema."
                },
                new()
                {
                    Name = "Properties",
                    Type = "IReadOnlyList<BitAiGenerativeProperty>",
                    DefaultValue = "[]",
                    Description = "The properties the model may set: each one's Name (as the model must spell it, matched case-insensitively), Type, Description and IsRequired."
                },
                new()
                {
                    Name = "Schema",
                    Type = "JsonElement",
                    DefaultValue = "",
                    Description = "The JSON Schema of its properties: nested objects with their own properties, collections with items, enums as their names, dates, identifiers and addresses as formatted strings, and additionalProperties false."
                },
                new()
                {
                    Name = "FindProperty(name)",
                    Type = "BitAiGenerativeProperty?",
                    DefaultValue = "",
                    Description = "A property by name, matched case-insensitively, as models are inconsistent."
                },
            ]
        },
        new()
        {
            Id = "generative-ignore-attribute",
            Title = "BitAiGenerativeIgnoreAttribute",
            Description = "Keeps a parameter out of the set a model may set and out of the schema the model is shown - for a value the app sets itself, such as a service key or an identifier, which is not dangerous but which a model has no business choosing: [Parameter, BitAiGenerativeIgnore] public string TenantId { get; set; }.",
            Parameters =
            [
                new()
                {
                    Name = "[BitAiGenerativeIgnore]",
                    Type = "Property | Field",
                    DefaultValue = "",
                    Description = "Goes on a component parameter, or on a property or field of a parameter's type (a member of a record the component takes), once per member; inherited by overrides. The registry and the schema skip the member, and the binder ignores a value the model sends for it."
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiGenerativeUiView-diagnostics-gap", DefaultValue = "spacing(1)", Description = "Room between two tags of the binding diagnostics." },
        new() { Name = "--bit-AiGenerativeUiView-diagnostics-margin", DefaultValue = "spacing(1)", Description = "Room between the component and its diagnostics." },
    ];



    private static BitAiGenerativeUiPart Card(string componentName, string props)
    {
        var part = new BitAiGenerativeUiPart(componentName);
        part.SetProps(JsonDocument.Parse(props).RootElement);
        part.Complete();
        return part;
    }

    private readonly BitAiGenerativeUiPart osloPart = Card("forecast_card", """{ "city": "Oslo", "degrees": -3, "condition": "Snow", "humidity": 81 }""");

    private const string LisbonForecast = """{ "city": "Lisbon", "degrees": 17, "condition": "Clear", "humidity": 54 }""";

    private bool isStreamingForecast;
    private BitAiGenerativeUiPart? streamedPart;

    private async Task StreamForecastAsync()
    {
        isStreamingForecast = true;
        streamedPart = new BitAiGenerativeUiPart("forecast_card");

        // Five characters at a time, the way a model writes it.
        for (var i = 0; i < LisbonForecast.Length; i += 5)
        {
            streamedPart.AppendProps(LisbonForecast.Substring(i, Math.Min(5, LisbonForecast.Length - i)));
            StateHasChanged();

            await Task.Delay(80);
        }

        streamedPart.Complete();
        isStreamingForecast = false;
    }

    // A scripted transport (Services/AI) that asks for three forecast_card components between two lines of text.
    private readonly BitAiChatSession forecastSession = new(AiDemoInteractiveTransports.GenerativeUi());
    private BitAiRenderScheduler? renderScheduler;
    private JsonElement registrySchema;

    protected override void OnInitialized()
    {
        // The session says so on every streamed event; the scheduler turns any number of those into one render per 50 ms.
        renderScheduler = new(() => InvokeAsync(StateHasChanged));
        forecastSession.StateChanged += renderScheduler.Request;

        registrySchema = GenerativeRegistry.DescribeAll();
    }

    private BitAiMessage? ForecastAnswer => forecastSession.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

    private Task AskForForecastsAsync() => forecastSession.SubmitAsync("The weather in Oslo, Lisbon and Auckland");

    public async ValueTask DisposeAsync()
    {
        if (renderScheduler is not null) await renderScheduler.DisposeAsync();

        await forecastSession.DisposeAsync();
    }

    private readonly BitAiGenerativeUiPart unregisteredPart = Card("stock_ticker", """{ "symbol": "BIT", "price": 42.5 }""");

    private readonly BitAiGenerativeUiPart faultyPart = Card("forecast_card", """{ "degrees": "very cold", "condition": "Windy", "wind": "40 km/h" }""");

    private bool showDiagnostics = true;
    private BitAiGenerativeUiViewParams[] generativeParams => [new() { ShowBindingErrors = showDiagnostics }];

    private readonly BitAiGenerativeUiPart tehranPart = Card("forecast_card", """{ "city": "تهران", "degrees": 24, "condition": "آفتابی", "humidity": 30 }""");
}
