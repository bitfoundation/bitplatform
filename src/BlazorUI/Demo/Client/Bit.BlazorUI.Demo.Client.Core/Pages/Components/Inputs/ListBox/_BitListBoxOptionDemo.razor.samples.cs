namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxOptionDemo
{
    private const string fruitOptions = @"
    <BitListBoxOption Text=""Apple"" Value=""@(""apple"")"" />
    <BitListBoxOption Text=""Banana"" Value=""@(""banana"")"" />
    <BitListBoxOption Text=""Cherry"" Value=""@(""cherry"")"" />
    <BitListBoxOption Text=""Grape"" Value=""@(""grape"")"" />
    <BitListBoxOption Text=""Orange"" Value=""@(""orange"")"" />
</BitListBox>";

    private readonly string example1RazorCode = @"
<BitListBox Label=""Fruit"" TItem=""BitListBoxOption<string>"" TValue=""string"" @bind-Value=""basicValue"">" + fruitOptions + @"
<div>Value: <b>@basicValue</b></div>";
    private readonly string example1CsharpCode = @"
private string? basicValue;";

    private readonly string example2RazorCode = @"
<BitListBox Label=""Fruits"" TItem=""BitListBoxOption<string>"" TValue=""string"" MultiSelect @bind-Values=""multiValues"">" + fruitOptions + @"
<div>Values: <b>@string.Join("", "", multiValues ?? [])</b></div>";
    private readonly string example2CsharpCode = @"
private IEnumerable<string?>? multiValues;";

    private readonly string example3RazorCode = @"
<BitListBox Label=""Shipping"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""standard"")"">
    <BitListBoxOption Text=""Standard"" Description=""3 to 5 days, free"" IconName=""@BitIconName.Car"" Value=""@(""standard"")"" />
    <BitListBoxOption Text=""Express"" Description=""Next day, $9.99"" IconName=""@BitIconName.Airplane"" Value=""@(""express"")"" />
    <BitListBoxOption Text=""Pick up"" Description=""From a store near you"" IconName=""@BitIconName.Shop"" Value=""@(""pickup"")"" />
</BitListBox>";

    private readonly string example4RazorCode = @"
<BitListBox Label=""Some disabled"" TItem=""BitListBoxOption<string>"" TValue=""string"">
    <BitListBoxOption Text=""Apple"" Value=""@(""apple"")"" />
    <BitListBoxOption Text=""Banana"" Value=""@(""banana"")"" IsDisabled />
    <BitListBoxOption Text=""Cherry"" Value=""@(""cherry"")"" />
    <BitListBoxOption Text=""Grape"" Value=""@(""grape"")"" IsDisabled />
    <BitListBoxOption Text=""Orange"" Value=""@(""orange"")"" />
</BitListBox>

<BitListBox Label=""Disabled"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""banana"")"" Disabled>" + fruitOptions;

    private readonly string example5RazorCode = @"
<BitListBox Label=""Read only"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""cherry"")"" ReadOnly>" + fruitOptions;

    private readonly string example6RazorCode = @"
<BitListBox Label=""Country"" TItem=""BitListBoxOption<string>"" TValue=""string"" MaxHeight=""12rem"" @bind-Value=""countryValue"">
    @foreach (var country in countries)
    {
        <BitListBoxOption Text=""@country"" Value=""@country"" />
    }
</BitListBox>
<div>Value: <b>@countryValue</b></div>";
    private readonly string example6CsharpCode = @"
private string? countryValue;

private static readonly string[] countries =
[
    ""Argentina"", ""Australia"", ""Austria"", ""Belgium"", ""Brazil"", ""Canada"", ""Chile"", ""China"", ""Denmark"", ""Egypt"",
    ""Finland"", ""France"", ""Germany"", ""Greece"", ""India"", ""Indonesia"", ""Iran"", ""Ireland"", ""Italy"", ""Japan"",
    ""Mexico"", ""Netherlands"", ""Norway"", ""Poland"", ""Portugal"", ""Spain"", ""Sweden"", ""Turkey""
];";

    private readonly string example7RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""() => validationResult = validationModel.ToString()"">
    <DataAnnotationsValidator />

<BitListBox Label=""Fruit (required)"" TItem=""BitListBoxOption<string>"" TValue=""string"" @bind-Value=""validationModel.Fruit"" Required ShowValidationMessage>" + fruitOptions + @"

<BitListBox Label=""Fruits (at least two)"" TItem=""BitListBoxOption<string>"" TValue=""string"" MultiSelect @bind-Values=""validationModel.Fruits"" ShowValidationMessage>" + fruitOptions + @"

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

<div>Submitted: <b>@validationResult</b></div>

<BitListBox Label=""With an error message"" TItem=""BitListBoxOption<string>"" TValue=""string"" ErrorMessage=""This fruit is out of season."">" + fruitOptions;
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
private string? validationResult;";

    private readonly string example8RazorCode = @"
<BitListBox TItem=""BitListBoxOption<string>"" TValue=""string""
            AriaLabel=""Fruits""
            MultiSelect
            OnSelectItem=""HandleOnSelectItem""
            OnDeselectItem=""HandleOnDeselectItem"">" + fruitOptions + @"
<div>Last event: <b>@eventsLog</b></div>";
    private readonly string example8CsharpCode = @"
private string? eventsLog;

private void HandleOnSelectItem(BitListBoxOption<string> item) => eventsLog = $""Selected {item.Text}"";

private void HandleOnDeselectItem(BitListBoxOption<string> item) => eventsLog = $""Deselected {item.Text}"";";

    private readonly string example9RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitListBox Label=""FontAwesome"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""car"")"">
    <BitListBoxOption Text=""Car"" Icon=""@(""fa-solid fa-car"")"" Value=""@(""car"")"" />
    <BitListBoxOption Text=""Train"" Icon=""@BitIconInfo.Fa(""solid train"")"" Value=""@(""train"")"" />
    <BitListBoxOption Text=""Plane"" Icon=""@BitIconInfo.Fa(""solid plane"")"" Value=""@(""plane"")"" />
</BitListBox>

<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitListBox Label=""Bootstrap Icons"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""bike"")"">
    <BitListBoxOption Text=""Bike"" Icon=""@BitIconInfo.Bi(""bicycle"")"" Value=""@(""bike"")"" />
    <BitListBoxOption Text=""Bus"" Icon=""@BitIconInfo.Bi(""bus-front"")"" Value=""@(""bus"")"" />
    <BitListBoxOption Text=""Taxi"" Icon=""@BitIconInfo.Bi(""taxi-front"")"" Value=""@(""taxi"")"" />
</BitListBox>";

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

<BitListBox Label=""Style"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""banana"")"" Style=""--bit-ListBox-selected-color: tomato; --bit-ListBox-selected-background: #ff63471f;"">" + fruitOptions + @"

<BitListBox Label=""Class"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""banana"")"" Class=""custom-class"">" + fruitOptions + @"

<BitListBox Label=""Item's Style & Class"" TItem=""BitListBoxOption<string>"" TValue=""string"">
    <BitListBoxOption Text=""Apple"" Value=""@(""apple"")"" Style=""color: seagreen;"" />
    <BitListBoxOption Text=""Banana"" Value=""@(""banana"")"" Class=""custom-item"" />
    <BitListBoxOption Text=""Cherry"" Value=""@(""cherry"")"" />
</BitListBox>

<BitListBox Label=""Styles & Classes""
            TItem=""BitListBoxOption<string>"" TValue=""string""
            MultiSelect
            DefaultValues=""@([""apple"", ""cherry""])""
            Styles=""@(new() { List = ""border-radius: 0.75rem;"", Text = ""font-style: italic;"" })""
            Classes=""@(new() { SelectedItem = ""custom-selected"" })"">" + fruitOptions;

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه"" TItem=""BitListBoxOption<string>"" TValue=""string"" DefaultValue=""@(""banana"")"">
        <BitListBoxOption Text=""سیب"" Value=""@(""apple"")"" />
        <BitListBoxOption Text=""موز"" Value=""@(""banana"")"" />
        <BitListBoxOption Text=""گیلاس"" Value=""@(""cherry"")"" />
    </BitListBox>

    <BitListBox Dir=""BitDir.Rtl"" Label=""میوه‌ها"" TItem=""BitListBoxOption<string>"" TValue=""string"" MultiSelect DefaultValues=""@([""apple"", ""cherry""])"">
        <BitListBoxOption Text=""سیب"" Value=""@(""apple"")"" />
        <BitListBoxOption Text=""موز"" Value=""@(""banana"")"" />
        <BitListBoxOption Text=""گیلاس"" Value=""@(""cherry"")"" />
    </BitListBox>
</div>";
}
