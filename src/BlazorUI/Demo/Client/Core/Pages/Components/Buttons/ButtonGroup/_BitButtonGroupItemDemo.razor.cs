using System.Diagnostics.Metrics;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupItemDemo
{
    private int clickCounter;

    private List<BitButtonGroupItem> basicItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

    private List<BitButtonGroupItem> primaryItems = new()
    {
        new()
        {
            Text = "Add (Disabled)",
            Key = "add-key",
            IconName = BitIconName.Add,
            IsEnabled = false
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

    private List<BitButtonGroupItem> standardItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Edit (Disabled)",
            Key = "edit-key",
            IconName = BitIconName.Edit,
            IsEnabled = false
        },
        new()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

    private List<BitButtonGroupItem> textItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new()
        {
            Text = "Delete (Disabled)",
            Key = "delete-key",
            IconName = BitIconName.Delete,
            IsEnabled = false
        }
    };

    private List<BitButtonGroupItem> counterItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Reset",
            Key = "reset-key",
            IconName = BitIconName.Refresh
        },
        new()
        {
            Text = "Remove",
            Key = "remove-key",
            IconName = BitIconName.Remove
        }
    };

    private List<BitButtonGroupItem> styleClassItems = new()
    {
        new()
        {
            Text = "Styled",
            Key = "styled-key",
            IconName = BitIconName.Brush,
            Style = "color:darkblue; font-weight:bold;"
        },
        new()
        {
            Text = "Classed",
            Key = "classed-key",
            IconName = BitIconName.FormatPainter,
            Class = "custom-class"
        }
    };

    private List<BitButtonGroupItem> visibilityItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new()
        {
            Text = "Ok",
            Key = "ok-key",
            IconName = BitIconName.CheckMark
        }
    };

    protected override void OnInitialized()
    {
        counterItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
        counterItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
        counterItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
    }



    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicItems"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example2RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""primaryItems"" />";
    private readonly string example2CsharpCode = @"
private List<BitButtonGroupItem> primaryItems = new()
{
    new()
    {
        Text = ""Add (Disabled)"",
        Key = ""add-key"",
        IconName = BitIconName.Add,
        IsEnabled = false
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" Items=""standardItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitButtonGroupItem> standardItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit (Disabled)"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit,
        IsEnabled = false
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" Items=""textItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitButtonGroupItem> textItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete (Disabled)"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete,
        IsEnabled = false
    }
};";

    private readonly string example5RazorCode = @"
<div>Click count: @clickCounter</div>
<BitButtonGroup Items=""counterItems"" />";
    private readonly string example5CsharpCode = @"
private int clickCounter;

private List<BitButtonGroupItem> counterItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Reset"",
        Key = ""reset-key"",
        IconName = BitIconName.Refresh
    },
    new()
    {
        Text = ""Remove"",
        Key = ""remove-key"",
        IconName = BitIconName.Remove
    }
};

protected override void OnInitialized()
{
    counterItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
    counterItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
    counterItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitButtonColor.Info"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitButtonColor.Success"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitButtonColor.Warning"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitButtonColor.SevereWarning"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitButtonColor.Error"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" Items=""basicItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" Items=""styleClassItems"" />";
    private readonly string example8CsharpCode = @"
private List<BitButtonGroupItem> styleClassItems = new()
{
    new()
    {
        Text = ""Styled"",
        Key = ""styled-key"",
        IconName = BitIconName.Brush,
        Style = ""color:darkblue; font-weight:bold;""
    },
    new()
    {
        Text = ""Classed"",
        Key = ""classed-key"",
        IconName = BitIconName.FormatPainter,
        Class = ""custom-class""
    }
};";

    private readonly string example9RazorCode = @"
<BitButtonGroup Vertical Items=""basicItems"" />

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Standard"" Items=""basicItems"" />

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Text"" Items=""basicItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitButtonGroupItem> basicItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};";

    private readonly string example10RazorCode = @"
Visible: [ <BitButtonGroup Visibility=""BitVisibility.Visible"" Items=""visibilityItems"" /> ]

Hidden: [ <BitButtonGroup Visibility=""BitVisibility.Hidden"" Items=""visibilityItems"" /> ]

Collapsed: [ <BitButtonGroup Visibility=""BitVisibility.Collapsed"" Items=""visibilityItems"" /> ]";
    private readonly string example10CsharpCode = @"
private List<BitButtonGroupItem> visibilityItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new()
    {
        Text = ""Ok"",
        Key = ""ok-key"",
        IconName = BitIconName.CheckMark
    }
};";
}
