using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new()
        {
            Name = "ButtonSize",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "BitButtonSize.Medium",
            Description = "The size of button, Possible values: Small | Medium | Large.",
        },
        new()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
        },
        new()
        {
            Name = "ButtonType",
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitMenuButton, that are BitMenuButtonOption components.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content inside the header of MenuButton can be customized.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon to show inside the header of MenuButton.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            LinkType = LinkType.Link,
            Href = "#menu-button-items",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the MenuButton."
        },
        new()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is enabled.",
        },
        new()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            DefaultValue = "null",
            Description = "Whether or not the item is enabled.",
        },
        new()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The content inside the item can be customized.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback is called when the MenuButton header is clicked."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "OnClick of each item returns that item with its property."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show inside the header of MenuButton."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "A unique value to use as a key of the item.",
        },
        new()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "A unique value to use as a key of the item.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
            Description = "BitMenuButtonItem is default type for item.",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        },
        new()
        {
            Id = "menu-button-options",
            Title = "BitMenuButtonOption",
            Description = "BitMenuButtonOption is a child component for BitMenuButton.",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-size-enum",
            Name = "BitButtonSize",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Small",
                    Description="The button size is small.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The button size is medium.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The button size is large.",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        }
    };



    private string example1SelectedItem;
    private string example2SelectedItem;
    private string example3SelectedItem;
    private string example4SelectedItem;
    private string example5SelectedItem;

    private List<BitMenuButtonItem> basicMenuButton = new()
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
    private List<BitMenuButtonItem> disabledItemMenuButton = new()
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
    private List<BitMenuButtonItem> itemTemplateMenuButton = new()
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

    private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private List<MenuActionItem> disabledItemMenuButtonWithCustomType = new()
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
    private List<MenuActionItem> itemTemplateMenuButtonWithCustomType = new()
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

    private void OnTabClick()
    {
        example1SelectedItem = string.Empty;
        example2SelectedItem = string.Empty;
        example3SelectedItem = string.Empty;
        example4SelectedItem = string.Empty;
        example5SelectedItem = string.Empty;
    }

    private readonly string example1BitMenuButtonItemHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitMenuButton Text=""Disabled""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key""
               IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
               Items=""disabledItemMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CustomItemHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitMenuButton Text=""Disabled""
               Items=""basicMenuButtonWithCustomType""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id""
               IsEnabled=""false"" />

<BitMenuButton Text=""Item Disabled""
               Items=""disabledItemMenuButtonWithCustomType""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1BitMenuButtonOptionHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Disabled""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key""
               IsEnabled=""false"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Item Disabled""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" IsEnabled=""false"" />
</BitMenuButton>

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1BitMenuButtonItemCSharpCode = @"
private string example1SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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

private List<BitMenuButtonItem> disabledItemMenuButton = new()
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
    private readonly string example1CustomItemCSharpCode = @"
private string example1SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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

private List<MenuActionItem> disabledItemMenuButtonWithCustomType = new()
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
";
    private readonly string example1BitMenuButtonOptionCSharpCode = @"
private string example1SelectedItem;
";

    private readonly string example2BitMenuButtonItemHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CustomItemHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               Items=""basicMenuButtonWithCustomType""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2BitMenuButtonOptionHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2BitMenuButtonItemCSharpCode = @"
private string example2SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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
";
    private readonly string example2CustomItemCSharpCode = @"
private string example2SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private readonly string example2BitMenuButtonOptionCSharpCode = @"
private string example2SelectedItem;";

    private readonly string example3BitMenuButtonItemHTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitMenuButton Text=""Styled Button""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
               Class=""custom-class"" />

<div>Clicked Item: @example3SelectedItem</div>";
    private readonly string example3CustomItemHTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitMenuButton Text=""Styled Button""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example3SelectedItem = item.Id""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

<BitMenuButton Text=""Classed Button""
               Items=""basicMenuButtonWithCustomType""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example3SelectedItem = item.Id""
               Class=""custom-class"" />

<div>Clicked Item: @example3SelectedItem</div>";
    private readonly string example3BitMenuButtonOptionHTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitMenuButton Text=""Styled Button""
               OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button""
               OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key""
               Class=""custom-class"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example3SelectedItem</div>";
    private readonly string example3BitMenuButtonItemCSharpCode = @"
private string example3SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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
";
    private readonly string example3CustomItemCSharpCode = @"
private string example3SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private readonly string example3BitMenuButtonOptionCSharpCode = @"
private string example3SelectedItem;
";

    private readonly string example4BitMenuButtonItemHTMLCode = @"
<BitMenuButton Items=""basicMenuButton""
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

<BitMenuButton Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key""
               ButtonStyle=""BitButtonStyle.Standard"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4CustomItemHTMLCode = @"
<BitMenuButton Items=""basicMenuButtonWithCustomType""
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

<BitMenuButton Items=""basicMenuButtonWithCustomType""
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
    private readonly string example4BitMenuButtonOptionHTMLCode = @"
<BitMenuButton ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
    </ChildContent>
</BitMenuButton>

<BitMenuButton ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
    </ChildContent>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4BitMenuButtonItemCSharpCode = @"
private string example4SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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
";
    private readonly string example4CustomItemCSharpCode = @"
private string example4SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private readonly string example4BitMenuButtonOptionCSharpCode = @"
private string example4SelectedItem;
";

    private readonly string example5BitMenuButtonItemHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateMenuButton""
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
               Items=""itemTemplateMenuButton""
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
    private readonly string example5CustomItemHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               Items=""itemTemplateMenuButtonWithCustomType""
               TextFieldSelector=""item => item.Name""
               KeyFieldSelector=""item => item.Id""
               IconNameFieldSelector=""item => item.Icon""
               OnItemClick=""(MenuActionItem item) => example5SelectedItem = item.Id""
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
               Items=""itemTemplateMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example5SelectedItem = item.Id""
               ButtonStyle=""BitButtonStyle.Standard"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Name (@item.Id)
            </span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<div>Clicked Item: @example5SelectedItem</div>";
    private readonly string example5BitMenuButtonOptionHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               ButtonStyle=""BitButtonStyle.Primary""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               ButtonStyle=""BitButtonStyle.Standard""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitMenuButton>

<div>Clicked Item: @example5SelectedItem</div>";
    private readonly string example5BitMenuButtonItemCSharpCode = @"
private string example5SelectedItem;

private List<BitMenuButtonItem> itemTemplateMenuButton = new()
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
";
    private readonly string example5CustomItemCSharpCode = @"
private string example5SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> itemTemplateMenuButtonWithCustomType = new()
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
";
    private readonly string example5BitMenuButtonOptionCSharpCode = @"
private string example5SelectedItem;
";

    private readonly string example6BitMenuButtonItemHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Small""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Medium""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Large""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />";
    private readonly string example6CustomItemHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Small""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Medium""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Large""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />";
    private readonly string example6BitMenuButtonOptionHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Small""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Medium""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               ButtonSize=""BitButtonSize.Large""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";
    private readonly string example6BitMenuButtonItemCSharpCode = @"
private string example1SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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
";
    private readonly string example6CustomItemCSharpCode = @"
private string example1SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private readonly string example6BitMenuButtonOptionCSharpCode = @"
private string example1SelectedItem;
";

    private readonly string example7BitMenuButtonItemHTMLCode = @"
<style>
    .custom-btn-sm {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }

    .custom-btn-md {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }

    .custom-btn-lg {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-sm""
               ButtonSize=""BitButtonSize.Small""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-md""
               ButtonSize=""BitButtonSize.Medium""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-lg""
               ButtonSize=""BitButtonSize.Large""
               Items=""basicMenuButton""
               OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />";
    private readonly string example7CustomItemHTMLCode = @"
<style>
    .custom-btn-sm {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }

    .custom-btn-md {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }

    .custom-btn-lg {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-sm""
               ButtonSize=""BitButtonSize.Small""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-md""
               ButtonSize=""BitButtonSize.Medium""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-lg""
               ButtonSize=""BitButtonSize.Large""
               Items=""basicMenuButtonWithCustomType""
               TextField=""@nameof(MenuActionItem.Name)""
               KeyField=""@nameof(MenuActionItem.Id)""
               IconNameField=""@nameof(MenuActionItem.Icon)""
               OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />";
    private readonly string example7BitMenuButtonOptionHTMLCode = @"
<style>
    .custom-btn-sm {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }

    .custom-btn-md {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }

    .custom-btn-lg {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-sm""
               ButtonSize=""BitButtonSize.Small""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitLabel>Medium size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-md""
               ButtonSize=""BitButtonSize.Medium""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitLabel>Large size</BitLabel>
<BitMenuButton Text=""Button""
               Class=""custom-btn-lg""
               ButtonSize=""BitButtonSize.Large""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";
    private readonly string example7BitMenuButtonItemCSharpCode = @"
private string example1SelectedItem;

private List<BitMenuButtonItem> basicMenuButton = new()
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
";
    private readonly string example7CustomItemCSharpCode = @"
private string example1SelectedItem;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<MenuActionItem> basicMenuButtonWithCustomType = new()
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
    private readonly string example7BitMenuButtonOptionCSharpCode = @"
private string example1SelectedItem;
";
}
