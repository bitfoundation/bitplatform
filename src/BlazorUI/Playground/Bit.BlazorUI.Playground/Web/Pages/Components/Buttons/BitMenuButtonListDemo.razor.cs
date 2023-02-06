using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitMenuButtonListDemo
{
    private string example1SelectedItem;
    private string example2SelectedItem;
    private string example3SelectedItem;
    private string example4SelectedItem;
    private string example5SelectedItem;
    private string example6SelectedItem;

    private List<ActionItem> basicMenuButton = new()
    {
        new ActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
        },
        new ActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
        },
        new ActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<ActionItem> disabledItemMenuButton = new()
    {
        new ActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji
        },
        new ActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new ActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<ActionItem> itemTemplateMenuButton = new()
    {
        new ActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new ActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new ActionItem()
        {
            Name = "Delete",
            Id = "delete-key",
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
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example1SelectedItem = item.Id"" />

    <BitMenuButtonList Text=""Primary""
                        ButtonStyle=""BitButtonStyle.Primary""
                        Items=""basicMenuButton""
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example1SelectedItem = item.Id"" />

    <BitMenuButtonList Items=""basicMenuButton""
                        KeyFieldSelector=""item => item.Id""
                        IsEnabledFieldSelector=""item => item.IsEnabled""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        Text=""Disabled""
                        IsEnabled=""false"" />

    <BitMenuButtonList Text=""Item Disabled""
                        Items=""disabledItemMenuButton""
                        KeyFieldSelector=""item => item.Id""
                        IsEnabledFieldSelector=""item => item.IsEnabled""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ActionItem item) => example1SelectedItem = item.Id"" />
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
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example2SelectedItem = item.Id"" />

    <BitMenuButtonList Text=""Primary Button""
                        IconName=""BitIconName.Edit""
                        ButtonStyle=""BitButtonStyle.Primary""
                        Items=""basicMenuButton""
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example2SelectedItem = item.Id"" />
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
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example3SelectedItem = item.Id""
                        Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"" />

    <BitMenuButtonList Text=""Classed Button""
                        Items=""basicMenuButton""
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example3SelectedItem = item.Id""
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
                            KeyField=""@nameof(ActionItem.Id)""
                            IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                            TextField=""@nameof(ActionItem.Name)""
                            IconNameField=""@nameof(ActionItem.Icon)""
                            OnItemClick=""(ActionItem item) => example4SelectedItem = item.Id""
                            Visibility=""BitComponentVisibility.Visible"" />
    </div>
    <div>
        Hidden Button: [<BitMenuButtonList Text=""Styled Button""
                                            IconName=""BitIconName.Add""
                                            Items=""basicMenuButton""
                                            KeyFieldSelector=""item => item.Id""
                                            IsEnabledFieldSelector=""item => item.IsEnabled""
                                            TextFieldSelector=""item => item.Name""
                                            IconNameFieldSelector=""item => item.Icon""
                                            Visibility=""BitComponentVisibility.Hidden"" />]
    </div>
    <div>
        Collapsed Button: [<BitMenuButtonList Text=""Styled Button""
                                                IconName=""BitIconName.Add""
                                                Items=""basicMenuButton""
                                                KeyField=""@nameof(ActionItem.Id)""
                                                IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                                                TextField=""@nameof(ActionItem.Name)""
                                                IconNameField=""@nameof(ActionItem.Icon)""
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
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example5SelectedItem = item.Id""
                        ButtonStyle=""BitButtonStyle.Standard"">
        <HeaderTemplate>
            <div style=""font-weight: bold; color: #d13438;"">
                Custom Header!
            </div>
        </HeaderTemplate>
    </BitMenuButtonList>

    <BitMenuButtonList Items=""basicMenuButton""
                        KeyFieldSelector=""item => item.Id""
                        IsEnabledFieldSelector=""item => item.IsEnabled""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ActionItem item) => example5SelectedItem = item.Id""
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
                        KeyFieldSelector=""item => item.Id""
                        IsEnabledFieldSelector=""item => item.IsEnabled""
                        TextFieldSelector=""item => item.Name""
                        IconNameFieldSelector=""item => item.Icon""
                        OnItemClick=""(ActionItem item) => example6SelectedItem = item.Id""
                        ButtonStyle=""BitButtonStyle.Standard"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Name (@item.Id)
                </span>
            </div>
        </ItemTemplate>
    </BitMenuButtonList>

    <BitMenuButtonList Text=""Primary Button""
                        IconName=""BitIconName.Edit""
                        Items=""itemTemplateMenuButton""
                        KeyField=""@nameof(ActionItem.Id)""
                        IsEnabledField=""@nameof(ActionItem.IsEnabled)""
                        TextField=""@nameof(ActionItem.Name)""
                        IconNameField=""@nameof(ActionItem.Icon)""
                        OnItemClick=""(ActionItem item) => example6SelectedItem = item.Id""
                        ButtonStyle=""BitButtonStyle.Primary"">
        <ItemTemplate Context=""item"">
            <div class=""item-template-box"">
                <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                    @item.Name (@item.Id)
                </span>
            </div>
        </ItemTemplate>
    </BitMenuButtonList>
</div>
<div class=""selected-item"">Selected Item: @example6SelectedItem</div>
";

    private readonly string example1CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example1SelectedItem;

private List<ActionItem> basicMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private List<ActionItem> disabledItemMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example2CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example2SelectedItem;

private List<ActionItem> basicMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example3CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example3SelectedItem;

private List<ActionItem> basicMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example4CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example4SelectedItem;

private List<ActionItem> basicMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example5CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private string example5SelectedItem;

private List<ActionItem> basicMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji,
    },
    new ActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example6CSharpCode = @"
public class ActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<ActionItem> itemTemplateMenuButton = new()
{
    new ActionItem()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new ActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new ActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};
";
}
