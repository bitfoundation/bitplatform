namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupItemDemo
{
    private int clickCounter;
    private string? clickedItem;

    private List<BitButtonGroupItem> basicItems = new()
    {
        new() { Text = "Add" }, new() { Text = "Edit" }, new() { Text = "Delete" }
    };

    private List<BitButtonGroupItem> disabledItems = new()
    {
        new() { Text = "Add" }, new() { Text = "Edit", IsEnabled = false }, new() { Text = "Delete" }
    };

    private List<BitButtonGroupItem> iconItems = new()
    {
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { Text = "Edit", IconName = BitIconName.Edit },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    };

    private List<BitButtonGroupItem> eventsItems = new()
    {
        new() { Text = "Increase", IconName = BitIconName.Add },
        new() { Text = "Reset", IconName = BitIconName.Reset },
        new() { Text = "Decrease", IconName = BitIconName.Remove }
    };

    private List<BitButtonGroupItem> styleClassItems = new()
    {
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
    };

    private List<BitButtonGroupItem> rtlItems = new()
    {
        new() { Text = "اضافه کردن", IconName = BitIconName.Add },
        new() { Text = "ویرایش", IconName = BitIconName.Edit },
        new() { Text = "حذف", IconName = BitIconName.Delete }
    };

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
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""disabledItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" IsEnabled=false />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""disabledItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" IsEnabled=false />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""disabledItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" IsEnabled=false />";
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
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""iconItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""iconItems"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""iconItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitButtonGroupItem> iconItems = new()
{
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" Vertical />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" Vertical />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" Vertical />";
    private readonly string example4CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example5RazorCode = @"
<BitButtonGroup Items=""basicItems"" OnItemClick=""(BitButtonGroupItem item) => clickedItem = item.Text"" />
<div>Clicked item: <b>@clickedItem</b></div>

<BitButtonGroup Items=""eventsItems"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example5CsharpCode = @"
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

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />
<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
};";

    private readonly string example8RazorCode = @"
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
    private readonly string example8CsharpCode = @"
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

    private readonly string example9RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" ButtonStyle=""BitButtonStyle.Primary"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" ButtonStyle=""BitButtonStyle.Standard"" Items=""rtlItems"" />
<BitButtonGroup Dir=""BitDir.Rtl"" ButtonStyle=""BitButtonStyle.Text"" Items=""rtlItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitButtonGroupItem> rtlItems = new()
{
    new() { Text = ""اضافه کردن"", IconName = BitIconName.Add },
    new() { Text = ""ویرایش"", IconName = BitIconName.Edit },
    new() { Text = ""حذف"", IconName = BitIconName.Delete }
};";
}
