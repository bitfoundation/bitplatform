namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private string? exampleSelectedCustom;

    private MenuActionItem? changedSelectedCustom;
    private MenuActionItem twoWaySelectedCustom = default!;


    private List<MenuActionItem> basicCustoms = new()
    {
        new() { Name = "Custom A", Id = "A" },
        new() { Name = "Custom B", Id = "B" },
        new() { Name = "Custom C", Id = "C" }
    };

    private List<MenuActionItem> basicIconCustoms = new()
    {
        new()
        {
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };

    private List<MenuActionItem> itemDisabledCustoms = new()
    {
        new()
        {
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };

    private List<MenuActionItem> basicCustomsOnClick = new()
    {
        new()
        {
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };

    private List<MenuActionItem> itemTemplateCustoms = new()
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

    private List<MenuActionItem> itemStyleClassCustoms = new()
    {
        new()
        {
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji,
            Style = "color:red"
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji,
            Class = "custom-item"
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji2,
            Style = "background:blue"
        }
    };

    private List<MenuActionItem> isSelectedCustoms = new()
    {
        new()
        {
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji2,
            IsSelected = true
        }
    };

    private List<MenuActionItem> rtlCustoms = new()
    {
        new()
        {
            Name = "گزینه الف",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "گزینه ب",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "گزینه ج",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };

    protected override void OnInitialized()
    {
        twoWaySelectedCustom = basicCustoms[2];

        Action<MenuActionItem> onClick = item =>
        {
            exampleSelectedCustom = $"{item.Name} - Clicked";
            StateHasChanged();
        };

        basicCustomsOnClick.ForEach(i => i.Clicked = onClick);
    }



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example1CsharpCode = @"
private string example1SelectedItem;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Fill""
               Items=""basicCustoms""
               Variant=""BitVariant.Fill""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Outline""
               Items=""basicCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Text""
               Items=""basicCustoms""
               Variant=""BitVariant.Text""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example2CsharpCode = @"
private string example1SelectedItem;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Customs""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Customs""
               IsEnabled=""false""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example3CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Customs""
               Items=""basicCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Customs""
               IsEnabled=""false""
               Items=""basicCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example4CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""Customs""
               Items=""basicCustoms""
               Variant=""BitVariant.Text""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Customs""
               IsEnabled=""false""
               Items=""basicCustoms""
               Variant=""BitVariant.Text""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example5CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example6RazorCode = @"
<BitMenuButton Split
               Text=""Fill""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Split
               Text=""Outline""
               Items=""basicCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Split
               Text=""Text""
               Items=""basicCustoms""
               Variant=""BitVariant.Text""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example6CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example7RazorCode = @"
<BitMenuButton Sticky
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Split Sticky
               Items=""basicCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />";
    private readonly string example7CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};";

    private readonly string example8RazorCode = @"
<BitMenuButton Text=""IconName""
               Items=""basicIconCustoms""
               IconName=""@BitIconName.Edit""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />

<BitMenuButton Split
               Text=""ChevronDownIcon""
               Items=""basicIconCustoms""
               IconName=""@BitIconName.Add""
               Variant=""BitVariant.Outline""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example8CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicIconCustoms = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};";

    private readonly string example9RazorCode = @"
<BitMenuButton Text=""Customs""
               Items=""itemDisabledCustoms""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnClick=""(MenuActionItem item) => exampleSelectedCustom = item?.Id"" />

<BitMenuButton Split
               Text=""Customs""
               Items=""basicCustomsOnClick""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnClick=""@((MenuActionItem item) => exampleSelectedCustom = ""Main button clicked"")"" />


<BitMenuButton Sticky
               Items=""basicCustomsOnClick""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnClick=""(MenuActionItem item) => exampleSelectedCustom = item?.Id"" />

<BitMenuButton Split Sticky
               Items=""itemDisabledCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnClick=""(MenuActionItem item) => exampleSelectedCustom = item?.Id"" />

<div class=""clicked-item"">Clicked custom item: @exampleSelectedCustom</div>";
    private readonly string example9CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> itemDisabledCustoms = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<MenuActionItem> basicCustomsOnClick = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};";

    private readonly string example10RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Split
               Text=""Customs""
               Items=""itemTemplateCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Name (@item.Id)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Customs""
               Items=""itemTemplateCustoms2""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) },
                                        Template = { Name = nameof(MenuActionItem.Fragment)} })"" />";
    private readonly string example10CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public RenderFragment<MenuActionItem>? Fragment { get; set; }
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};

private List<MenuActionItem> itemTemplateCustoms = new()
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
};

private List<MenuActionItem> itemTemplateCustoms2 = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add,
        Fragment = (item => @<div class=""item-template-box"" style=""color:green"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit,
        Fragment = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete,
        Fragment = (item => @<div class=""item-template-box"" style=""color:red"">@item.Name (@item.Id)</div>)
    }
};";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        overflow: hidden;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-icon {
        color: red;
    }

    .custom-text {
        color: aqua;
    }
</style>

<BitMenuButton Text=""Styled Button""
               Items=""basicCustoms""
               Style=""width: 200px; height: 40px;""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicCustoms""
               Class=""custom-class"" 
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />


<BitMenuButton Text=""Custom Styled & Classed Button""
               Items=""itemStyleClassCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) },
                                        Class = { Name = nameof(MenuActionItem.Class) },
                                        Style = { Name = nameof(MenuActionItem.Style) } })"" />


<BitMenuButton Text=""Styles""
               Items=""basicCustoms""
               IconName=""@BitIconName.ExpandMenu""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })"" 
               Styles=""@(new() { Icon = ""color: red;"",
                                 Text = ""color: aqua;"",
                                 ItemText = ""color: dodgerblue; font-size: 11px;"",
                                 Overlay = ""background-color: var(--bit-clr-bg-overlay);"" })"" />

<BitMenuButton Text=""Classes""
               Items=""basicCustoms""
               IconName=""@BitIconName.ExpandMenu""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" 
               Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"" />";
    private readonly string example11CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};

private List<MenuActionItem> itemStyleClassCustoms = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        Style = ""color:red""
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
        Class = ""custom-item""
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2,
        Style = ""background:blue""
    }
};";

    private readonly string example12RazorCode = @"
<BitMenuButton Split Sticky
               Items=""basicCustoms""
               DefaultSelectedItem=""basicCustoms[1]""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />


<BitMenuButton Sticky
               Items=""basicCustoms""
               @bind-SelectedItem=""twoWaySelectedCustom""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />

<div>Selected item: <b>@twoWaySelectedCustom.Name</b></div>


<BitMenuButton Split Sticky
               Items=""basicCustoms""
               OnChange=""(MenuActionItem item) => changedSelectedCustom = item""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />

<div>Changed item: <b>@changedSelectedCustom?.Name</b></div>


<BitMenuButton Sticky
               Items=""isSelectedCustoms""
               Variant=""BitVariant.Outline""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example12CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public RenderFragment<MenuActionItem>? Fragment { get; set; }
}

private MenuActionItem? changedSelectedCustom;
private MenuActionItem twoWaySelectedCustom = default!;

private List<MenuActionItem> basicCustoms = new()
{
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"" },
    new() { Name = ""Custom C"", Id = ""C"" }
};

private List<MenuActionItem> isSelectedCustoms = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2,
        IsSelected = true
    }
};

protected override void OnInitialized()
{
    twoWaySelectedCustom = basicCustoms[2];
}";

    private readonly string example13RazorCode = @"
<BitMenuButton Text=""گزینه ها""
               Dir=""BitDir.Rtl""
               Items=""rtlCustoms""
               IconName=""@BitIconName.Edit""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />

<BitMenuButton Split
               Text=""گرینه ها""
               Dir=""BitDir.Rtl""
               Items=""rtlCustoms""
               IconName=""@BitIconName.Add""
               Variant=""BitVariant.Outline""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example13CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> rtlCustoms = new()
{
    new()
    {
        Name = ""گزینه الف"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""گزینه ب"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""گزینه ج"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};";
}
