namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private int clickCounter;
    private string? clickedCustom;

    private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

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
<BitButtonGroup Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example1CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""disabledCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""disabledCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms""NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""disabledCustoms""NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" IsEnabled=""false"" />";
    private readonly string example2CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

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
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example3CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example4CsharpCode = @"
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

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />";
    private readonly string example5CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example6RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example6CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

public class ButtonGroupActionItem
{
    public string? Name { get; set; }
}

private List<ButtonGroupActionItem> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
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

<BitButtonGroup Items=""basicCustoms"" Style=""padding:1rem;background:red"" NameSelectors=""nameSelector"" />
<BitButtonGroup Items=""basicCustoms"" Class=""custom-class"" NameSelectors=""nameSelector"" />

<BitButtonGroup Items=""styleClassCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example7CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

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

    private readonly string example8RazorCode = @"
<BitButtonGroup Items=""basicCustoms""
                NameSelectors=""nameSelector""
                OnItemClick=""(ButtonGroupActionItem item) => clickedCustom = item.Name"" />
<div>Clicked item: <b>@clickedCustom</b></div>

<BitButtonGroup Items=""eventsCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon },
                                         OnClick = { Selector = i => i.Clicked } })"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private BitButtonGroupNameSelectors<ButtonGroupActionItem> nameSelector = new() { Text = { Selector = i => i.Name } };

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

    private readonly string example9RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Fill""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Outline""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Text""
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
