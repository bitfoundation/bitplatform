namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonCustomDemo
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


    private List<SplitActionItem> basicCustomItems = new()
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
    private List<SplitActionItem> itemStyleClassCustoms = new()
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
    private List<SplitActionItem> isStickyCustomItems = new()
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
            Name = "Custom A",
            Id = "A",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Custom B",
            Id = "B",
            Icon = BitIconName.Emoji2
        },
        new()
        {
            Name = "Custom C",
            Id = "C",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Custom D",
            Id = "D",
            Icon = BitIconName.Emoji2
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
    private List<SplitActionItem> itemsOnClick = new()
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

    protected override void OnInitialized()
    {
        Action<SplitActionItem> onClick = item =>
        {
            example4SelectedItem = $"{item.Name} - Clicked";
            StateHasChanged();
        };

        itemsOnClick.ForEach(i => i.Clicked = onClick);
    }



    private readonly string example1HTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""basicCustomItems""
                OnClick=""(SplitActionItem item) => example1SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example1SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<BitLabel>Disabled</BitLabel>
<BitSplitButton IsEnabled=""false""
                Items=""basicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />

<div>Clicked item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
private string example1SelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> basicCustomItems = new()
{
    new()
    {
        Name = ""Custom A"",
        Id = ""A""
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

    private readonly string example2HTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""isStickyCustomItems""
                OnClick=""(SplitActionItem item) => example2SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""isStickyCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example2SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<div>Clicked item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> isStickyCustomItems = new()
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

    private readonly string example3HTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""disabledCustomItems""
                OnClick=""(SplitActionItem item) => example3SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton Items=""disabledCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example3SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<div>Clicked item: @example3SelectedItem</div>";
    private readonly string example3CSharpCode = @"
private string example3SelectedItem;

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
        Name = ""Custom A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Name = ""Custom B"",
        Id = ""B"",
        Icon = BitIconName.Emoji2
    },
    new()
    {
        Name = ""Custom C"",
        Id = ""C"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Name = ""Custom D"",
        Id = ""D"",
        Icon = BitIconName.Emoji2
    }
};";

    private readonly string example4HTMLCode = @"
<BitSplitButton Items=""itemsOnClick""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) },
                                         OnClick = { Name = nameof(SplitActionItem.Clicked) } })"" />

<BitSplitButton IsSticky=""true""
                Items=""itemsOnClick""
                ButtonStyle=""BitButtonStyle.Standard""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name },
                                         OnClick = { Selector = item => item.Clicked } })"" />

<div>Clicked item: @example4SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string? example4SelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Action<SplitActionItem>? Clicked { get; set; }
}

private List<SplitActionItem> itemsOnClick = new()
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
};

protected override void OnInitialized()
{
    Action<SplitActionItem> onClick = item =>
    {
        example4SelectedItem = $""{item.Text} - Clicked"";
        StateHasChanged();
    };

    itemsOnClick.ForEach(i => i.Clicked = onClick);
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

<BitSplitButton Items=""basicCustomItems""
                Style=""width:200px;height:40px;""
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8""
                OnClick=""(SplitActionItem item) => example51SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })"" />

<BitSplitButton Items=""basicCustomItems""
                Class=""custom-class""
                OnClick=""(SplitActionItem item) => example51SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })"" />

<div>Clicked item: @example51SelectedItem</div>


<BitSplitButton IsSticky=""true"" 
                Items=""itemStyleClassCustoms""
                OnClick=""(SplitActionItem item) => example52SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) },
                                         Class = { Name = nameof(SplitActionItem.Class) },
                                         Style = { Name = nameof(SplitActionItem.Style) } })"" />

<div>Clicked Item: @example52SelectedItem</div>


<BitSplitButton Items=""basicCustomItems""
                OnClick=""(SplitActionItem item) => example53SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) } })""
                Styles=""@(new() { ChevronDownButton=""background-color:red"",
                                  ChevronDownIcon=""color:darkblue"",
                                  ItemButton=""background:darkgoldenrod"" })"" />

<BitSplitButton Items=""basicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example53SelectedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name } })""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"", ItemButton=""custom-button"" })"" />
        
<div>Clicked Item: @example53SelectedItem</div>";
    private readonly string example5CSharpCode = @"
private string example51SelectedItem;
private string example52SelectedItem;
private string example53SelectedItem;

public class SplitActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<SplitActionItem> basicCustoms = new()
{
    new()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<SplitActionItem> itemStyleClassCustoms = new()
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

    private readonly string example6HTMLCode = @"
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms""
                OnClick=""(SplitActionItem item) => example61SelectedItem = item.Name""
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
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => example61SelectedItem = item.Name""
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

<div>Clicked item: @example61SelectedItem</div>


<BitSplitButton IsSticky=""true""
                Items=""itemTemplateCustoms2""
                OnClick=""(SplitActionItem item) => example62SelectedItem = item.Name""
                NameSelectors=""@(new() { Text = { Name = nameof(SplitActionItem.Name) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Template = { Name = nameof(SplitActionItem.Fragment)} })"" />

<div class=""clicked-item"">Clicked Item: @example62SelectedItem</div>";
    private readonly string example6CSharpCode = @"
private string example61SelectedItem;
private string example62SelectedItem;

public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
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
}
