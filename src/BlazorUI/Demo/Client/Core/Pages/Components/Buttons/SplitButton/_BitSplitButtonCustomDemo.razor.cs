namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonCustomDemo
{
    private SplitActionItem? example41SelectedItem;
    private SplitActionItem? example42SelectedItem;

    private SplitActionItem twoWaySelectedItem = default!;
    private SplitActionItem? changedSelectedItem;


    private List<SplitActionItem> basicCustomItems = new()
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
    private List<SplitActionItem> disabledCustomItems = new()
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
            Icon = BitIconName.Edit,
            IsEnabled = false,
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };
    private List<SplitActionItem> itemsOnClick = new()
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
    private List<SplitActionItem> itemStyleClassCustoms = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add,
            Style = "color:red"
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit,
            Class = "custom-item"
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete,
            Style = "background:blue"
        }
    };
    private List<SplitActionItem> itemTemplateCustoms = new()
    {
        new()
        {
            Name = "Add",
            Id = "add-key"
        },
        new()
        {
            Name = "Edit",
            Id = "edit-key"
        },
        new()
        {
            Name = "Delete",
            Id = "delete-key"
        }
    };
    private List<SplitActionItem> isSelectedCustomItems = new()
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
            Icon = BitIconName.Delete,
            IsCurrent = true
        }
    };

    protected override void OnInitialized()
    {
        twoWaySelectedItem = basicCustomItems[2];

        Action<SplitActionItem> onClick = item =>
        {
            example42SelectedItem = item;
            StateHasChanged();
        };

        itemsOnClick.ForEach(i => i.Clicked = onClick);
    }



    private readonly string example1RazorCode = @"
<BitSplitButton Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitSplitButton Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<BitSplitButton IsEnabled=""false""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />";
    private readonly string example1CsharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
}

private List<SplitActionItem> basicCustomItems = new()
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
<BitSplitButton IsSticky=""true""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitSplitButton IsSticky=""true""
                Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />";
    private readonly string example2CsharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
}

private List<SplitActionItem> basicCustomItems = new()
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

    private readonly string example3RazorCode = @"
<BitSplitButton IsSticky=""true""
                Items=""disabledCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitSplitButton Items=""disabledCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />";
    private readonly string example3CsharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> disabledCustomItems = new()
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
        Icon = BitIconName.Edit,
        IsEnabled = false,
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};";

    private readonly string example4RazorCode = @"
<BitSplitButton IsSticky=""true""
                Items=""basicCustomItems""
                OnClick=""(SplitActionItem item) => example41SelectedItem = item""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })"" />

<BitSplitButton Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example41SelectedItem = item""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })"" />
<div>Clicked item: <b>@example41SelectedItem?.Name</b></div>


<BitSplitButton IsSticky=""true""
                Items=""itemsOnClick""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) },
                                         OnClick = { Name = nameof(SplitActionItem.Clicked) } })"" />

<BitSplitButton Items=""itemsOnClick""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name },
                                         OnClick = { Selector = item => item.Clicked } })"" />

<div>Clicked item: @example42SelectedItem?.Name</div>";
    private readonly string example4CsharpCode = @"
private SplitActionItem? example41SelectedItem;
private SplitActionItem? example42SelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public Action<SplitActionItem>? Clicked { get; set; }
}

private List<SplitActionItem> itemsOnClick = new()
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

protected override void OnInitialized()
{
    Action<SplitActionItem> onClick = item =>
    {
        example42SelectedItem = item;
        StateHasChanged();
    };

    itemsOnClick.ForEach(i => i.Clicked = onClick);
}";

    private readonly string example5RazorCode = @"
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

<BitSplitButton Items=""basicCustomItems""
                Style=""width:200px;height:40px;""
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })"" />

<BitSplitButton Items=""basicCustomItems""
                Class=""custom-class""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })"" />



<BitSplitButton IsSticky=""true"" 
                Items=""itemStyleClassCustoms""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) },
                                         Class = { Name = nameof(SplitActionItem.Class) },
                                         Style = { Name = nameof(SplitActionItem.Style) } })"" />



<BitSplitButton Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })""
                Styles=""@(new() { ChevronDownButton=""background-color:red"",
                                  ChevronDownIcon=""color:darkblue"",
                                  ItemButton=""background:darkgoldenrod"" })"" />

<BitSplitButton Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"", ItemButton=""custom-button"" })"" />";
    private readonly string example5CsharpCode = @"
public class SplitActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<SplitActionItem> basicCustomItems = new()
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

private List<SplitActionItem> itemStyleClassCustoms = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add,
        Style = ""color:red""
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit,
        Class = ""custom-item""
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete,
        Style = ""background:blue""
    }
};";

    private readonly string example6RazorCode = @"
Visible: [ <BitSplitButton Visibility=""BitVisibility.Visible""
                           Items=""basicCustomItems""
                           NameSelectors=""@(new() { Text = { Name = nameof(SplitActionItem.Name) },
                                                    Key = { Name = nameof(SplitActionItem.Id) },
                                                    IconName = { Name = nameof(SplitActionItem.Icon) } })"" /> ]

Hidden: [ <BitSplitButton Visibility=""BitVisibility.Hidden""
                          Items=""basicCustomItems""
                          NameSelectors=""@(new() { Text = { Name = nameof(SplitActionItem.Name) },
                                                   Key = { Name = nameof(SplitActionItem.Id) },
                                                   IconName = { Name = nameof(SplitActionItem.Icon) } })"" /> ]

Collapsed: [ <BitSplitButton Visibility=""BitVisibility.Collapsed""
                             Items=""basicCustomItems""
                             NameSelectors=""@(new() { Text = { Name = nameof(SplitActionItem.Name) },
                                                      Key = { Name = nameof(SplitActionItem.Id) },
                                                      IconName = { Name = nameof(SplitActionItem.Icon) } })"" /> ]";
    private readonly string example6CsharpCode = @"
public class SplitActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

private List<SplitActionItem> basicCustomItems = new()
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
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Name (@item.Id)
            </span>
        </div>
    </ItemTemplate>
</BitSplitButton>
        
<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"">
    <ItemTemplate Context=""item"">
        @if (item.Id == ""add-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Add"" />
                <span style=""color: green;"">
                    @item.Name (@item.Id)
                </span>
            </div>
        }
        else if (item.Id == ""edit-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Edit"" />
                <span style=""color: yellow;"">
                    @item.Name (@item.Id)
                </span>
            </div>
        }
        else if (item.Id == ""delete-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Delete"" />
                <span style=""color: red;"">
                    @item.Name (@item.Id)
                </span>
            </div>
        }
    </ItemTemplate>
</BitSplitButton>



<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms2""
                NameSelectors=""@(new() { Text = { Name = nameof(SplitActionItem.Name) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Template = { Name = nameof(SplitActionItem.Fragment)} })"" />";
    private readonly string example7CsharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public RenderFragment<SplitActionItem>? Fragment { get; set; }
}

private List<SplitActionItem> itemTemplateCustoms = new()
{
    new()
    {
        Name = ""Add"",
        Id = ""add-key""
    },
    new()
    {
        Name = ""Edit"",
        Id = ""edit-key""
    },
    new()
    {
        Name = ""Delete"",
        Id = ""delete-key""
    }
};

private List<SplitActionItem> itemTemplateCustoms2 = new()
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

    private readonly string example8RazorCode = @"
<BitSplitButton Items=""basicCustomItems""
                DefaultSelectedItem=""basicCustomItems[1]""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })"" />
      
<BitSplitButton @bind-SelectedItem=""twoWaySelectedItem""
                IsSticky=""true""
                Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })"" />
<div>Selected item: <b>@twoWaySelectedItem.Name</b></div>

<BitSplitButton IsSticky=""true"" 
                Items=""basicCustomItems""
                OnChange=""(SplitActionItem item) => changedSelectedItem = item""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })"" />
<div>Selected item: <b>@changedSelectedItem?.Name</b></div>

<BitSplitButton Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         IsSelected = { Selector = item => item.IsCurrent },
                                         Text = { Selector = item => item.Name } })"" />

<div>Selected item: <b>@twoWaySelectedItem.Name</b></div>";
    private readonly string example8CsharpCode = @"
private SplitActionItem twoWaySelectedItem = default!;
private SplitActionItem? changedSelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsCurrent { get; set; }
}

protected override void OnInitialized()
{
    twoWaySelectedItem = basicCustomItems[2];
}

private List<SplitActionItem> basicCustomItems = new()
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

private List<SplitActionItem> isSelectedCustomItems = new()
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
        Icon = BitIconName.Delete,
        IsCurrent = true
    }
};";
}
