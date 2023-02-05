using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons.MenuButtonList;

public partial class BitMenuButtonListDemo
{
    private string example1SelectedItem;
    private string example2SelectedItem;
    private string example3SelectedItem;
    private string example4SelectedItem;
    private string example5SelectedItem;
    private string example6SelectedItem;

    private List<ButtonInfo> basicMenuButton = new()
    {
        new ButtonInfo()
        {
            Name = "Item A",
            Key = "A",
            Icon = BitIconName.Emoji
        },
        new ButtonInfo()
        {
            Name = "Item B",
            Key = "B",
            Icon = BitIconName.Emoji
        },
        new ButtonInfo()
        {
            Name = "Item C",
            Key = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<ButtonInfo> disabledItemMenuButton = new()
    {
        new ButtonInfo()
        {
            Name = "Item A",
            Key = "A",
            Icon = BitIconName.Emoji
        },
        new ButtonInfo()
        {
            Name = "Item B",
            Key = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new ButtonInfo()
        {
            Name = "Item C",
            Key = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<ButtonInfo> itemTemplateMenuButton = new()
    {
        new ButtonInfo()
        {
            Name = "Add",
            Key = "add-key",
            Icon = BitIconName.Add
        },
        new ButtonInfo()
        {
            Name = "Edit",
            Key = "edit-key",
            Icon = BitIconName.Edit
        },
        new ButtonInfo()
        {
            Name = "Delete",
            Key = "delete-key",
            Icon = BitIconName.Delete
        }
    };

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
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard."
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button."
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
        },
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButtonList Text=""Standard""
                        ButtonStyle=""BitButtonStyle.Standard""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example1SelectedItem = item.Key"" />

    <BitMenuButtonList Text=""Primary""
                        ButtonStyle=""BitButtonStyle.Primary""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example1SelectedItem = item.Key"" />

    <BitMenuButtonList Items=""basicMenuButton""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        Text=""Disabled""
                        IsEnabled=""false"" />

    <BitMenuButtonList Text=""Item Disabled""
                        Items=""disabledItemMenuButton""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ButtonInfo item) => example1SelectedItem = item.Key"" />
</div>
<div class=""selected-item"">Selected Item: @example1SelectedItem</div>
";

    private readonly string example2HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButtonList Text=""Standard Button""
                        IconName=""BitIconName.Add""
                        ButtonStyle=""BitButtonStyle.Standard""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example2SelectedItem = item.Key"" />

    <BitMenuButtonList Text=""Primary Button""
                        IconName=""BitIconName.Edit""
                        ButtonStyle=""BitButtonStyle.Primary""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example2SelectedItem = item.Key"" />
</div>
<div class=""selected-item"">Selected Item: @example2SelectedItem</div>
";

    private readonly string example3HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .custom-menu-btn.primary {
        height: 2.5rem;
        width: 10.5rem;
        background-color: #515151;
        border-color: black;
    }

    .custom-menu-btn.primary:hover {
        background-color: #403f3f;
        border-color: black;
    }
</style>

<div class=""example-content"">
    <BitMenuButtonList Text=""Styled Button""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example3SelectedItem = item.Key""
                        Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

    <BitMenuButtonList Text=""Classed Button""
                        Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example3SelectedItem = item.Key""
                        Class=""custom-menu-btn"" />
</div>
<div class=""selected-item"">Selected Item: @example3SelectedItem</div>
";

    private readonly string example4HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .example-content.column {
        flex-direction: column;
        align-items: flex-start;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content column"">
    <div>
        <BitMenuButtonList Text=""Visible Button""
                            Items=""basicMenuButton""
                            TextField=""@nameof(ButtonInfo.Name)""
                            IconNameField=""@nameof(ButtonInfo.Icon)""
                            OnItemClick=""(ButtonInfo item) => example4SelectedItem = item.Key""
                            Visibility=""BitComponentVisibility.Visible"" />
    </div>
    <div>
        Hidden Button: [<BitMenuButtonList Text=""Styled Button""
                                            IconName=""BitIconName.Add""
                                            Items=""basicMenuButton""
                                            TextFieldSelector=""item => item.Name""
                                            IconNameFieldSelector=""item => item.Icon""
                                            Visibility=""BitComponentVisibility.Hidden"" />]
    </div>
    <div>
        Collapsed Button: [<BitMenuButtonList Text=""Styled Button""
                                                IconName=""BitIconName.Add""
                                                Items=""basicMenuButton""
                                                TextField=""@nameof(ButtonInfo.Name)""
                                                IconNameField=""@nameof(ButtonInfo.Icon)""
                                                Visibility=""BitComponentVisibility.Collapsed"" />]
    </div>
</div>
<div class=""selected-item"">Selected Item: @example4SelectedItem</div>
";

    private readonly string example5HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <BitMenuButtonList Items=""basicMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example5SelectedItem = item.Key""
                        ButtonStyle=""BitButtonStyle.Standard"">
        <HeaderTemplate>
            <div style=""font-weight: bold; color: #d13438;"">
                Custom Header!
            </div>
        </HeaderTemplate>
    </BitMenuButtonList>

    <BitMenuButtonList Items=""basicMenuButton""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ButtonInfo item) => example5SelectedItem = item.Key""
                        ButtonStyle=""BitButtonStyle.Primary"">
        <HeaderTemplate>
            <BitIcon IconName=""BitIconName.Warning"" />
            <div style=""font-weight: 600; color: white;"">
                Custom Header!
            </div>
            <BitIcon IconName=""BitIconName.Warning"" />
        </HeaderTemplate>
    </BitMenuButtonList>
</div>
<div class=""selected-item"">Selected Item: @example5SelectedItem</div>
";

    private readonly string example6HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: center;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }

    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<div class=""example-content"">
    <BitMenuButtonList Text=""Standard Button""
                        IconName=""BitIconName.Edit""
                        Items=""itemTemplateMenuButton""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ButtonInfo item) => example6SelectedItem = item.Key""
                        ButtonStyle=""BitButtonStyle.Standard"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Name (@item.Key)
                </span>
            </div>
        </ItemTemplate>
    </BitMenuButtonList>

    <BitMenuButtonList Text=""Primary Button""
                        IconName=""BitIconName.Edit""
                        Items=""itemTemplateMenuButton""
                        TextField=""@nameof(ButtonInfo.Name)""
                        IconNameField=""@nameof(ButtonInfo.Icon)""
                        OnItemClick=""(ButtonInfo item) => example6SelectedItem = item.Key""
                        ButtonStyle=""BitButtonStyle.Primary"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Name (@item.Key)
                </span>
            </div>
        </ItemTemplate>
    </BitMenuButtonList>
</div>
<div class=""selected-item"">Selected Item: @example6SelectedItem</div>
";

    private readonly string example1CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example1SelectedItem;

private List<ButtonInfo> basicMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<ButtonInfo> disabledItemMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example2CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example2SelectedItem;

private List<ButtonInfo> basicMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example3CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example3SelectedItem;

private List<ButtonInfo> basicMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example4CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example4SelectedItem;

private List<ButtonInfo> basicMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example5CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example5SelectedItem;

private List<ButtonInfo> basicMenuButton = new()
{
    new ButtonInfo()
    {
        Name = ""Item A"",
        Key = ""A"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item B"",
        Key = ""B"",
        Icon = BitIconName.Emoji
    },
    new ButtonInfo()
    {
        Name = ""Item C"",
        Key = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example6CSharpCode = @"
public class ButtonInfo
{
    public string Name { get; set; }
    public string Key { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example6SelectedItem;

    private List<ButtonInfo> itemTemplateMenuButton = new()
    {
        new ButtonInfo()
        {
            Name = ""Add"",
            Key = ""add-key"",
            Icon = BitIconName.Add
        },
        new ButtonInfo()
        {
            Name = ""Edit"",
            Key = ""edit-key"",
            Icon = BitIconName.Edit
        },
        new ButtonInfo()
        {
            Name = ""Delete"",
            Key = ""delete-key"",
            Icon = BitIconName.Delete
        }
    };
";
}
