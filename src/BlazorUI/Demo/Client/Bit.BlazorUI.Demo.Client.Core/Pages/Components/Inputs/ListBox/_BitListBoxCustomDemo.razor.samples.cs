namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxCustomDemo
{
    private const string nameSelectorsCode = @"
public class Produce
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Note { get; set; }
    public string? Symbol { get; set; }
    public BitIconInfo? Glyph { get; set; }
    public bool OutOfSeason { get; set; }
    public string? Css { get; set; }
    public string? Inline { get; set; }
}

private static readonly BitListBoxNameSelectors<Produce, string> nameSelectors = new()
{
    Text = { Name = nameof(Produce.Name) },
    Value = { Name = nameof(Produce.Code) },
    Description = { Name = nameof(Produce.Note) },
    IconName = { Name = nameof(Produce.Symbol) },
    Icon = { Selector = p => p.Glyph },
    IsDisabled = { Name = nameof(Produce.OutOfSeason) },
    Class = { Name = nameof(Produce.Css) },
    Style = { Name = nameof(Produce.Inline) },
};
";

    private const string basicItemsCode = @"
private readonly List<Produce> basicItems =
[
    new() { Name = ""Apple"", Code = ""apple"" },
    new() { Name = ""Banana"", Code = ""banana"" },
    new() { Name = ""Cherry"", Code = ""cherry"" },
    new() { Name = ""Grape"", Code = ""grape"" },
    new() { Name = ""Orange"", Code = ""orange"" }
];";

    private readonly string example1RazorCode = @"
<BitListBox Label=""Fruit"" Items=""basicItems"" NameSelectors=""nameSelectors"" @bind-Value=""basicValue"" />
<div>Value: <b>@basicValue</b></div>";
    private readonly string example1CsharpCode = nameSelectorsCode + @"
private string? basicValue;
" + basicItemsCode;

    private readonly string example2RazorCode = @"
<BitListBox Label=""Fruits"" Items=""basicItems"" NameSelectors=""nameSelectors"" MultiSelect @bind-Values=""multiValues"" />
<div>Values: <b>@string.Join("", "", multiValues ?? [])</b></div>";
    private readonly string example2CsharpCode = nameSelectorsCode + @"
private IEnumerable<string?>? multiValues;
" + basicItemsCode;

    private readonly string example3RazorCode = @"
<BitListBox Label=""Shipping"" Items=""iconItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""standard"")"" />";
    private readonly string example3CsharpCode = nameSelectorsCode + @"
private readonly List<Produce> iconItems =
[
    new() { Name = ""Standard"", Note = ""3 to 5 days, free"", Symbol = BitIconName.Car, Code = ""standard"" },
    new() { Name = ""Express"", Note = ""Next day, $9.99"", Symbol = BitIconName.Airplane, Code = ""express"" },
    new() { Name = ""Pick up"", Note = ""From a store near you"", Symbol = BitIconName.Shop, Code = ""pickup"" }
];";

    private readonly string example4RazorCode = @"
<BitListBox Label=""Some disabled"" Items=""disabledItems"" NameSelectors=""nameSelectors"" />

<BitListBox Label=""Disabled"" Items=""basicItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""banana"")"" Disabled />";
    private readonly string example4CsharpCode = nameSelectorsCode + @"
private readonly List<Produce> disabledItems =
[
    new() { Name = ""Apple"", Code = ""apple"" },
    new() { Name = ""Banana"", Code = ""banana"", OutOfSeason = true },
    new() { Name = ""Cherry"", Code = ""cherry"" },
    new() { Name = ""Grape"", Code = ""grape"", OutOfSeason = true },
    new() { Name = ""Orange"", Code = ""orange"" }
];
" + basicItemsCode;

    private readonly string example5RazorCode = @"
<BitListBox Label=""Read only"" Items=""basicItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""cherry"")"" ReadOnly />";
    private readonly string example5CsharpCode = nameSelectorsCode + basicItemsCode;

    private readonly string example6RazorCode = @"
<BitListBox Label=""Country"" Items=""countryItems"" NameSelectors=""nameSelectors"" MaxHeight=""12rem"" @bind-Value=""countryValue"" />
<div>Value: <b>@countryValue</b></div>";
    private readonly string example6CsharpCode = nameSelectorsCode + @"
private string? countryValue;

private readonly List<Produce> countryItems =
[
    .. new[]
    {
        ""Argentina"", ""Australia"", ""Austria"", ""Belgium"", ""Brazil"", ""Canada"", ""Chile"", ""China"", ""Denmark"", ""Egypt"",
        ""Finland"", ""France"", ""Germany"", ""Greece"", ""India"", ""Indonesia"", ""Iran"", ""Ireland"", ""Italy"", ""Japan"",
        ""Mexico"", ""Netherlands"", ""Norway"", ""Poland"", ""Portugal"", ""Spain"", ""Sweden"", ""Turkey""
    }.Select(name => new Produce { Name = name, Code = name })
];";

    private readonly string example7RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""() => validationResult = validationModel.ToString()"">
    <DataAnnotationsValidator />

    <BitListBox Label=""Fruit (required)"" Items=""basicItems"" NameSelectors=""nameSelectors"" @bind-Value=""validationModel.Fruit"" Required ShowValidationMessage />
    <BitListBox Label=""Fruits (at least two)"" Items=""basicItems"" NameSelectors=""nameSelectors"" MultiSelect @bind-Values=""validationModel.Fruits"" ShowValidationMessage />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

<div>Submitted: <b>@validationResult</b></div>

<BitListBox Label=""With an error message"" Items=""basicItems"" NameSelectors=""nameSelectors"" ErrorMessage=""This fruit is out of season."" />";
    private readonly string example7CsharpCode = nameSelectorsCode + @"
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
<BitListBox Items=""basicItems"" NameSelectors=""nameSelectors"" TItem=""Produce"" TValue=""string""
            AriaLabel=""Fruits""
            MultiSelect
            OnSelectItem=""HandleOnSelectItem""
            OnDeselectItem=""HandleOnDeselectItem"" />
<div>Last event: <b>@eventsLog</b></div>";
    private readonly string example8CsharpCode = nameSelectorsCode + @"
private string? eventsLog;

private void HandleOnSelectItem(Produce item) => eventsLog = $""Selected {item.Name}"";

private void HandleOnDeselectItem(Produce item) => eventsLog = $""Deselected {item.Name}"";
" + basicItemsCode;

    private readonly string example9RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitListBox Label=""FontAwesome"" Items=""externalIconItems1"" NameSelectors=""nameSelectors"" DefaultValue=""@(""car"")"" />

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitListBox Label=""Bootstrap Icons"" Items=""externalIconItems2"" NameSelectors=""nameSelectors"" DefaultValue=""@(""bike"")"" />";
    private readonly string example9CsharpCode = nameSelectorsCode + @"
private readonly List<Produce> externalIconItems1 =
[
    new() { Name = ""Car"", Glyph = ""fa-solid fa-car"", Code = ""car"" },
    new() { Name = ""Train"", Glyph = BitIconInfo.Fa(""solid train""), Code = ""train"" },
    new() { Name = ""Plane"", Glyph = BitIconInfo.Fa(""solid plane""), Code = ""plane"" }
];

private readonly List<Produce> externalIconItems2 =
[
    new() { Name = ""Bike"", Glyph = BitIconInfo.Bi(""bicycle""), Code = ""bike"" },
    new() { Name = ""Bus"", Glyph = BitIconInfo.Bi(""bus-front""), Code = ""bus"" },
    new() { Name = ""Taxi"", Glyph = BitIconInfo.Bi(""taxi-front""), Code = ""taxi"" }
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

<BitListBox Label=""Style"" Items=""basicItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""banana"")"" Style=""--bit-ListBox-selected-color: tomato; --bit-ListBox-selected-background: #ff63471f;"" />

<BitListBox Label=""Class"" Items=""basicItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""banana"")"" Class=""custom-class"" />

<BitListBox Label=""Item's Style & Class"" Items=""styleClassItems"" NameSelectors=""nameSelectors"" />

<BitListBox Label=""Styles & Classes""
            Items=""basicItems"" NameSelectors=""nameSelectors""
            MultiSelect
            DefaultValues=""@([""apple"", ""cherry""])""
            Styles=""@(new() { List = ""border-radius: 0.75rem;"", Text = ""font-style: italic;"" })""
            Classes=""@(new() { SelectedItem = ""custom-selected"" })"" />";
    private readonly string example10CsharpCode = nameSelectorsCode + @"
private readonly List<Produce> styleClassItems =
[
    new() { Name = ""Apple"", Code = ""apple"", Inline = ""color: seagreen;"" },
    new() { Name = ""Banana"", Code = ""banana"", Css = ""custom-item"" },
    new() { Name = ""Cherry"", Code = ""cherry"" }
];
" + basicItemsCode;

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه"" Items=""rtlItems"" NameSelectors=""nameSelectors"" DefaultValue=""@(""banana"")"" />

    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه‌ها"" Items=""rtlItems"" NameSelectors=""nameSelectors"" MultiSelect DefaultValues=""@([""apple"", ""cherry""])"" />
</div>";
    private readonly string example11CsharpCode = nameSelectorsCode + @"
private readonly List<Produce> rtlItems =
[
    new() { Name = ""سیب"", Code = ""apple"" },
    new() { Name = ""موز"", Code = ""banana"" },
    new() { Name = ""گیلاس"", Code = ""cherry"" }
];";
}
