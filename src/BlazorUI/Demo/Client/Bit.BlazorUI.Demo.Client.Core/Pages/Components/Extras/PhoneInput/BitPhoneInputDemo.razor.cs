namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PhoneInput;

public partial class BitPhoneInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitPhoneInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPhoneInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the phone input.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Countries",
            Type = "ICollection<BitCountry>",
            DefaultValue = "BitCountries.All",
            Description = "The list of the countries to show in the country dropdown.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Country",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The currently selected country of the phone input. (two-way bound)",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "DefaultCountry",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The default selected country to be initially used when the Country parameter is not set.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce time in milliseconds for the number input (applied when Immediate is enabled).",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the country dropdown callout.",
        },
        new()
        {
            Name = "DropdownPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the country dropdown when no country is selected.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the phone input to fill 100% of its container width.",
        },
        new()
        {
            Name = "Immediate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Updates the number input value as the user types (based on the 'oninput' HTML event).",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label of the phone input shown above the field.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the phone input.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Determines the maximum number of characters allowed in the number input.",
        },
        new()
        {
            Name = "NoResultsMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to show when the search result of the country dropdown is empty.",
        },
        new()
        {
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the search box of the country dropdown.",
        },
        new()
        {
            Name = "OnCountryChange",
            Type = "EventCallback<BitCountry?>",
            DefaultValue = "",
            Description = "The callback that is invoked when the selected country changes.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the number input.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the search box of the country dropdown.",
        },
        new()
        {
            Name = "SearchBoxAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label for the search box of the country dropdown.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPhoneInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPhoneInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The throttle time in milliseconds for the number input (applied when Immediate is enabled).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitPhoneInputClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitPhoneInput." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitPhoneInput." },
                new() { Name = "FieldGroup", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the field group (the container of the country dropdown and the number input) of the BitPhoneInput." },
                new() { Name = "Dropdown", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the country dropdown of the BitPhoneInput." },
                new() { Name = "Overlay", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the overlay of the country dropdown of the BitPhoneInput." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the callout of the country dropdown of the BitPhoneInput." },
                new() { Name = "SearchBox", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search box of the country dropdown of the BitPhoneInput." },
                new() { Name = "ScrollContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of the country dropdown of the BitPhoneInput." },
                new() { Name = "DropdownText", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the selected country text (flag and dialing code) of the BitPhoneInput." },
                new() { Name = "ItemName", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the name of each item in the country dropdown of the BitPhoneInput." },
                new() { Name = "ItemCode", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the dialing code of each item in the country dropdown of the BitPhoneInput." },
                new() { Name = "Input", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the number input (text field) of the BitPhoneInput." },
            ]
        },
        new()
        {
            Id = "country",
            Title = "BitCountry",
            Description = "Represents the basic information of a specific country.",
            Parameters =
            [
                new() { Name = "Name", Type = "string", Description = "The full name of the country." },
                new() { Name = "Code", Type = "string", Description = "The dialing code of the country." },
                new() { Name = "Iso2", Type = "string", Description = "The ISO 3166-1 alpha-2 code of the country." },
                new() { Name = "Iso3", Type = "string", Description = "The ISO 3166-1 alpha-3 code of the country." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Value = "0" },
                new() { Name = "Secondary", Value = "1" },
                new() { Name = "Tertiary", Value = "2" },
                new() { Name = "Info", Value = "3" },
                new() { Name = "Success", Value = "4" },
                new() { Name = "Warning", Value = "5" },
                new() { Name = "SevereWarning", Value = "6" },
                new() { Name = "Error", Value = "7" },
            ]
        }
    ];



    private string? bindingNumber;
    private BitCountry? bindingCountry;
    private BitCountry? changedCountry;
    private string? immediateNumber;
    private string? debouncedNumber;
    private string? throttledNumber;
    private readonly List<BitCountry> customCountries =
    [
        BitCountries.UnitedStates,
        BitCountries.Canada,
        BitCountries.UnitedKingdom,
        BitCountries.Germany,
        BitCountries.France,
    ];



    private readonly string example1RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Enter your number"" />";

    private readonly string example2RazorCode = @"
<BitPhoneInput @bind-Value=""bindingNumber""
               @bind-Country=""bindingCountry""
               DefaultCountry=""BitCountries.Germany""
               Placeholder=""Enter your number"" />

<div>Number: @bindingNumber</div>
<div>Country: @bindingCountry?.Name</div>
<div>Full number: +@bindingCountry?.Code@bindingNumber</div>";
    private readonly string example2CsharpCode = @"
private string? bindingNumber;
private BitCountry? bindingCountry;";

    private readonly string example3RazorCode = @"
<BitPhoneInput Label=""Phone number""
               DefaultCountry=""BitCountries.UnitedKingdom""
               Placeholder=""Enter your number"" />";

    private readonly string example4RazorCode = @"
<BitPhoneInput NoSearchBox DefaultCountry=""BitCountries.France"" Placeholder=""Enter your number"" />";

    private readonly string example5RazorCode = @"
<BitPhoneInput Countries=""customCountries""
               DefaultCountry=""BitCountries.Canada""
               Placeholder=""Enter your number"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitCountry> customCountries =
[
    BitCountries.UnitedStates,
    BitCountries.Canada,
    BitCountries.UnitedKingdom,
    BitCountries.Germany,
    BitCountries.France,
];";

    private readonly string example6RazorCode = @"
<BitPhoneInput FullWidth DefaultCountry=""BitCountries.Italy"" Placeholder=""Enter your number"" />";

    private readonly string example7RazorCode = @"
<BitPhoneInput Immediate
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""immediateNumber"" />
<div>Value: [@immediateNumber]</div>

<BitPhoneInput Immediate
               DebounceTime=""500""
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""debouncedNumber"" />
<div>Value: [@debouncedNumber]</div>

<BitPhoneInput Immediate
               ThrottleTime=""500""
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""throttledNumber"" />
<div>Value: [@throttledNumber]</div>";
    private readonly string example7CsharpCode = @"
private string? immediateNumber;
private string? debouncedNumber;
private string? throttledNumber;";

    private readonly string example8RazorCode = @"
<BitPhoneInput IsEnabled=""false"" DefaultCountry=""BitCountries.Spain"" Value=""1234567"" />

<BitPhoneInput ReadOnly DefaultCountry=""BitCountries.Spain"" Value=""1234567"" />";
    private readonly string example8CsharpCode = @"";

    private readonly string example9RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.UnitedStates""
               OnCountryChange=""c => changedCountry = c""
               Placeholder=""Enter your number"" />

<div>Selected country: @changedCountry?.Name</div>";
    private readonly string example9CsharpCode = @"
private BitCountry? changedCountry;";

    private readonly string example10RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.Netherlands""
               Placeholder=""Enter your number""
               Style=""width: 300px;""
               Class=""custom-class""
               Styles=""@(new() { FieldGroup = ""border-color: blueviolet;"" })""
               Classes=""@(new() { Input = ""custom-input"" })"" />";
    private readonly string example10CsharpCode = @"";

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitPhoneInput Dir=""BitDir.Rtl"" DefaultCountry=""BitCountries.Iran"" Placeholder=""شماره خود را وارد کنید"" />
</div>";
}
