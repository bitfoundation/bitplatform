namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private string? example1SelectedItem;
    private string? example2SelectedItem;
    private string? example3SelectedItem;
    private string? example41SelectedItem;
    private string? example42SelectedItem;
    private string? example43SelectedItem;
    private string? example5SelectedItem;
    private string? example61SelectedItem;
    private string? example62SelectedItem;


    private List<MenuActionItem> basicCustoms = new()
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

    protected override void OnInitialized()
    {
        Action<MenuActionItem> onClick = item =>
        {
            example2SelectedItem = $"{item.Name} - Clicked";
            StateHasChanged();
        };

        basicCustomsOnClick.ForEach(i => i.Clicked = onClick);
    }



    private readonly string example1HTMLCode = @"
<BitMenuButton Text=""Primary""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Disabled"" Items=""basicCustoms"" IsEnabled=""false"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
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

    private readonly string example2HTMLCode = @"
<BitMenuButton Text=""Custom Disabled""
               Items=""itemDisabledCustoms""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

<BitMenuButton Text=""Custom OnClick""
               Items=""basicCustomsOnClick""
               ButtonStyle=""BitButtonStyle.Standard""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon },
                                        OnClick = { Selector = item => item.Clicked } })"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;

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
};
protected override void OnInitialized()
{
    Action<MenuActionItem> onClick = item =>
    {
        example2SelectedItem = $""{item.Name} - Clicked"";
        StateHasChanged();
    };

    basicCustomsOnClick.ForEach(i => i.Clicked = onClick);
}";

    private readonly string example3HTMLCode = @"
<BitMenuButton Text=""IconName""
               Items=""basicCustoms""
               IconName=""@BitIconName.Edit""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example3SelectedItem = item.Id"" />

<BitMenuButton Text=""ChevronDownIcon""
               Items=""basicCustoms""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example3SelectedItem = item.Id"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example3CSharpCode = @"
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

    private readonly string example4HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
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
               Style=""width:200px;height:40px;background-color:#888;"" 
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })""
               OnItemClick=""(MenuActionItem item) => example31SelectedItem = item.Id"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicCustoms""
               Class=""custom-class"" 
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example31SelectedItem = item.Id"" />

<div>Clicked Item: @example31SelectedItem</div>


<BitMenuButton Text=""Custom Styled & Classed Button""
               Items=""itemStyleClassCustoms""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) },
                                        Class = { Name = nameof(MenuActionItem.Class) },
                                        Style = { Name = nameof(MenuActionItem.Style) } })""
               OnItemClick=""(MenuActionItem item) => example32SelectedItem = item.Id"" />

<div>Clicked Item: @example32SelectedItem</div>


<BitMenuButton Text=""Styles""
                Items=""basicCustoms""
                IconName=""@BitIconName.ExpandMenu""
                OnItemClick=""(MenuActionItem item) => example33SelectedItem = item.Id""
                NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                         Key = { Name = nameof(MenuActionItem.Id) },
                                         IconName = { Name = nameof(MenuActionItem.Icon) } })"" 
                Styles=""@(new() { Icon = ""color:red"" ,
                                  Text = ""color:aqua"",
                                  ItemText = ""color:dodgerblue;font-size:11px"",
                                  Overlay = ""background-color: var(--bit-clr-bg-overlay)"" })"" />

<BitMenuButton Text=""Classes""
                Items=""basicCustoms""
                IconName=""@BitIconName.ExpandMenu""
                ButtonStyle=""BitButtonStyle.Standard""
                OnItemClick=""(MenuActionItem item) => example33SelectedItem = item.Id""
                NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                         Key = { Selector = item => item.Id },
                                         IconName = { Selector = item => item.Icon } })"" 
                Classes=""@(new() { Icon = ""custom-icon"" , Text = ""custom-text"" })"" />

<div>Clicked Item: @example33SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string example31SelectedItem;
private string example32SelectedItem;
private string example33SelectedItem;

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


    private readonly string example5HTMLCode = @"
<BitMenuButton Items=""basicCustoms""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example4SelectedItem = item.Id"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Items=""basicCustoms""
               ButtonStyle=""BitButtonStyle.Standard""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })""
               OnItemClick=""(MenuActionItem item) => example4SelectedItem = item.Id"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example5CSharpCode = @"
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

    private readonly string example6HTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateCustoms""
               NameSelectors=""@(new() { Text = { Selector = item => item.Name },
                                        Key = { Selector = item => item.Id },
                                        IconName = { Selector = item => item.Icon } })""
               OnItemClick=""(MenuActionItem item) => example51SelectedItem = item.Id"">
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
               ButtonStyle=""BitButtonStyle.Standard""
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) } })""
               OnItemClick=""(MenuActionItem item) => example51SelectedItem = item.Id"">
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
               NameSelectors=""@(new() { Text = { Name = nameof(MenuActionItem.Name) },
                                        Key = { Name = nameof(MenuActionItem.Id) },
                                        IconName = { Name = nameof(MenuActionItem.Icon) },
                                        Template = { Name = nameof(MenuActionItem.Fragment)} })""
               OnItemClick=""(MenuActionItem item) => example52SelectedItem = item.Id"" />

<div>Clicked Item: @example52SelectedItem</div>";
    private readonly string example6CSharpCode = @"
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
}
