namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private string? exampleSelectedItem;

    private BitMenuButtonItem? changedSelectedItem;
    private BitMenuButtonItem twoWaySelectedItem = default!;

    private List<BitMenuButtonItem> basicItems = new()
    {
        new() { Text = "Item A", Key = "A" },
        new() { Text = "Item B", Key = "B" },
        new() { Text = "Item C", Key = "C" }
    };

    private List<BitMenuButtonItem> basicItemsIcon = new()
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

    private List<BitMenuButtonItem> isSelectedItems = new()
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
            IconName = BitIconName.Emoji2,
            IsSelected = true
        }
    };

    private List<BitMenuButtonItem> rtlItemsIcon = new()
    {
        new()
        {
            Text = "گزینه الف",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "گزینه ب",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new()
        {
            Text = "گزینه ج",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };

    protected override void OnInitialized()
    {
        twoWaySelectedItem = basicItems[2];

        Action<BitMenuButtonItem> onClick = item =>
        {
            exampleSelectedItem = $"{item.Text} - OnClick";
            StateHasChanged();
        };

        basicItemsOnClick.ForEach(i => i.OnClick = onClick);
    }



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Fill""
                Items=""basicItems""
                Variant=""BitVariant.Fill"" />

<BitMenuButton Text=""Outline""
                Items=""basicItems""
                Variant=""BitVariant.Outline"" />

<BitMenuButton Text=""Text""
                Items=""basicItems""
                Variant=""BitVariant.Text"" />";
    private readonly string example2CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Items"" Items=""basicItems"" />

<BitMenuButton Text=""Items"" IsEnabled=""false"" Items=""basicItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Items""
               Items=""basicItems""
               Variant=""BitVariant.Outline"" />

<BitMenuButton Text=""Items""
               IsEnabled=""false""
               Items=""basicItems""
               Variant=""BitVariant.Outline"" />";
    private readonly string example4CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""Items""
               Items=""basicItems""
               Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Items""
               IsEnabled=""false""
               Items=""basicItems""
               Variant=""BitVariant.Text"" />";
    private readonly string example5CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example6RazorCode = @"
<BitMenuButton Split
               Text=""Fill""
               Items=""basicItems"" />

<BitMenuButton Split
               Text=""Outline""
               Items=""basicItems""
               Variant=""BitVariant.Outline"" />

<BitMenuButton Split
               Text=""Text""
               Items=""basicItems""
               Variant=""BitVariant.Text"" />";
    private readonly string example6CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example7RazorCode = @"
<BitMenuButton Sticky Items=""basicItems"" />

<BitMenuButton Split Sticky
               Items=""basicItems""
               Variant=""BitVariant.Outline"" />";
    private readonly string example7CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};";

    private readonly string example8RazorCode = @"
<BitMenuButton Text=""IconName""
               Items=""basicItemsIcon""
               IconName=""@BitIconName.Edit"" />

<BitMenuButton Split
               Text=""ChevronDownIcon""
               Items=""basicItemsIcon""
               IconName=""@BitIconName.Add""
               Variant=""BitVariant.Outline""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown"" />";
    private readonly string example8CsharpCode = @"
private List<BitMenuButtonItem> basicItemsIcon = new()
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

    private readonly string example9RazorCode = @"
<BitMenuButton Text=""Items""
               Items=""itemDisabledItems""
               OnClick=""(BitMenuButtonItem item) => example2SelectedItem = item?.Key"" />

<BitMenuButton Split
               Text=""Items""
               Items=""basicItemsOnClick""
               Variant=""BitVariant.Outline""
               OnClick=""@((BitMenuButtonItem item) => example2SelectedItem = ""Main button clicked"")"" />


<BitMenuButton Sticky
               Items=""basicItemsOnClick""
               OnClick=""(BitMenuButtonItem item) => example2SelectedItem = item?.Key"" />

<BitMenuButton Split Sticky
               Items=""itemDisabledItems""
               Variant=""BitVariant.Outline""
               OnClick=""(BitMenuButtonItem item) => example2SelectedItem = item?.Key"" />

<div class=""clicked-item"">Clicked item: @example2SelectedItem</div>";
    private readonly string example9CsharpCode = @"
private string? exampleSelectedItem;

private List<BitMenuButtonItem> itemDisabledItems = new()
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
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
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
        example2SelectedItem = $""{item.Text} - OnClick"";
        StateHasChanged();
    };

    basicItemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example10RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicItems"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Split
               Text=""Items""
               Items=""itemTemplateItems""
               Variant=""BitVariant.Outline"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Items"" Items=""itemTemplateItems2"" />";
    private readonly string example10CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};

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
               Items=""basicItems""
               Style=""width: 200px; height: 40px;"" />

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
               Variant=""BitVariant.Outline""
               Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"" />";
    private readonly string example11CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
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

    private readonly string example12RazorCode = @"
<BitMenuButton Split Sticky
               Items=""basicItems""
               DefaultSelectedItem=""basicItems[1]"" />


<BitMenuButton Sticky
               Items=""basicItems""
               @bind-SelectedItem=""twoWaySelectedItem""
               Variant=""BitVariant.Outline"" />

<div>Selected item: <b>@twoWaySelectedItem.Text</b></div>


<BitMenuButton Split Sticky
               Items=""basicItems""
               OnChange=""(BitMenuButtonItem item) => changedSelectedItem = item"" />

<div>Changed item: <b>@changedSelectedItem?.Text</b></div>


<BitMenuButton Sticky
               Items=""isSelectedItems""
               Variant=""BitVariant.Outline"" />";
    private readonly string example12CsharpCode = @"
private BitMenuButtonItem? changedSelectedItem;
private BitMenuButtonItem twoWaySelectedItem = default!;

private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"" },
    new() { Text = ""Item C"", Key = ""C"" }
};

private List<BitMenuButtonItem> isSelectedItems = new()
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
        IconName = BitIconName.Emoji2,
        IsSelected = true
    }
};

protected override void OnInitialized()
{
    twoWaySelectedItem = basicItems[2];
}";

    private readonly string example13RazorCode = @"
<BitMenuButton Text=""گزینه ها""
               Dir=""BitDir.Rtl""
               Items=""rtlItemsIcon""
               IconName=""@BitIconName.Edit"" />

<BitMenuButton Split
               Text=""گزینه ها""
               Dir=""BitDir.Rtl""
               Items=""rtlItemsIcon""
               IconName=""@BitIconName.Add""
               Variant=""BitVariant.Outline""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown"" />";
    private readonly string example13CsharpCode = @"
 private List<BitMenuButtonItem> rtlItemsIcon = new()
 {
    new()
    {
        Text = ""گزینه الف"",
        Key = ""A"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""گزینه ب"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new()
    {
        Text = ""گزینه ج"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
 };";
}
