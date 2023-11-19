namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private string? example2SelectedItem;
    private string? example3SelectedItem;



    private List<BitMenuButtonItem> basicItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemStyleClassItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
            Style = "color:red"
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji,
            Class = "custom-item"
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2,
            Style = "background:blue"
        }
    };
    private List<BitMenuButtonItem> itemDisabledItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemTemplateItems = new()
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
    private List<BitMenuButtonItem> basicItemsOnClick = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };

    protected override void OnInitialized()
    {
        Action<BitMenuButtonItem> onClick = item =>
        {
            example2SelectedItem = $"{item.Text} - OnClick";
            StateHasChanged();
        };

        basicItemsOnClick.ForEach(i => i.OnClick = onClick);
    }



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicItems"" />

<BitMenuButton Text=""Standard""
               Items=""basicItems""
               ButtonStyle=""BitButtonStyle.Standard"" />

<BitMenuButton Text=""Text""
               Items=""basicItems""
               ButtonStyle=""BitButtonStyle.Text"" />";
    private readonly string example1CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Item Disabled""
               Items=""itemDisabledItems""
               OnClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<BitMenuButton Text=""Item OnClick"" Items=""basicItemsOnClick"" ButtonStyle=""BitButtonStyle.Standard"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CsharpCode = @"
private string example2SelectedItem;

private List<BitMenuButtonItem> itemDisabledItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};

private List<BitMenuButtonItem> basicItemsOnClick = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};

protected override void OnInitialized()
{
    Action<BitMenuButtonItem> onClick = item =>
    {
        example1SelectedItem = $""{item.Text} - OnClick"";
        StateHasChanged();
    };

    basicItemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example3RazorCode = @"
<BitMenuButton Split Sticky
               Items=""basicItemsOnClick""
               OnClick=""(BitMenuButtonItem item) => example3SelectedItem = item?.Key"" />

<BitMenuButton Split
               Text=""Split""
               Items=""basicItemsOnClick""
               ButtonStyle=""BitButtonStyle.Standard""
               OnClick=""(BitMenuButtonItem item) => example3SelectedItem = item?.Key"" />

<div class=""clicked-item"">Clicked Item: @example3SelectedItem</div>";
    private readonly string example3CsharpCode = @"
private string example3SelectedItem;

private List<BitMenuButtonItem> basicItemsOnClick = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""IconName""
               Items=""basicItems""
               IconName=""@BitIconName.Edit"" />

<BitMenuButton Text=""ChevronDownIcon""
               Items=""basicItems""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown"" />";
    private readonly string example4CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example5RazorCode = @"
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
               Items=""basicItems""
               Style=""width: 200px; height: 40px; background-color: #888;"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicItems""
               Class=""custom-class""  />


<BitMenuButton Text=""Item Styled & Classed Button"" Items=""itemStyleClassItems"" />


<BitMenuButton Text=""Styles""
                Items=""basicItems""
                IconName=""@BitIconName.ExpandMenu""
                Styles=""@(new() { Icon = ""color: red;"",
                                  Text = ""color: aqua;"",
                                  ItemText = ""color: dodgerblue; font-size: 11px;"",
                                  Overlay = ""background-color: var(--bit-clr-bg-overlay);"" })"" />

<BitMenuButton Text=""Classes""
                Items=""basicItems""
                IconName=""@BitIconName.ExpandMenu""
                ButtonStyle=""BitButtonStyle.Standard""
                Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"" />";
    private readonly string example5CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};

private List<BitMenuButtonItem> itemStyleClassItems = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
        Style = ""color:red""
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji,
        Class = ""custom-item""
    },
    new()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2,
        Style = ""background:blue""
    }
};";

    private readonly string example6RazorCode = @"
Visible: [ <BitMenuButton Visibility=""BitVisibility.Visible""
                          Text=""Visible menu button""
                          Items=""basicItems"" /> ]

Hidden: [ <BitMenuButton Visibility=""BitVisibility.Hidden""
                         Text=""Hidden menu button""
                         Items=""basicItems"" /> ]

Collapsed: [ <BitMenuButton Visibility=""BitVisibility.Collapsed""
                            Text=""Collapsed menu button""
                            Items=""basicItems"" /> ]";
    private readonly string example6CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example7RazorCode = @"
<BitMenuButton Items=""basicItems"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicItems"" ButtonStyle=""BitButtonStyle.Standard"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>";
    private readonly string example7CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitMenuButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example8RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               Items=""itemTemplateItems""
               IconName=""@BitIconName.Edit"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               Items=""itemTemplateItems""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Standard"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<div>Clicked Item: @example71SelectedItem</div>


<BitMenuButton Text=""Primary Button""
               Items=""itemTemplateItems2""
               IconName=""@BitIconName.Edit"" />";
    private readonly string example8CsharpCode = @"
private List<BitMenuButtonItem> itemTemplateItems = new()
{
    new BitMenuButtonItem()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new BitMenuButtonItem()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new BitMenuButtonItem()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};

private List<BitMenuButtonItem> itemTemplateItems2 = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add,
        Template = (item => @<div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div>)
    },
    new ()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit,
        Template = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div>)
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete,
        Template = (item => @<div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div>)
    }
};";
}
