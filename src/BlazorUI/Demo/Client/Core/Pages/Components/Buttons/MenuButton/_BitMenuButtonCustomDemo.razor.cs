namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private List<MenuActionItem> basicCustoms = new()
    {
        new()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };

    private List<MenuActionItem> itemStyleClassCustoms = new()
    {
        new()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
            Style = "color:red"
        },
        new()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
            Class = "custom-item"
        },
        new()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2,
            Style = "background:blue"
        }
    };

    private List<MenuActionItem> itemDisabledCustoms = new()
    {
        new()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Item C",
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

    private List<MenuActionItem> basicCustomsWithOnClick = default!;

    protected override void OnInitialized()
    {
        Action<MenuActionItem> onClick = item =>
        {
            example1SelectedItem = $"{item.Name} - Clicked";
            StateHasChanged();
        };

        basicCustomsWithOnClick = new()
        {
            new()
            {
                Name = "Item A",
                Id = "A",
                Icon = BitIconName.Emoji,
                Clicked = onClick
            },
            new()
            {
                Name = "Item B",
                Id = "B",
                Icon = BitIconName.Emoji,
                Clicked = onClick
            },
            new()
            {
                Name = "Item C",
                Id = "C",
                Icon = BitIconName.Emoji2,
                Clicked = onClick
            }
        };
    }



    private readonly string example1CustomItemHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Disabled""
               Items=""basicCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id""
               IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
               Items=""itemDisabledCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CustomItemCSharpCode = @"
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
    new MenuActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<MenuActionItem> itemDisabledCustoms = new()
{
    new MenuActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new MenuActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<MenuActionItem> basicCustomsWithOnClick = default!;
protected override void OnInitialized()
{
    Action<MenuActionItem> onClick = item =>
    {
        example1SelectedItem = $""{item.Name} - Clicked"";
        StateHasChanged();
    };

    basicCustomsWithOnClick = new()
    {
        new()
        {
            Name = ""Item A"",
            Id = ""A"",
            Icon = BitIconName.Emoji,
            Clicked = onClick
        },
        new()
        {
            Name = ""Item B"",
            Id = ""B"",
            Icon = BitIconName.Emoji,
            Clicked = onClick
        },
        new()
        {
            Name = ""Item C"",
            Id = ""C"",
            Icon = BitIconName.Emoji2,
            Clicked = onClick
        }
    };
}";

    private readonly string example2CustomItemHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CustomItemCSharpCode = @"
private string example2SelectedItem;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new MenuActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example3CustomItemHTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }
</style>

<BitMenuButton Text=""Styled Button""
               Items=""basicCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example31SelectedItem = item.Id""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example31SelectedItem = item.Id""
               Class=""custom-class"" />

<div>Clicked Item: @example31SelectedItem</div>


<BitMenuButton Text=""Item Styled & Classed Button""
               Items=""itemStyleClassCustoms"" 
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               StyleField=""@nameof(MenuActionItem.Style)""
               ClassField=""@nameof(MenuActionItem.Class)""
               OnItemClick=""(MenuActionItem item) => example32SelectedItem = item.Id"" />

<div>Clicked Item: @example32SelectedItem</div>";
    private readonly string example3CustomItemCSharpCode = @"
private string example31SelectedItem;
private string example32SelectedItem;

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
    new MenuActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<MenuActionItem> itemStyleClassCustoms = new()
{
    new()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        Style = ""color:red""
    },
    new()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
        Class = ""custom-item""
    },
    new()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2,
        Style = ""background:blue""
    }
};";


    private readonly string example4CustomItemHTMLCode = @"
<BitMenuButton Items=""basicCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example4SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Primary"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example4SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Standard"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4CustomItemCSharpCode = @"
private string example4SelectedItem;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicCustoms = new()
{
    new MenuActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new MenuActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example5CustomItemHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateCustoms""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example51SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Primary"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Name (@item.Id)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateCustoms""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example51SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Standard"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Name (@item.Id)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<div>Clicked Item: @example51SelectedItem</div>


<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateCustoms2""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               TemplateField=""@nameof(MenuActionItem.Fragment)""
               OnItemClick=""(MenuActionItem item) => example52SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Primary"" />

<div>Clicked Item: @example52SelectedItem</div>";
    private readonly string example5CustomItemCSharpCode = @"
private string example51SelectedItem;
private string example52SelectedItem;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public RenderFragment<MenuActionItem>? Fragment { get; set; }
}

private List<MenuActionItem> itemTemplateCustoms = new()
{
    new MenuActionItem()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new MenuActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new MenuActionItem()
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
}
