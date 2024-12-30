namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupItemDemo
{
    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" IsEnabled=false />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" IsEnabled=false />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" IsEnabled=false />";
    private readonly string example2CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];

private List<BitButtonGroupItem> disabledItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"", IsEnabled = false }, new() { Text = ""Delete"" }
];";

    private readonly string example3RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitButtonGroupItem> iconItems = 
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconItems"" IconOnly />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconItems"" IconOnly />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconItems"" IconOnly />

<BitButtonGroup Variant=""BitVariant.Fill"" Items=""onlyIconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""onlyIconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""onlyIconItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitButtonGroupItem> iconItems = 
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];

private List<BitButtonGroupItem> onlyIconItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""reversedIconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""reversedIconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""reversedIconItems"" />";
    private readonly string example5CsharpCode = @"
private List<BitButtonGroupItem> reversedIconItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add, ReversedIcon = true },
    new() { Text = ""Edit"", IconName = BitIconName.Edit, ReversedIcon = true },
    new() { Text = ""Delete"", IconName = BitIconName.Delete, ReversedIcon = true }
];";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Variant=""BitVariant.Fill"" Items=""toggledItems"" />
<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""toggledItems"" />
<BitButtonGroup Toggle Variant=""BitVariant.Text"" Items=""toggledItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitButtonGroupItem> toggledItems =
[
    new() { OnText = ""Back (2X)"", OffText = ""Back (1X)"", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
    new() { OnTitle = ""Resume"", OffTitle = ""Play"", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
    new() { OnText = ""Forward (2X)"", OffText = ""Forward (1X)"", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
];";

    private readonly string example7RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" Vertical />";
    private readonly string example7CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example8RazorCode = @"
<BitButtonGroup Items=""basicItems"" OnItemClick=""(BitButtonGroupItem item) => clickedItem = item.Text"" />
<div>Clicked item: <b>@clickedItem</b></div>

<BitButtonGroup Items=""eventsItems"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? clickedItem;

private List<BitButtonGroupItem> eventsItems =
[
    new() { Text = ""Increase"", IconName = BitIconName.Add },
    new() { Text = ""Reset"", IconName = BitIconName.Reset },
    new() { Text = ""Decrease"", IconName = BitIconName.Remove }
];

protected override void OnInitialized()
{
    eventsItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
    eventsItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
    eventsItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example9RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example10RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example10CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example11RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example11CsharpCode = @"
private List<BitButtonGroupItem> basicItems = 
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class button {
        color: tomato;
        border-color: tomato;
    }

    .custom-class button:hover {
        color: unset;
        background-color: lightcoral;
    }

    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-btn {
        color: aliceblue;
        border-color: aliceblue;
        background-color: crimson;
    }
</style>


<BitButtonGroup Items=""basicItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" />
<BitButtonGroup Items=""basicItems"" Class=""custom-class"" Variant=""BitVariant.Outline"" />

<BitButtonGroup Items=""styleClassItems"" Variant=""BitVariant.Text"" />

<BitButtonGroup Items=""basicItems""
                Variant=""BitVariant.Text""
                Styles=""@(new() { Button = ""color: darkcyan; border-color: deepskyblue; background-color: azure;"" })"" />

<BitButtonGroup Items=""basicItems""
                Variant=""BitVariant.Text""
                Classes=""@(new() { Button = ""custom-btn"" })"" />";
    private readonly string example12CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];

private List<BitButtonGroupItem> styleClassItems =
[
    new()
    {
        Text = ""Styled"",
        Style = ""color: tomato; border-color: brown; background-color: peachpuff;"",
        IconName = BitIconName.Brush,
    },
    new()
    {
        Text = ""Classed"",
        Class = ""custom-item"",
        IconName = BitIconName.FormatPainter,
    }
];";

    private readonly string example13RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Text"" Items=""rtlItems"" />";
    private readonly string example13CsharpCode = @"
private List<BitButtonGroupItem> rtlItems =
[
    new() { Text = ""اضافه کردن"", IconName = BitIconName.Add },
    new() { Text = ""ویرایش"", IconName = BitIconName.Edit },
    new() { Text = ""حذف"", IconName = BitIconName.Delete }
];";
}
