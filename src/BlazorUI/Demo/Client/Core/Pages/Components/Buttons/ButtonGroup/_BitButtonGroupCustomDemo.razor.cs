namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private int clickCounter;

    private List<ButtonGroupActionItem> basicCustomItems = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };

    private List<ButtonGroupActionItem> primaryCustomItems = new()
    {
        new()
        {
            Name = "Add (Disabled)",
            Id = "add-key",
            Icon = BitIconName.Add,
            IsEnabled = false
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };

    private List<ButtonGroupActionItem> standardCustomItems = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new()
        {
            Name = "Edit (Disabled)",
            Id = "edit-key",
            Icon = BitIconName.Edit,
            IsEnabled = false
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };

    private List<ButtonGroupActionItem> textCustomItems = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new()
        {
            Name = "Delete (Disabled)",
            Id = "delete-key",
            Icon = BitIconName.Delete,
            IsEnabled = false
        }
    };

    private List<ButtonGroupActionItem> counterCustomItems = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new()
        {
            Name = "Reset",
            Id = "reset-key",
            Icon = BitIconName.Refresh
        },
        new()
        {
            Name = "Remove",
            Id = "remove-key",
            Icon = BitIconName.Remove
        }
    };

    private List<ButtonGroupActionItem> styleClassCustomItems = new()
    {
        new()
        {
            Name = "Styled",
            Id = "styled-key",
            Icon = BitIconName.Brush,
            Style = "color:darkblue; font-weight:bold;"
        },
        new()
        {
            Name = "Classed",
            Id = "classed-key",
            Icon = BitIconName.FormatPainter,
            Class = "custom-class"
        }
    };

    private List<ButtonGroupActionItem> visibilityCustomItems = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new()
        {
            Name = "Ok",
            Id = "ok-key",
            Icon = BitIconName.CheckMark
        }
    };

    protected override void OnInitialized()
    {
        counterCustomItems[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
        counterCustomItems[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
        counterCustomItems[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
    }



    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example1CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> basicCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example2RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary""
                Items=""primaryCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example2CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> primaryCustomItems = new()
{
    new()
    {
        Name = ""Add (Disabled)"",
        Id = ""add-key"",
        Icon = BitIconName.Add,
        IsEnabled = false
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard""
                Items=""standardCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example3CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> standardCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit (Disabled)"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit,
        IsEnabled = false
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text""
                Items=""textCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example4CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> textCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete (Disabled)"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete,
        IsEnabled = false
    }
};";

    private readonly string example5RazorCode = @"
<div>Click count: @clickCounter</div>
<BitButtonGroup Items=""counterCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         OnClick = { Name = nameof(ButtonGroupActionItem.Clicked) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example5CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private int clickCounter;

private List<ButtonGroupActionItem> counterCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Reset"",
        Id = ""reset-key"",
        Icon = BitIconName.Refresh
    },
    new()
    {
        Name = ""Remove"",
        Id = ""remove-key"",
        Icon = BitIconName.Remove
    }
};

protected override void OnInitialized()
{
    counterCustomItems[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
    counterCustomItems[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
    counterCustomItems[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitButtonColor.Info""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Info""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Info""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />


<BitButtonGroup Color=""BitButtonColor.Success""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Success""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Success""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />


<BitButtonGroup Color=""BitButtonColor.Warning""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Warning""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Warning""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />


<BitButtonGroup Color=""BitButtonColor.SevereWarning""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.SevereWarning""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.SevereWarning""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />


<BitButtonGroup Color=""BitButtonColor.Error""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Error""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Color=""BitButtonColor.Error""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example6CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> basicCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small""
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Size=""BitButtonSize.Medium""
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Size=""BitButtonSize.Large""
                ButtonStyle=""BitButtonStyle.Primary""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example7CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> basicCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary""
                Items=""styleClassCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example8CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> styleClassCustomItems = new()
{
    new()
    {
        Name = ""Styled"",
        Id = ""styled-key"",
        Icon = BitIconName.Brush,
        Style = ""color:darkblue; font-weight:bold;""
    },
    new()
    {
        Name = ""Classed"",
        Id = ""classed-key"",
        Icon = BitIconName.FormatPainter,
        Class = ""custom-class""
    }
};";

    private readonly string example9RazorCode = @"
<BitButtonGroup Vertical
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Vertical 
                ButtonStyle=""BitButtonStyle.Standard""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />

<BitButtonGroup Vertical 
                ButtonStyle=""BitButtonStyle.Text""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                         Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                         Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" />";
    private readonly string example9CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> basicCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example10RazorCode = @"
Visible: [ <BitButtonGroup Visibility=""BitVisibility.Visible""
                           Items=""visibilityCustomItems""
                           NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                                    Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                                    Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" /> ]

Hidden: [ <BitButtonGroup Visibility=""BitVisibility.Hidden""
                          Items=""visibilityCustomItems""
                          NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                                   Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                                   Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" /> ]

Collapsed: [ <BitButtonGroup Visibility=""BitVisibility.Collapsed""
                             Items=""visibilityCustomItems""
                             NameSelectors=""@(new() { IconName = { Name = nameof(ButtonGroupActionItem.Icon) },
                                                      Key = { Name = nameof(ButtonGroupActionItem.Id) },
                                                      Text = { Name = nameof(ButtonGroupActionItem.Name) } })"" /> ]";
    private readonly string example10CsharpCode = @"
public class ButtonGroupActionItem
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<ButtonGroupActionItem>? Clicked { get; set; }
}

private List<ButtonGroupActionItem> visibilityCustomItems = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new()
    {
        Name = ""Ok"",
        Id = ""ok-key"",
        Icon = BitIconName.CheckMark
    }
};";
}
