namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private int clickCounter;
    private string? clickedCustom;

    private List<ButtonGroupActionItem> basicCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit" }, new() { Name = "Delete" }
    ];

    private List<ButtonGroupActionItem> disabledCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit", IsEnabled = false }, new() { Name = "Delete" }
    ];

    private List<ButtonGroupActionItem> iconCustoms =
    [
        new() { Name = "Add", Icon = BitIconName.Add },
        new() { Name = "Edit", Icon = BitIconName.Edit },
        new() { Name = "Delete", Icon = BitIconName.Delete }
    ];

    private List<ButtonGroupActionItem> eventsCustoms =
    [
        new() { Name = "Increase", Icon = BitIconName.Add },
        new() { Name = "Reset", Icon = BitIconName.Reset },
        new() { Name = "Decrease", Icon = BitIconName.Remove }
    ];

    private List<ButtonGroupActionItem> styleClassCustoms =
    [
        new()
        {
            Name = "Styled",
            Style = "color:darkred",
            Icon = BitIconName.Brush,
        },
        new()
        {
            Name = "Classed",
            Class = "custom-item",
            Icon = BitIconName.FormatPainter,
        }
    ];

    private List<ButtonGroupActionItem> rtlCustoms =
    [
        new() { Name = "اضافه کردن", Icon = BitIconName.Add },
        new() { Name = "ویرایش", Icon = BitIconName.Edit },
        new() { Name = "حذف", Icon = BitIconName.Delete }
    ];

    protected override void OnInitialized()
    {
        eventsCustoms[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
        eventsCustoms[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
        eventsCustoms[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
    }



    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicCustoms"" NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />";
    private readonly string example1CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example2RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""disabledCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms"" IsEnabled=""false""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""disabledCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms"" IsEnabled=""false""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""disabledCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms"" IsEnabled=""false""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />";
    private readonly string example2CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<ButtonGroupActionItem> disabledCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"", IsEnabled = false }, new() { Name = ""Delete"" }
};";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example3CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private List<ButtonGroupActionItem> iconCustoms = new()
{
    new() { Name = ""Add"", Icon = BitIconName.Add },
    new() { Name = ""Edit"", Icon = BitIconName.Edit },
    new() { Name = ""Delete"", Icon = BitIconName.Delete }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms"" Vertical
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms"" Vertical
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms"" Vertical
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />";
    private readonly string example4CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example5RazorCode = @"
<BitButtonGroup Items=""basicCustoms""
                OnItemClick=""(ButtonGroupActionItem item) => clickedCustom = item.Name""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<div>Clicked item: <b>@clickedCustom</b></div>

<BitButtonGroup Items=""eventsCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon },
                                         OnClick = { Selector = i => i.Clicked } })"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example5CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private int clickCounter;

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<ButtonGroupActionItem> eventsCustoms = new()
{
    new() { Name = ""Increase"", Icon = BitIconName.Add },
    new() { Name = ""Reset"", Icon = BitIconName.Reset },
    new() { Name = ""Decrease"", Icon = BitIconName.Remove }
};

protected override void OnInitialized()
{
    eventsCustoms[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
    eventsCustoms[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
    eventsCustoms[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Info"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Success"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Color=""BitColor.Error"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />";
    private readonly string example6CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Small"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Small"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Size=""BitSize.Medium"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Medium"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Size=""BitSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Large"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Size=""BitSize.Large"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />";
    private readonly string example7CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
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

<BitButtonGroup Items=""basicCustoms"" Style=""padding:1rem;background:red""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />
<BitButtonGroup Items=""basicCustoms"" Class=""custom-class""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name } })"" />

<BitButtonGroup Items=""styleClassCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example8CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<ButtonGroupActionItem> styleClassCustoms = new()
{
    new()
    {
        Name = ""Styled"",
        Style = ""color:darkred"",
        Icon = BitIconName.Brush,
    },
    new()
    {
        Name = ""Classed"",
        Class = ""custom-item"",
        Icon = BitIconName.FormatPainter,
    }
};";

    private readonly string example9RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                ButtonStyle=""BitButtonStyle.Primary""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                ButtonStyle=""BitButtonStyle.Text""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example9CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private List<ButtonGroupActionItem> rtlCustoms = new()
{
    new() { Name = ""اضافه کردن"", Icon = BitIconName.Add },
    new() { Name = ""ویرایش"", Icon = BitIconName.Edit },
    new() { Name = ""حذف"", Icon = BitIconName.Delete }
};";
}
