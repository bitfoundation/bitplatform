namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonItemDemo
{
    private BitSplitButtonItem? example41SelectedItem;
    private BitSplitButtonItem? example42SelectedItem;

    private BitSplitButtonItem twoWaySelectedItem = default!;
    private BitSplitButtonItem? changedSelectedItem;


    private List<BitSplitButtonItem> basicItems = new()
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
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new()
        {
            Text = "Edit",
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
    private List<BitSplitButtonItem> itemsOnClick = new()
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
    private List<BitSplitButtonItem> itemStyleClassItems = new()
    {
        new()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add,
            Style = "color:red"
        },
        new()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit,
            Class = "custom-item"
        },
        new()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete,
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
    private List<BitSplitButtonItem> isSelectedItems = new()
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
            IconName = BitIconName.Delete,
            IsSelected = true
        }
    };

    protected override void OnInitialized()
    {
        twoWaySelectedItem = basicItems[2];

        Action<BitSplitButtonItem> onClick = item =>
        {
            example42SelectedItem = item;
            StateHasChanged();
        };

        itemsOnClick.ForEach(i => i.OnClick = onClick);
    }



    private readonly string example1HtmlCode = @"
<BitSplitButton Items=""basicItems"" />
    
<BitSplitButton Items=""basicItems"" ButtonStyle=""BitButtonStyle.Standard"" />

<BitSplitButton Items=""basicItems"" IsEnabled=""false"" />";
    private readonly string example1CsharpCode = @"
private List<BitSplitButtonItem> basicItems = new()
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

    private readonly string example2HtmlCode = @"
<BitSplitButton IsSticky=""true"" Items=""basicItems"" />
        
<BitSplitButton IsSticky=""true"" Items=""basicItems"" ButtonStyle=""BitButtonStyle.Standard"" />";
    private readonly string example2CsharpCode = @"
private List<BitSplitButtonItem> basicItems = new()
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

    private readonly string example3HtmlCode = @"
<BitSplitButton Items=""disabledItems"" IsSticky=""true"" />
        
<BitSplitButton Items=""disabledItems"" ButtonStyle=""BitButtonStyle.Standard"" />";
    private readonly string example3CsharpCode = @"
private List<BitSplitButtonItem> disabledItems = new()
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

    private readonly string example4HtmlCode = @"
<BitSplitButton IsSticky=""true""
                Items=""basicItems""
                OnClick=""(BitSplitButtonItem item) => example41SelectedItem = item"" />

<BitSplitButton Items=""basicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => example41SelectedItem = item"" />

<div>Clicked item: @example41SelectedItem?.Text</div>


<BitSplitButton Items=""itemsOnClick"" IsSticky=""true"" />

<BitSplitButton Items=""itemsOnClick"" ButtonStyle=""BitButtonStyle.Standard"" />

<div>Clicked item: @example42SelectedItem?.Text</div>";
    private readonly string example4CsharpCode = @"
private BitSplitButtonItem? example41SelectedItem;
private BitSplitButtonItem? example42SelectedItem;

private List<BitSplitButtonItem> basicItems = new()
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
};

private List<BitSplitButtonItem> itemsOnClick = new()
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
};

protected override void OnInitialized()
{
    Action<BitSplitButtonItem> onClick = item =>
    {
        example42SelectedItem = item;
        StateHasChanged();
    };

    itemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example5HtmlCode = @"
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
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8"" />

<BitSplitButton Items=""basicItems"" 
                Class=""custom-class""
                ButtonStyle=""BitButtonStyle.Standard""  />


<BitSplitButton IsSticky=""true"" Items=""itemStyleClassItems"" />


<BitSplitButton Items=""basicItems""
                Styles=""@(new() { ChevronDownButton=""background-color:red"", 
                                  ChevronDownIcon=""color:darkblue"", 
                                  ItemButton=""background:darkgoldenrod"" })"" />

<BitSplitButton Items=""basicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"", ItemButton=""custom-button"" })"" />";
    private readonly string example5CsharpCode = @"
private List<BitSplitButtonItem> basicItems = new()
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
};

private List<BitSplitButtonItem> itemStyleClassItems = new()
{
    new()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add,
        Style = ""color:red""
    },
    new()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit,
        Class = ""custom-item""
    },
    new()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete,
        Style = ""background:blue""
    }
};";

    private readonly string example6HtmlCode = @"
Visible: [ <BitSplitButton Visibility=""BitVisibility.Visible"" Items=""basicItems"" /> ]

Hidden: [ <BitSplitButton Visibility=""BitVisibility.Hidden"" Items=""basicItems"" /> ]

Collapsed: [ <BitSplitButton Visibility=""BitVisibility.Collapsed"" Items=""basicItems"" /> ]";
    private readonly string example6CsharpCode = @"
private List<BitSplitButtonItem> basicItems = new()
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

    private readonly string example7HtmlCode = @"
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitSplitButton IsSticky=""true"" Items=""itemTemplateItems"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
</BitSplitButton>
        
<BitSplitButton IsSticky=""true"" Items=""itemTemplateItems"" ButtonStyle=""BitButtonStyle.Standard"">
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


<BitSplitButton IsSticky=""true"" Items=""itemTemplateItems2"" />";
    private readonly string example7CsharpCode = @"
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

    private readonly string example8HtmlCode = @"
<BitSplitButton Items=""basicItems"" DefaultSelectedItem=""basicItems[1]"" />

      
<BitSplitButton IsSticky=""true"" Items=""basicItems"" @bind-SelectedItem=""twoWaySelectedItem"" ButtonStyle=""BitButtonStyle.Standard"" />
<div>Selected item: <b>@twoWaySelectedItem.Text</b></div>


<BitSplitButton IsSticky=""true"" Items=""basicItems"" OnChange=""(BitSplitButtonItem item) => changedSelectedItem = item"" />
<div>Changed item: <b>@changedSelectedItem?.Text</b></div>


<BitSplitButton Items=""isSelectedItems"" />";
    private readonly string example8CsharpCode = @"
private BitSplitButtonItem twoWaySelectedItem = default!;
private BitSplitButtonItem? changedSelectedItem;

protected override void OnInitialized()
{
    twoWaySelectedItem = basicItems[2];
}

private List<BitSplitButtonItem> basicItems = new()
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
};

private List<BitSplitButtonItem> isSelectedItems = new()
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
        IconName = BitIconName.Delete,
        IsSelected = true
    }
};";
}
