namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoPage
{
    private const string REPO_URL = "https://github.com/bitfoundation/bitplatform";

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
    [Parameter] public string? GitHubDemoUrl { get; set; }



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
        "SearchBox", "TextField", "TimePicker", "CircularTimePicker", "Toggle"
    ];

    private readonly List<ComponentParameter> _inputBaseParameters =
    [
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
        "NumberField", "TextField", "SearchBox"
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



    private readonly List<string> _notInheritedComponents = ["DataGrid", "Chart", "ModalService"];
}
