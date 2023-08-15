namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonItemDemo
{
    private string? example1SelectedItem;
    private string? example2SelectedItem;
    private string? example3SelectedItem;
    private string? example4SelectedItem;
    private string? example51SelectedItem;
    private string? example52SelectedItem;
    private string? example53SelectedItem;
    private string? example61SelectedItem;
    private string? example62SelectedItem;


    private List<BitSplitButtonItem> basicItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
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
    private List<BitSplitButtonItem> isStickyItems = new()
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
    private List<BitSplitButtonItem> disabledItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji2
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Text = "Item D",
            Key = "D",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> itemStyleClassItems = new()
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
    private List<BitSplitButtonItem> itemTemplateItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key"
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key"
        },
        new()
        {
            Text = "Delete",
            Key = "delete-key"
        }
    };
    private List<BitSplitButtonItem> itemsOnClick = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
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
        Action<BitSplitButtonItem> onClick = item =>
        {
            example4SelectedItem = $"{item.Text} - OnClick";
            StateHasChanged();
        };

        itemsOnClick.ForEach(i => i.OnClick = onClick);
    }



    private readonly string example1HTMLCode = @"
<BitSplitButton Items=""BasicItems"" OnClick=""(BitSplitButtonItem item) => example1SelectedItem = item.Text"" />
    
<BitSplitButton Items=""BasicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example1SelectedItem = item.Text"" />

<BitSplitButton Items=""BasicItems"" IsEnabled=""false"" />

<div>Clicked item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
private string? example1SelectedItem;

private List<BitSplitButtonItem> BasicItems = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
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

    private readonly string example2HTMLCode = @"
<BitSplitButton IsSticky=""true""
                Items=""IsStickyItems""
                OnClick=""(BitSplitButtonItem item) => example2SelectedItem = item.Text"" />
        
<BitSplitButton IsSticky=""true""
                Items=""IsStickyItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example2SelectedItem = item.Text"" />

<div>Clicked item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string? example2SelectedItem;

private List<BitSplitButtonItem> IsStickyItems = new()
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

    private readonly string example3HTMLCode = @"
<BitSplitButton IsSticky=""true""
                Items=""DisabledItems""
                OnClick=""(BitSplitButtonItem item) => example3SelectedItem = item.Text"" />
        
<BitSplitButton Items=""DisabledItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example3SelectedItem = item.Text"" />

<div>Clicked item: @example3SelectedItem</div>";
    private readonly string example3CSharpCode = @"
private string? example3SelectedItem;

private List<BitSplitButtonItem> DisabledItems = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji2
    },
    new()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Text = ""Item D"",
        Key = ""D"",
        IconName = BitIconName.Emoji2
    }
};";

    private readonly string example4HTMLCode = @"
<BitSplitButton Items=""itemsOnClick"" />

<BitSplitButton Items=""itemsOnClick"" ButtonStyle=""BitButtonStyle.Standard"" IsSticky=""true"" />

<div>Clicked item: @example4SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string? example4SelectedItem;

private List<BitSplitButtonItem> itemsOnClick = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
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
    Action<BitSplitButtonItem> onClick = item =>
    {
        example4SelectedItem = $""{item.Text} - OnClick"";
        StateHasChanged();
    };

    itemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example5HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        font-size: 18px;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-chevron {
        background-color: aquamarine;
    }

    .custom-button {
        background-color: brown;
    }
</style>

<BitSplitButton Items=""basicItems"" 
                Style=""width:200px;height:40px;""
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8""
                OnClick=""(BitSplitButtonItem item) => example51SelectedItem = item.Text"" />

<BitSplitButton Items=""basicItems""
                Class=""custom-class"" 
                OnClick=""(BitSplitButtonItem item) => example51SelectedItem = item.Text"" />

<div>Clicked item: @example51SelectedItem</div>


<BitSplitButton IsSticky=""true""
                Items=""itemStyleClassItems""
                OnClick=""(BitSplitButtonItem item) => example52SelectedItem = item.Text"" />

<div>Clicked Item: @example52SelectedItem</div>


<BitSplitButton Items=""basicItems""
                OnClick=""(BitSplitButtonItem item) => example53SelectedItem = item.Text"" 
                Styles=""@(new() { ChevronDownButton=""background-color:red"", 
                                  ChevronDownIcon=""color:darkblue"", 
                                  ItemButton=""background:darkgoldenrod"" })"" />

<BitSplitButton Items=""basicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example53SelectedItem = item.Text""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"",
                                   ItemButton=""custom-button"" })"" />

<div>Clicked Item: @example53SelectedItem</div>";
    private readonly string example5CSharpCode = @"
private string example51SelectedItem;
private string example52SelectedItem;
private string example53SelectedItem;

private List<BitSplitButtonItem> basicItems = new()
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

private List<BitSplitButtonItem> itemStyleClassItems = new()
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

    private readonly string example6HTMLCode = @"
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitSplitButton IsSticky=""true""
                Items=""itemTemplateItems""
                OnClick=""(BitSplitButtonItem item) => example61SelectedItem = item.Text"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitSplitButton>
        
<BitSplitButton IsSticky=""true""
                Items=""itemTemplateItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example61SelectedItem = item.Text"">
    <ItemTemplate Context=""item"">
        @if (item.Key == ""add-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Add"" />
                <span style=""color: green;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""edit-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Edit"" />
                <span style=""color: yellow;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""delete-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Delete"" />
                <span style=""color: red;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
    </ItemTemplate>
</BitSplitButton>

<div>Clicked item: @example61SelectedItem</div>


<BitSplitButton IsSticky=""true""
                Items=""itemTemplateItems2""
                OnClick=""(BitSplitButtonItem item) => example62SelectedItem = item.Text"" />

<div>Clicked Item: @example62SelectedItem</div>";
    private readonly string example6CSharpCode = @"
private string? example61SelectedItem;
private string? example62SelectedItem;

private List<BitSplitButtonItem> itemTemplateItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key""
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key""
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key""
    }
};

private List<BitSplitButtonItem> itemTemplateItems2 = new()
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
