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



    private readonly string example1BitMenuButtonItemHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Disabled""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key""
               IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
               Items=""itemDisabledItems""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1BitMenuButtonItemCSharpCode = @"
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
";

    private readonly string example2BitMenuButtonItemHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2BitMenuButtonItemCSharpCode = @"
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

    private readonly string example3BitMenuButtonItemHTMLCode = @"
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
    private readonly string example3BitMenuButtonItemCSharpCode = @"
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

    private readonly string example4BitMenuButtonItemHTMLCode = @"
<BitMenuButton Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key""
               ButtonStyle=""BitButtonStyle.Primary"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicItems""
               OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key""
               ButtonStyle=""BitButtonStyle.Standard"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4BitMenuButtonItemCSharpCode = @"
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

    private readonly string example5BitMenuButtonItemHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateItems""
               OnItemClick=""(BitMenuButtonItem item) => example5SelectedItem = item.Key""
               ButtonStyle=""BitButtonStyle.Primary"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateItems""
               OnItemClick=""(BitMenuButtonItem item) => example5SelectedItem = item.Key""
               ButtonStyle=""BitButtonStyle.Standard"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<div>Clicked Item: @example5SelectedItem</div>";
    private readonly string example5BitMenuButtonItemCSharpCode = @"
private string example5SelectedItem;

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
};";
}
