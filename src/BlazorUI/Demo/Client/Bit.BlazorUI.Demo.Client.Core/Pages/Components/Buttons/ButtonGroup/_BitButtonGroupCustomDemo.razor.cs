namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private int clickCounter;
    private string? clickedCustom;

    private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

    private List<OperationModel> basicCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit" }, new() { Name = "Delete" }
    ];

    private List<OperationModel> disabledCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit", IsEnabled = false }, new() { Name = "Delete" }
    ];

    private List<OperationModel> iconCustoms =
    [
        new() { Name = "Add", Icon = BitIconName.Add },
        new() { Name = "Edit", Icon = BitIconName.Edit },
        new() { Name = "Delete", Icon = BitIconName.Delete }
    ];

    private List<OperationModel> eventsCustoms =
    [
        new() { Name = "Increase", Icon = BitIconName.Add },
        new() { Name = "Reset", Icon = BitIconName.Reset },
        new() { Name = "Decrease", Icon = BitIconName.Remove }
    ];

    private List<OperationModel> styleClassCustoms =
    [
        new()
        {
            Name = "Styled",
            Style = "color: tomato; border-color: brown; background-color: peachpuff;",
            Icon = BitIconName.Brush,
        },
        new()
        {
            Name = "Classed",
            Class = "custom-item",
            Icon = BitIconName.FormatPainter,
        }
    ];

    private List<OperationModel> rtlCustoms =
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
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
}

private List<OperationModel> basicCustoms = new()
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
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<OperationModel> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<OperationModel> disabledCustoms = new()
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
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example3CsharpCode = @"
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
}

private List<OperationModel> basicCustoms = new()
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
public class OperationModel
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private List<OperationModel> iconCustoms = new()
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
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
}

private List<OperationModel> basicCustoms = new()
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
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
}

private List<OperationModel> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};";

    private readonly string example7RazorCode = @"
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
</style>


<BitButtonGroup Items=""basicCustoms"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" NameSelectors=""nameSelector"" />
<BitButtonGroup Items=""basicCustoms"" Class=""custom-class"" Variant=""BitVariant.Outline"" NameSelectors=""nameSelector"" />

<BitButtonGroup Items=""styleClassCustoms""
                Variant=""BitVariant.Text""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example7CsharpCode = @"
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<OperationModel> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<OperationModel> styleClassCustoms = new()
{
    new()
    {
        Name = ""Styled"",
        Style = ""color: tomato; border-color: brown; background-color: peachpuff;"",
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
                OnItemClick=""(OperationModel item) => clickedCustom = item.Name"" />
<div>Clicked item: <b>@clickedCustom</b></div>

<BitButtonGroup Items=""eventsCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Icon },
                                         OnClick = { Selector = i => i.Clicked } })"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private BitButtonGroupNameSelectors<OperationModel> nameSelector = new() { Text = { Selector = i => i.Name } };

public class OperationModel
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public Action<OperationModel>? Clicked { get; set; }
}

private int clickCounter;

private List<OperationModel> basicCustoms = new()
{
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
};

private List<OperationModel> eventsCustoms = new()
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
public class OperationModel
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private List<OperationModel> rtlCustoms = new()
{
    new() { Name = ""اضافه کردن"", Icon = BitIconName.Add },
    new() { Name = ""ویرایش"", Icon = BitIconName.Edit },
    new() { Name = ""حذف"", Icon = BitIconName.Delete }
};";
}
