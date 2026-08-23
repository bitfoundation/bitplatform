namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoPage
{
    private const string REPO_URL = "https://github.com/bitfoundation/bitplatform";

    /// <summary>Host of the demo videos, which are proxied same-origin - see <see cref="_introductionVideoUrl"/>.</summary>
    private const string VIDEOS_BASE_URL = "https://videos.bitplatform.dev/";

    private bool _forceAnimation;

    /// <summary>
    /// This page's entry in the catalog, which is what supplies the category shown above the title.
    /// Null for a component that has no nav entry, in which case the eyebrow is simply not rendered.
    /// </summary>
    private ComponentCatalogItem? _catalogItem;

    /// <summary>
    /// The NuGet package this component ships in. Which package a component belongs to is not
    /// something a reader can infer from the component itself, and reaching for a BitChart or a
    /// BitDataGrid without referencing Bit.BlazorUI.Extras is the most common false start with this
    /// library - so the page says so beside the title rather than only in the install guide.
    /// </summary>
    private string _packageName = "Bit.BlazorUI";

    /// <summary>The component's own source on GitHub, and the same URL in edit mode.</summary>
    private string? _sourceUrl;
    private string? _sourceEditUrl;

    [Parameter] public string Name { get; set; } = default!;
    [Parameter] public string[]? SecondaryNames { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }
    [Parameter] public string? Notes { get; set; }
    [Parameter] public RenderFragment? NotesTemplate { get; set; }
    [Parameter] public string? IntroductionVideoUrl { get; set; }
    [Parameter] public string? Introduction { get; set; }
    [Parameter] public RenderFragment? IntroductionTemplate { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Examples { get; set; }
    [Parameter] public List<ComponentParameter> Parameters { get; set; } = [];
    [Parameter] public List<ComponentSubClass> SubClasses { get; set; } = [];
    [Parameter] public List<ComponentSubEnum> SubEnums { get; set; } = [];
    [Parameter] public List<ComponentParameter> PublicMembers { get; set; } = [];
    [Parameter] public string? GitHubUrl { get; set; }
    [Parameter] public string? GitHubExtrasUrl { get; set; }
    [Parameter] public string? GitHubLegacyUrl { get; set; }
    [Parameter] public string? GitHubDemoUrl { get; set; }
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }


    /// <summary>
    /// The url the &lt;video&gt; element is actually given. The web demo is served cross-origin
    /// isolated with Cross-Origin-Embedder-Policy: require-corp (see Middlewares.cs), under which a
    /// cross-origin subresource is blocked unless it carries a Cross-Origin-Resource-Policy header
    /// or is requested in CORS mode; the video host sends neither CORP nor
    /// Access-Control-Allow-Origin, so both routes are dead ends and the video simply does not
    /// play. It therefore comes through the same-origin passthrough of VideosController instead -
    /// COEP only constrains cross-origin resources.
    /// <para>
    /// Blazor Hybrid keeps the url verbatim: its origin (app://0.0.0.0) hosts no such endpoint, and
    /// a hybrid WebView is not cross-origin isolated, so the direct url both works and is the only
    /// one that resolves there. Same reasoning as the CesiumJS BaseUrl in BitMapDemo.
    /// </para>
    /// </summary>
    private string? _introductionVideoUrl =>
        AppRenderMode.IsBlazorHybrid || IntroductionVideoUrl?.StartsWith(VIDEOS_BASE_URL, StringComparison.OrdinalIgnoreCase) is not true
            ? IntroductionVideoUrl
            : $"/api/videos/{IntroductionVideoUrl![VIDEOS_BASE_URL.Length..]}";


    protected override Task OnParamsSetAsync()
    {
        _catalogItem = ComponentCatalog.Find(NavigationManager.Uri);

        // Exactly one of the three source parameters is set on any given page, and which one it is
        // says both which package the component comes from and where its source lives.
        (_packageName, _sourceUrl) =
            GitHubExtrasUrl.HasValue() ? ("Bit.BlazorUI.Extras", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI.Extras/Components/{GitHubExtrasUrl}")
            : GitHubLegacyUrl.HasValue() ? ("Bit.BlazorUI.Legacy", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI.Legacy/Components/{GitHubLegacyUrl}")
            : GitHubUrl.HasValue() ? ("Bit.BlazorUI", $"{REPO_URL}/blob/develop/src/BlazorUI/Bit.BlazorUI/Components/{GitHubUrl}")
            : ("Bit.BlazorUI", null);

        _sourceEditUrl = _sourceUrl?.Replace("/blob/", "/edit/", StringComparison.Ordinal);

        return base.OnParamsSetAsync();
    }

    private readonly List<ComponentParameter> _componentBaseParameters =
    [
        new()
        {
            Name = "AriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the accessible label for the component, used by assistive technologies.",
        },
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the CSS class name(s) to apply to the rendered element.",
        },
        new()
        {
            Name = "Dir",
            Type = "BitDir?",
            DefaultValue = "null",
            Description = "Gets or sets the text directionality for the component's content.",
            LinkType = LinkType.Link,
            Href = "#component-dir",
        },
        new()
        {
            Name = "ForceAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gets or sets a value indicating whether the component's animations play at their full duration even when reduced motion is requested.",
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.",
        },
        new()
        {
            Name = "Id",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the unique identifier for the component's root element.",
        },
        new()
        {
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Gets or sets a value indicating whether the component is enabled and can respond to user interaction.",
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the CSS style string to apply to the rendered element.",
        },
        new()
        {
            Name = "TabIndex",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the tab order index for the component when navigating with the keyboard.",
        },
        new()
        {
            Name = "Visibility",
            Type = "BitVisibility",
            DefaultValue = "BitVisibility.Visible",
            Description = "Gets or sets the visibility state (visible, hidden, or collapsed) of the component.",
            LinkType = LinkType.Link,
            Href = "#component-visibility",
        },
    ];

    private readonly List<ComponentParameter> _componentBasePublicMembers =
    [
        new()
        {
            Name = "UniqueId",
            Type = "Guid",
            DefaultValue = "Guid.NewGuid()",
            Description = "Gets the readonly unique identifier for the component's root element, assigned when the component instance is constructed.",
        },
        new()
        {
            Name = "RootElement",
            Type = "ElementReference",
            Description = "Gets the reference to the root HTML element associated with this component.",
        },
    ];

    private readonly List<ComponentSubEnum> _componentBaseSubEnums =
    [
        new()
        {
            Id = "component-visibility",
            Name = "BitVisibility",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Visible",
                    Value="0",
                    Description="The content of the component is visible.",
                },
                new()
                {
                    Name= "Hidden",
                    Value="1",
                    Description="The content of the component is hidden, but the space it takes on the page remains (visibility:hidden).",
                },
                new()
                {
                    Name= "Collapsed",
                    Value="2",
                    Description="The component is hidden (display:none).",
                }
            ]
        },
        new()
        {
            Id = "component-dir",
            Name = "BitDir",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Ltr",
                    Value="0",
                    Description="Ltr (left to right) is to be used for languages that are written from the left to the right (like English).",
                },
                new()
                {
                    Name= "Rtl",
                    Value="1",
                    Description="Rtl (right to left) is to be used for languages that are written from the right to the left (like Arabic).",
                },
                new()
                {
                    Name= "Auto",
                    Value="2",
                    Description="Auto lets the user agent decide. It uses a basic algorithm as it parses the characters inside the element until it finds a character with a strong directionality, then applies that directionality to the whole element.",
                }
            ]
        }
    ];



    private readonly List<string> _inputComponents = [
        "Calendar", "Checkbox", "ChoiceGroup", "DatePicker", "DateRangePicker", "Dropdown", "NumberField", "OtpInput", "Rating",
        "SearchBox", "TextField", "TimePicker", "CircularTimePicker", "Toggle", "TagsInput"
    ];

    private readonly List<ComponentParameter> _inputBaseParameters =
    [
        new()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "The default value of the input to be used in uncontrolled mode (i.e. when the Value is not bound), typically used alongside the OnChange callback.",
        },
        new()
        {
            Name = "DisplayName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the display name for this field.",
        },
        new()
        {
            Name = "InputHtmlAttributes",
            Type = "IReadOnlyDictionary<string, object>?",
            DefaultValue = "null",
            Description = "Gets or sets a collection of additional attributes that will be applied to the created element.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the element. Allows access by name from the associated form.",
        },
        new()
        {
            Name = "NoValidate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the validation of the input.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TValue?>",
            DefaultValue = "",
            Description = "Callback for when the input value changes.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the input read-only.",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the input required.",
        },
        new()
        {
            Name = "Value",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Gets or sets the value of the input. This should be used with two-way binding.",
        },
    ];

    private readonly List<ComponentParameter> _inputBasePublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference of the input element.",
        },
        new()
        {
            Name = "FocusAsync()",
            Type = "() => ValueTask",
            Description = "Gives focus to the input element.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "(bool preventScroll) => ValueTask",
            Description = "Gives focus to the input element.",
        },
    ];



    private readonly List<string> _textInputComponents = [
        "NumberField", "TextField", "SearchBox", "PhoneInput"
    ];

    private readonly List<ComponentParameter> _textInputBaseParameters =
    [
        new()
        {
            Name = "AutoComplete",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the value of the autocomplete attribute of the input component.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the text input is auto focused on first render.",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce time in milliseconds.",
        },
        new()
        {
            Name = "Immediate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change the content of the input field when the user write text (based on 'oninput' HTML event).",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The throttle time in milliseconds.",
        },
    ];



    private readonly List<string> _notInheritedComponents = [
        "CascadingValueProvider", "Chart", "ChartLegacy", "DataGrid", "DataGridLegacy", "ModalService", "Params", "ProModalService"
    ];
}
