using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Components;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the button for the benefit of screen readers."
        },
        new ComponentParameter
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new ComponentParameter
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard."
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitMenuButton, that are BitMenuButtonOption components.",
        },
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "BitIconName?",
            Description = "The icon to show inside the header of MenuButton.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "RenderFragment<TItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<TItem>",
            LinkType = LinkType.Link,
            Href = "#menu-button-items",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the MenuButton."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is enabled.",
        },
        new ComponentParameter()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is enabled.",
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback is called when the MenuButton header is clicked."
        },
        new ComponentParameter
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "OnClick of each item returns that item with its property."
        },
        new ComponentParameter
        {
            Name = "Text",
            Type = "string?",
            Description = "The text to show inside the header of MenuButton."
        },
        new ComponentParameter()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            Description = "Name of an icon to render next to the item text.",
        },
        new ComponentParameter()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "A unique value to use as a key of the item.",
        },
        new ComponentParameter()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, string>>?",
            Description = "A unique value to use as a key of the item.",
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
            Description = "BitMenuButtonItem is default type for item.",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a Key of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        },
        new ComponentSubParameter()
        {
            Id = "menu-button-options",
            Title = "BitMenuButtonOption",
            Description = "BitMenuButtonOption is a child component for BitMenuButton.",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a Key of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to render in the item.",
               }
            }
        }
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "button-style-enum",
            Title = "BitButtonStyle Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "button-type-enum",
            Title = "BitButtonType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new EnumItem()
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
        new BitMenuButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> disabledItemMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new BitMenuButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitMenuButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitMenuButtonItem> itemTemplateMenuButton = new()
    {
        new BitMenuButtonItem()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new BitMenuButtonItem()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitMenuButtonItem()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };

    private List<MenuActionItem> basicMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<MenuActionItem> disabledItemMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new MenuActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new MenuActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<MenuActionItem> itemTemplateMenuButtonWithCustomType = new()
    {
        new MenuActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new MenuActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new MenuActionItem()
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
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

    <BitMenuButton Text=""Primary""
                    ButtonStyle=""BitButtonStyle.Primary""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />

    <BitMenuButton Text=""Disabled""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key""
                    IsEnabled=""false"" />

    <BitMenuButton Text=""Item Disabled""
                    Items=""disabledItemMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example1SelectedItem = item.Key"" />
</div>
<div class=""clicked-item"">Clicked Item: @example1SelectedItem</div>
";
    private readonly string example1CustomItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Items=""basicMenuButtonWithCustomType""
                    TextField=""@nameof(MenuActionItem.Name)""
                    KeyField=""@nameof(MenuActionItem.Id)""
                    IconNameField=""@nameof(MenuActionItem.Icon)""
                    OnItemClick=""(MenuActionItem item) => example1SelectedItem = item.Id"" />

    <BitMenuButton Text=""Primary""
                    ButtonStyle=""BitButtonStyle.Primary""
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
</div>
<div class=""clicked-item"">Clicked Item: @example1SelectedItem</div>
";
    private readonly string example1BitMenuButtonOptionHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard""
                    ButtonStyle=""BitButtonStyle.Standard""
                    OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>

    <BitMenuButton Text=""Primary""
                    ButtonStyle=""BitButtonStyle.Primary""
                    OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>

    <BitMenuButton Text=""Disabled""
                    OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key""
                    IsEnabled=""false"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>

    <BitMenuButton Text=""Item Disabled""
                    OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" IsEnabled=""false"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" IsEnabled=""false"" />
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example1SelectedItem</div>
";
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
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    IconName=""BitIconName.Add""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />

    <BitMenuButton Text=""Primary Button""
                    IconName=""BitIconName.Edit""
                    ButtonStyle=""BitButtonStyle.Primary""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example2SelectedItem = item.Key"" />
</div>
<div class=""clicked-item"">Clicked Item: @example2SelectedItem</div>
";
    private readonly string example2CustomItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    IconName=""BitIconName.Add""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Items=""basicMenuButtonWithCustomType""
                    TextField=""@nameof(MenuActionItem.Name)""
                    KeyField=""@nameof(MenuActionItem.Id)""
                    IconNameField=""@nameof(MenuActionItem.Icon)""
                    OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />

    <BitMenuButton Text=""Primary Button""
                    IconName=""BitIconName.Edit""
                    ButtonStyle=""BitButtonStyle.Primary""
                    Items=""basicMenuButtonWithCustomType""
                    TextFieldSelector=""item => item.Name""
                    KeyFieldSelector=""item => item.Id""
                    IconNameFieldSelector=""item => item.Icon""
                    OnItemClick=""(MenuActionItem item) => example2SelectedItem = item.Id"" />
</div>
<div class=""clicked-item"">Clicked Item: @example2SelectedItem</div>
";
    private readonly string example2BitMenuButtonOptionHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    IconName=""BitIconName.Add""
                    ButtonStyle=""BitButtonStyle.Standard""
                    OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>

    <BitMenuButton Text=""Primary Button""
                    IconName=""BitIconName.Edit""
                    ButtonStyle=""BitButtonStyle.Primary""
                    OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example2SelectedItem</div>
";
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
private string example2SelectedItem;
";

    private readonly string example3BitMenuButtonItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Styled Button""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
                    Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

    <BitMenuButton Text=""Classed Button""
                    Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example3SelectedItem = item.Key""
                    Class=""custom-menu-btn"" />
</div>
<div class=""clicked-item"">Clicked Item: @example3SelectedItem</div>
";
    private readonly string example3CustomItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
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
                    Class=""custom-menu-btn"" />
</div>
<div class=""clicked-item"">Clicked Item: @example3SelectedItem</div>
";
    private readonly string example3BitMenuButtonOptionHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Styled Button""
                    OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key""
                    Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>

    <BitMenuButton Text=""Classed Button""
                    OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key""
                    Class=""custom-menu-btn"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example3SelectedItem</div>
";
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
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .custom-menu-btn {
        &.primary {
            height: 2.5rem;
            width: 10.5rem;
            background-color: #515151;
            border-color: black;

            &:hover {
                background-color: #403f3f;
                border-color: black;
            }
        }
    }
</style>

<div class=""example-content"">
    <BitMenuButton Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key""
                    ButtonStyle=""BitButtonStyle.Standard"">
        <HeaderTemplate>
            <div style=""font-weight: bold; color: #d13438;"">
                Custom Header!
            </div>
        </HeaderTemplate>
    </BitMenuButton>

    <BitMenuButton Items=""basicMenuButton""
                    OnItemClick=""(BitMenuButtonItem item) => example4SelectedItem = item.Key""
                    ButtonStyle=""BitButtonStyle.Primary"">
        <HeaderTemplate>
            <BitIcon IconName=""BitIconName.Warning"" />
            <div style=""font-weight: 600; color: white;"">
                Custom Header!
            </div>
            <BitIcon IconName=""BitIconName.Warning"" />
        </HeaderTemplate>
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example4SelectedItem</div>
";
    private readonly string example4CustomItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .custom-menu-btn {
        &.primary {
            height: 2.5rem;
            width: 10.5rem;
            background-color: #515151;
            border-color: black;

            &:hover {
                background-color: #403f3f;
                border-color: black;
            }
        }
    }
</style>

<div class=""example-content"">
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

    <BitMenuButton Items=""basicMenuButtonWithCustomType""
                    TextFieldSelector=""item => item.Name""
                    KeyFieldSelector=""item => item.Id""
                    IconNameFieldSelector=""item => item.Icon""
                    OnItemClick=""(MenuActionItem item) => example4SelectedItem = item.Id""
                    ButtonStyle=""BitButtonStyle.Primary"">
        <HeaderTemplate>
            <BitIcon IconName=""BitIconName.Warning"" />
            <div style=""font-weight: 600; color: white;"">
                Custom Header!
            </div>
            <BitIcon IconName=""BitIconName.Warning"" />
        </HeaderTemplate>
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example4SelectedItem</div>
";
    private readonly string example4BitMenuButtonOptionHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .custom-menu-btn {
        &.primary {
            height: 2.5rem;
            width: 10.5rem;
            background-color: #515151;
            border-color: black;

            &:hover {
                background-color: #403f3f;
                border-color: black;
            }
        }
    }
</style>

<div class=""example-content"">
    <BitMenuButton ButtonStyle=""BitButtonStyle.Standard""
                    OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
        <HeaderTemplate>
            <div style=""font-weight: bold; color: #d13438;"">
                Custom Header!
            </div>
        </HeaderTemplate>
        <ChildContent>
            <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
            <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
            <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji"" />
        </ChildContent>
    </BitMenuButton>

    <BitMenuButton ButtonStyle=""BitButtonStyle.Primary""
                    OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
        <HeaderTemplate>
            <BitIcon IconName=""BitIconName.Warning"" />
            <div style=""font-weight: 600; color: white;"">
                Custom Header!
            </div>
            <BitIcon IconName=""BitIconName.Warning"" />
        </HeaderTemplate>
        <ChildContent>
            <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
            <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
            <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji"" />
        </ChildContent>
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example4SelectedItem</div>
";
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
    private readonly string example4BitMenuButtonOptionCSharpCode = @"
private string example4SelectedItem;
";

    private readonly string example5BitMenuButtonItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    IconName=""BitIconName.Edit""
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

    <BitMenuButton Text=""Primary Button""
                    IconName=""BitIconName.Edit""
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
</div>
<div class=""clicked-item"">Clicked Item: @example5SelectedItem</div>
";
    private readonly string example5CustomItemHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    IconName=""BitIconName.Edit""
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

    <BitMenuButton Text=""Primary Button""
                    IconName=""BitIconName.Edit""
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
</div>
<div class=""clicked-item"">Clicked Item: @example5SelectedItem</div>
";
    private readonly string example5BitMenuButtonOptionHTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 30px;
    }

    .clicked-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<div class=""example-content"">
    <BitMenuButton Text=""Standard Button""
                    ButtonStyle=""BitButtonStyle.Standard""
                    IconName=""BitIconName.Add""
                    OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Text (@item.Key)
                </span>
            </div>
        </ItemTemplate>
        <ChildContent>
            <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
            <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
            <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
        </ChildContent>
    </BitMenuButton>

    <BitMenuButton Text=""Primary Button""
                    ButtonStyle=""BitButtonStyle.Primary""
                    IconName=""BitIconName.Edit""
                    OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Text (@item.Key)
                </span>
            </div>
        </ItemTemplate>
        <ChildContent>
            <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
            <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
            <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
        </ChildContent>
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example5SelectedItem</div>
";
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
    public BitIconName Icon { get; set; }
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
}
