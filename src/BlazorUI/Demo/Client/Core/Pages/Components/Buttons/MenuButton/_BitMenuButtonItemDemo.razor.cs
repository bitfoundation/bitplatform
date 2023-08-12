namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
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

    private List<BitMenuButtonItem> basicItemsWithOnClick = default!;

    protected override void OnInitialized()
    {
        Action<BitMenuButtonItem> onClick = item =>
        {
            example1SelectedItem = $"{item.Text} - OnClick";
            StateHasChanged();
        };

        basicItemsWithOnClick = new()
        {
            new()
            {
                Text = "Item A",
                Key = "A",
                IconName = BitIconName.Emoji,
                OnClick = onClick
            },
            new()
            {
                Text = "Item B",
                Key = "B",
                IconName = BitIconName.Emoji,
                OnClick = onClick
            },
            new()
            {
                Text = "Item C",
                Key = "C",
                IconName = BitIconName.Emoji2,
                OnClick = onClick
            }
        };
    }



    private readonly string example1HTMLCode = @"
<BitMenuButton Text=""Primary""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard""
               Items=""basicItems""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Disabled"" Items=""basicItems"" IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
               Items=""itemDisabledItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Item OnClick"" Items=""basicItemsWithOnClick"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
private string example1SelectedItem;

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

private List<BitMenuButtonItem> basicItemsWithOnClick = default!;
protected override void OnInitialized()
{
    Action<BitMenuButtonItem> onClick = item =>
    {
        example1SelectedItem = $""{item.Text} - OnClick"";
        StateHasChanged();
    };
    basicItemsWithOnClick = new()
    {
        new()
        {
            Text = ""Item A"",
            Key = ""A"",
            IconName = BitIconName.Emoji,
            OnClick = onClick
        },
        new()
        {
            Text = ""Item B"",
            Key = ""B"",
            IconName = BitIconName.Emoji,
            OnClick = onClick
        },
        new()
        {
            Text = ""Item C"",
            Key = ""C"",
            IconName = BitIconName.Emoji2,
            OnClick = onClick
        }
    };
}";

    private readonly string example2HTMLCode = @"
<BitMenuButton Text=""Primary Button""
               Items=""basicItems""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard Button""
               Items=""basicItems""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;

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

    private readonly string example3HTMLCode = @"
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
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
               Class=""custom-class"" />

<div>Clicked Item: @example3SelectedItem</div>


<BitMenuButton Text=""Styled Button""
               Items=""itemStyleClassItems""
               OnItemClick=""(BitMenuButtonItem item) => example32SelectedItem = item.Key"" />

<div>Clicked Item: @example32SelectedItem</div>";
    private readonly string example3CSharpCode = @"
private string example31SelectedItem;
private string example32SelectedItem;

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

    private readonly string example4HTMLCode = @"
<BitMenuButton Items=""basicItems"" OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicItems""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string example4SelectedItem;

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

    private readonly string example5HTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               Items=""itemTemplateItems""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonItem item) => example51SelectedItem = item.Key"">
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
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonItem item) => example51SelectedItem = item.Key"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<div>Clicked Item: @example51SelectedItem</div>


<BitMenuButton Text=""Primary Button""
               Items=""itemTemplateItems2""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonItem item) => example52SelectedItem = item.Key"" />

<div>Clicked Item: @example52SelectedItem</div>";
    private readonly string example5CSharpCode = @"
    private string? example51SelectedItem;
    private string? example52SelectedItem;

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

    private readonly string example6HTMLCode = @"
<BitMenuButton Text=""ClassStyles""
               Items=""basicItems""
               IconName=""@BitIconName.ExpandMenu""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               OnItemClick=""(BitMenuButtonItem item) => example6SelectedItem = item.Key"" 
               ClassStyles=""@(new() { Icon = { Style = ""color:red"" },
                                      Text = { Style = ""color:aqua"" },
                                      ChevronDown = { Style = ""color:darkred;font-size:10px"" }})""/>

<div>Clicked Item: @example6SelectedItem</div>";
    private readonly string example6CSharpCode = @"
private string? example6SelectedItem;

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

}
