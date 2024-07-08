namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupItemDemo
{
    private int clickCounter;
    private string? clickedItem;

    private List<BitButtonGroupItem> basicItems =
    [
        new() { Text = "Add" }, new() { Text = "Edit" }, new() { Text = "Delete" }
    ];

    private List<BitButtonGroupItem> disabledItems =
    [
        new() { Text = "Add" }, new() { Text = "Edit", IsEnabled = false }, new() { Text = "Delete" }
    ];

    private List<BitButtonGroupItem> iconItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { Text = "Edit", IconName = BitIconName.Edit },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    ];

    private List<BitButtonGroupItem> eventsItems =
    [
        new() { Text = "Increase", IconName = BitIconName.Add },
        new() { Text = "Reset", IconName = BitIconName.Reset },
        new() { Text = "Decrease", IconName = BitIconName.Remove }
    ];

    private List<BitButtonGroupItem> styleClassItems =
    [
        new()
        {
            Text = "Styled",
            Style = "color:darkred",
            IconName = BitIconName.Brush,
        },
        new()
        {
            Text = "Classed",
            Class = "custom-item",
            IconName = BitIconName.FormatPainter,
        }
    ];

    private List<BitButtonGroupItem> rtlItems =
    [
        new() { Text = "اضافه کردن", IconName = BitIconName.Add },
        new() { Text = "ویرایش", IconName = BitIconName.Edit },
        new() { Text = "حذف", IconName = BitIconName.Delete }
    ];

    protected override void OnInitialized()
    {
        eventsItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
        eventsItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
        eventsItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
    }



    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

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
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};

private List<BitButtonGroupItem> disabledItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"", IsEnabled = false }, new() { Text = ""Delete"" }
};";

    private readonly string example3RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Severity=""BitSeverity.Info"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Info"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Info"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Severity=""BitSeverity.Success"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Success"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Success"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Severity=""BitSeverity.Warning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Warning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Warning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Severity=""BitSeverity.Error"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Error"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Severity=""BitSeverity.Error"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitButtonGroupItem> iconItems = new()
{
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
};";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" Vertical />";
    private readonly string example5CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example6RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        padding: 2rem;
        border-radius:1rem;
        background-color: blueviolet;
    }

    .custom-item {
        color: blueviolet;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup Items=""basicItems"" Style=""padding:1rem;background:red"" />
<BitButtonGroup Items=""basicItems"" Class=""custom-class"" />

<BitButtonGroup Items=""styleClassItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};

private List<BitButtonGroupItem> styleClassItems = new()
{
    new()
    {
        Text = ""Styled"",
        Style = ""color:darkred"",
        IconName = BitIconName.Brush,
    },
    new()
    {
        Text = ""Classed"",
        Class = ""custom-item"",
        IconName = BitIconName.FormatPainter,
    }
};";

    private readonly string example8RazorCode = @"
<BitButtonGroup Items=""basicItems"" OnItemClick=""(BitButtonGroupItem item) => clickedItem = item.Text"" />
<div>Clicked item: <b>@clickedItem</b></div>

<BitButtonGroup Items=""eventsItems"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? clickedItem;

private List<BitButtonGroupItem> eventsItems = new()
{
    new() { Text = ""Increase"", IconName = BitIconName.Add },
    new() { Text = ""Reset"", IconName = BitIconName.Reset },
    new() { Text = ""Decrease"", IconName = BitIconName.Remove }
};

protected override void OnInitialized()
{
    eventsItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
    eventsItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
    eventsItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example9RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Text"" Items=""rtlItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitButtonGroupItem> rtlItems = new()
{
    new() { Text = ""اضافه کردن"", IconName = BitIconName.Add },
    new() { Text = ""ویرایش"", IconName = BitIconName.Edit },
    new() { Text = ""حذف"", IconName = BitIconName.Delete }
};";
}
