namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class ComponentDemo
{
    [Parameter] public string ComponentName { get; set; } = default!;
    [Parameter] public string? ComponentDescription { get; set; }
    [Parameter] public string? Notes { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public List<ComponentParameter> ComponentParameters { get; set; } = [];
    [Parameter] public List<ComponentSubClass> ComponentSubClasses { get; set; } = [];
    [Parameter] public List<ComponentSubEnum> ComponentSubEnums { get; set; } = [];
    [Parameter] public List<ComponentParameter> ComponentPublicMembers { get; set; } = [];



    private readonly List<ComponentParameter> _componentBaseParameters =
    [
        new()
        {
            Name = "AriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the control for the benefit of screen readers.",
        },
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS class for the root element of the component.",
        },
        new()
        {
            Name = "Dir",
            Type = "BitDir?",
            DefaultValue = "null",
            Description = "Determines the component direction.",
            LinkType = LinkType.Link,
            Href = "#component-dir",
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional attributes in addition to the component's parameters.",
        },
        new()
        {
            Name = "Id",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom id attribute for the root element. if null the UniqueId will be used instead.",
        },
        new()
        {
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether or not the component is enabled.",
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS style for the root element of the component.",
        },
        new()
        {
            Name = "Visibility",
            Type = "BitVisibility",
            DefaultValue = "BitVisibility.Visible",
            Description = "Whether the component is visible, hidden or collapsed.",
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
            Description = "The readonly unique id of the root element. it will be assigned to a new Guid at component instance construction.",
        },
        new()
        {
            Name = "RootElement",
            Type = "ElementReference",
            Description = "The ElementReference of the root element.",
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
        "SearchBox", "SpinButton", "TextField", "TimePicker", "CircularTimePicker", "Toggle"
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
