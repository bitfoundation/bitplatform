namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxItemDemo
{
    private const string basicItemsCode = @"
private readonly List<BitListBoxItem<string>> basicItems =
[
    new() { Text = ""Apple"", Value = ""apple"" },
    new() { Text = ""Banana"", Value = ""banana"" },
    new() { Text = ""Cherry"", Value = ""cherry"" },
    new() { Text = ""Grape"", Value = ""grape"" },
    new() { Text = ""Orange"", Value = ""orange"" }
];";

    private readonly string example1RazorCode = @"
<BitListBox Label=""Fruit"" Items=""basicItems"" @bind-Value=""basicValue"" />
<div>Value: <b>@basicValue</b></div>";
    private readonly string example1CsharpCode = @"
private string? basicValue;
" + basicItemsCode;

    private readonly string example2RazorCode = @"
<BitListBox Label=""Fruits"" Items=""basicItems"" MultiSelect @bind-Values=""multiValues"" />
<div>Values: <b>@string.Join("", "", multiValues ?? [])</b></div>";
    private readonly string example2CsharpCode = @"
private IEnumerable<string?>? multiValues;
" + basicItemsCode;

    private readonly string example3RazorCode = @"
<BitListBox Label=""Shipping"" Items=""iconItems"" DefaultValue=""@(""standard"")"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitListBoxItem<string>> iconItems =
[
    new() { Text = ""Standard"", Description = ""3 to 5 days, free"", IconName = BitIconName.Car, Value = ""standard"" },
    new() { Text = ""Express"", Description = ""Next day, $9.99"", IconName = BitIconName.Airplane, Value = ""express"" },
    new() { Text = ""Pick up"", Description = ""From a store near you"", IconName = BitIconName.Shop, Value = ""pickup"" }
];";

    private readonly string example4RazorCode = @"
<BitListBox Label=""Some disabled"" Items=""disabledItems"" TItem=""BitListBoxItem<string>"" TValue=""string"" />

<BitListBox Label=""Disabled"" Items=""basicItems"" DefaultValue=""@(""banana"")"" Disabled />";
    private readonly string example4CsharpCode = @"
private readonly List<BitListBoxItem<string>> disabledItems =
[
    new() { Text = ""Apple"", Value = ""apple"" },
    new() { Text = ""Banana"", Value = ""banana"", IsDisabled = true },
    new() { Text = ""Cherry"", Value = ""cherry"" },
    new() { Text = ""Grape"", Value = ""grape"", IsDisabled = true },
    new() { Text = ""Orange"", Value = ""orange"" }
];
" + basicItemsCode;

    private readonly string example5RazorCode = @"
<BitListBox Label=""Read only"" Items=""basicItems"" DefaultValue=""@(""cherry"")"" ReadOnly />";
    private readonly string example5CsharpCode = basicItemsCode;

    private readonly string example6RazorCode = @"
<BitListBox Label=""Country"" Items=""countryItems"" MaxHeight=""12rem"" @bind-Value=""countryValue"" />
<div>Value: <b>@countryValue</b></div>";
    private readonly string example6CsharpCode = @"
private string? countryValue;

private readonly List<BitListBoxItem<string>> countryItems =
[
    .. new[]
    {
        ""Argentina"", ""Australia"", ""Austria"", ""Belgium"", ""Brazil"", ""Canada"", ""Chile"", ""China"", ""Denmark"", ""Egypt"",
        ""Finland"", ""France"", ""Germany"", ""Greece"", ""India"", ""Indonesia"", ""Iran"", ""Ireland"", ""Italy"", ""Japan"",
        ""Mexico"", ""Netherlands"", ""Norway"", ""Poland"", ""Portugal"", ""Spain"", ""Sweden"", ""Turkey""
    }.Select(name => new BitListBoxItem<string> { Text = name, Value = name })
];";

    private readonly string example7RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""() => validationResult = validationModel.ToString()"">
    <DataAnnotationsValidator />

    <BitListBox Label=""Fruit (required)"" Items=""basicItems"" @bind-Value=""validationModel.Fruit"" Required ShowValidationMessage />
    <BitListBox Label=""Fruits (at least two)"" Items=""basicItems"" MultiSelect @bind-Values=""validationModel.Fruits"" ShowValidationMessage />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

<div>Submitted: <b>@validationResult</b></div>

<BitListBox Label=""With an error message"" Items=""basicItems"" TItem=""BitListBoxItem<string>"" TValue=""string"" ErrorMessage=""This fruit is out of season."" />";
    private readonly string example7CsharpCode = @"
public class ListBoxValidationModel
{
    [Required(ErrorMessage = ""Pick a fruit."")]
    public string? Fruit { get; set; }

    [MinLength(2, ErrorMessage = ""Pick at least two fruits."")]
    public IEnumerable<string?>? Fruits { get; set; } = [];

    public override string ToString() => $""{Fruit} / {string.Join("", "", Fruits ?? [])}"";
}

private readonly ListBoxValidationModel validationModel = new();
private string? validationResult;
" + basicItemsCode;

    private readonly string example8RazorCode = @"
<BitListBox Items=""basicItems"" TItem=""BitListBoxItem<string>"" TValue=""string""
            AriaLabel=""Fruits""
            MultiSelect
            OnSelectItem=""HandleOnSelectItem""
            OnDeselectItem=""HandleOnDeselectItem"" />
<div>Last event: <b>@eventsLog</b></div>";
    private readonly string example8CsharpCode = @"
private string? eventsLog;

private void HandleOnSelectItem(BitListBoxItem<string> item) => eventsLog = $""Selected {item.Text}"";

private void HandleOnDeselectItem(BitListBoxItem<string> item) => eventsLog = $""Deselected {item.Text}"";
" + basicItemsCode;

    private readonly string example9RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitListBox Label=""FontAwesome"" Items=""externalIconItems1"" DefaultValue=""@(""car"")"" />

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitListBox Label=""Bootstrap Icons"" Items=""externalIconItems2"" DefaultValue=""@(""bike"")"" />";
    private readonly string example9CsharpCode = @"
private readonly List<BitListBoxItem<string>> externalIconItems1 =
[
    new() { Text = ""Car"", Icon = ""fa-solid fa-car"", Value = ""car"" },
    new() { Text = ""Train"", Icon = BitIconInfo.Fa(""solid train""), Value = ""train"" },
    new() { Text = ""Plane"", Icon = BitIconInfo.Fa(""solid plane""), Value = ""plane"" }
];

private readonly List<BitListBoxItem<string>> externalIconItems2 =
[
    new() { Text = ""Bike"", Icon = BitIconInfo.Bi(""bicycle""), Value = ""bike"" },
    new() { Text = ""Bus"", Icon = BitIconInfo.Bi(""bus-front""), Value = ""bus"" },
    new() { Text = ""Taxi"", Icon = BitIconInfo.Bi(""taxi-front""), Value = ""taxi"" }
];";

    private readonly string example10RazorCode = @"
<style>
    .custom-class .bit-lbx-lst {
        border-width: 0.125rem;
        border-color: dodgerblue;
        border-radius: 0.75rem;
    }

    .custom-item .bit-lbx-txt {
        font-weight: bold;
        color: dodgerblue;
    }

    .custom-selected {
        background-color: #ff634733;
    }
</style>

<BitListBox Label=""Style"" Items=""basicItems"" DefaultValue=""@(""banana"")"" Style=""--bit-ListBox-selected-color: tomato; --bit-ListBox-selected-background: #ff63471f;"" />

<BitListBox Label=""Class"" Items=""basicItems"" DefaultValue=""@(""banana"")"" Class=""custom-class"" />

<BitListBox Label=""Item's Style & Class"" Items=""styleClassItems"" TItem=""BitListBoxItem<string>"" TValue=""string"" />

<BitListBox Label=""Styles & Classes""
            Items=""basicItems""
            MultiSelect
            DefaultValues=""@([""apple"", ""cherry""])""
            Styles=""@(new() { List = ""border-radius: 0.75rem;"", Text = ""font-style: italic;"" })""
            Classes=""@(new() { SelectedItem = ""custom-selected"" })"" />";
    private readonly string example10CsharpCode = @"
private readonly List<BitListBoxItem<string>> styleClassItems =
[
    new() { Text = ""Apple"", Value = ""apple"", Style = ""color: seagreen;"" },
    new() { Text = ""Banana"", Value = ""banana"", Class = ""custom-item"" },
    new() { Text = ""Cherry"", Value = ""cherry"" }
];
" + basicItemsCode;

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه"" Items=""rtlItems"" DefaultValue=""@(""banana"")"" />

    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه‌ها"" Items=""rtlItems"" MultiSelect DefaultValues=""@([""apple"", ""cherry""])"" />
</div>";
    private readonly string example11CsharpCode = @"
private readonly List<BitListBoxItem<string>> rtlItems =
[
    new() { Text = ""سیب"", Value = ""apple"" },
    new() { Text = ""موز"", Value = ""banana"" },
    new() { Text = ""گیلاس"", Value = ""cherry"" }
];";
}
