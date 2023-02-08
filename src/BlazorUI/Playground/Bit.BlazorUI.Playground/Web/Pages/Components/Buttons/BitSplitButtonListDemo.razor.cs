using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitSplitButtonListDemo
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
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<SplitActionItem>",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
            DefaultValue = "new List<SplitActionItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton."
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
        new ComponentParameter
        {
            Name = "IsSticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the current item is going to be change selected item."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<SplitActionItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<SplitActionItem>",
            Description = "The callback is called when the button or button item is clicked."
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


    private string BasicPrimarySelectedItem;
    private string BasicStandardSelectedItem;
    private string StickyPrimarySelectedItem;
    private string StickyStandardSelectedItem;
    private string DisabledPrimarySelectedItem;
    private string DisabledStandardSelectedItem;
    private string TemplatePrimarySelectedItem;
    private string TemplateStandardSelectedItem;

    private List<SplitButtonActionItem> example1Items = new()
    {
        new SplitButtonActionItem()
        {
            Name = "Item A",
            Id = "A"
        },
        new SplitButtonActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new SplitButtonActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitButtonActionItem> example2Items = new()
    {
        new SplitButtonActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new SplitButtonActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new SplitButtonActionItem()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };
    private List<SplitButtonActionItem> example3Items = new()
    {
        new SplitButtonActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitButtonActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji2
        },
        new SplitButtonActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitButtonActionItem()
        {
            Name = "Item D",
            Id = "D",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitButtonActionItem> example4Items = new()
    {
        new SplitButtonActionItem()
        {
            Name = "Add",
            Id = "add-key"
        },
        new SplitButtonActionItem()
        {
            Name = "Edit",
            Id = "edit-key"
        },
        new SplitButtonActionItem()
        {
            Name = "Delete",
            Id = "delete-key"
        }
    };

    private readonly string example1HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 4rem;
        width: fit-content;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButtonList Items=""example1Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            ButtonStyle=""BitButtonStyle.Primary""
                            OnClick=""(SplitButtonActionItem item) => BasicPrimarySelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @BasicPrimarySelectedItem</div>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButtonList Items=""example1Items""
                            KeyFieldSelector=""item => item.Id""
                            TextFieldSelector=""item => item.Name""
                            IconNameFieldSelector=""item => item.Icon""
                            ButtonStyle=""BitButtonStyle.Standard""
                            OnClick=""(SplitButtonActionItem item) => BasicStandardSelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @BasicStandardSelectedItem</div>
    </div>
    <div>
        <BitLabel>Disabled</BitLabel>
        <BitSplitButtonList Items=""example1Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            IsEnabled=""false"" />
    </div>
</div>
";
    private readonly string example1CSharpCode = @"
private string BasicPrimarySelectedItem;
private string BasicStandardSelectedItem;

private List<SplitButtonActionItem> example1Items = new()
{
    new SplitButtonActionItem()
    {
        Name = ""Item A"",
        Id = ""A""
    },
    new SplitButtonActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new SplitButtonActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example2HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 4rem;
        width: fit-content;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButtonList Items=""example2Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            ButtonStyle=""BitButtonStyle.Primary""
                            IsSticky=""true""
                            OnClick=""(SplitButtonActionItem item) => StickyPrimarySelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @StickyPrimarySelectedItem</div>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButtonList Items=""example2Items""
                            KeyFieldSelector=""item => item.Id""
                            TextFieldSelector=""item => item.Name""
                            IconNameFieldSelector=""item => item.Icon""
                            ButtonStyle=""BitButtonStyle.Standard""
                            IsSticky=""true""
                            OnClick=""(SplitButtonActionItem item) => StickyStandardSelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @StickyStandardSelectedItem</div>
    </div>
</div>
";
    private readonly string example2CSharpCode = @"
private string StickyPrimarySelectedItem;
private string StickyStandardSelectedItem;

private List<SplitButtonActionItem> example2Items = new()
{
    new SplitButtonActionItem()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new SplitButtonActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new SplitButtonActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};
";

    private readonly string example3HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 4rem;
        width: fit-content;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <div>
        <BitLabel>Sticky Primary</BitLabel>
        <BitSplitButtonList Items=""example3Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            ButtonStyle=""BitButtonStyle.Primary""
                            IsSticky=""true""
                            OnClick=""(SplitButtonActionItem item) => DisabledPrimarySelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @DisabledPrimarySelectedItem</div>
    </div>
    <div>
        <BitLabel>Basic Standard</BitLabel>
        <BitSplitButtonList Items=""example3Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            ButtonStyle=""BitButtonStyle.Standard""
                            OnClick=""(SplitButtonActionItem item) => DisabledStandardSelectedItem = item.Name"" />
        <div class=""selected-item"">Clicked item: @DisabledStandardSelectedItem</div>
    </div>
</div>
";
    private readonly string example3CSharpCode = @"
private string DisabledPrimarySelectedItem;
private string DisabledStandardSelectedItem;

private List<SplitButtonActionItem> example3Items = new()
{
    new SplitButtonActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new SplitButtonActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji2
    },
    new SplitButtonActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new SplitButtonActionItem()
    {
        Name = ""Item D"",
        Id = ""D"",
        Icon = BitIconName.Emoji2
    }
};
";

    private readonly string example4HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: row wrap;
        gap: 4rem;
        width: fit-content;
    }

    .selected-item {
        margin-top: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""example-content"">
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButtonList Items=""example4Items""
                            KeyField=""@nameof(SplitButtonActionItem.Id)""
                            TextField=""@nameof(SplitButtonActionItem.Name)""
                            IconNameField=""@nameof(SplitButtonActionItem.Icon)""
                            ButtonStyle=""BitButtonStyle.Primary""
                            IsSticky=""true""
                            OnClick=""(SplitButtonActionItem item) => TemplatePrimarySelectedItem = item.Name"">
            <ItemTemplate Context=""item"">
                <div class=""item-template-box"">
                    <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                        @item.Name (@item.Id)
                    </span>
                </div>
            </ItemTemplate>
        </BitSplitButtonList>
        <div class=""selected-item"">Clicked item: @TemplatePrimarySelectedItem</div>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButtonList Items=""example4Items""
                            KeyFieldSelector=""item => item.Id""
                            TextFieldSelector=""item => item.Name""
                            IconNameFieldSelector=""item => item.Icon""
                            ButtonStyle=""BitButtonStyle.Standard""
                            IsSticky=""true""
                            OnClick=""(SplitButtonActionItem item) => TemplateStandardSelectedItem = item.Name"">
            <ItemTemplate Context=""item"">
                @if (item.Id == ""add-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Add"" />
                        <span style=""color: green;"">
                            @item.Name (@item.Id)
                        </span>
                    </div>
                }
                else if (item.Id == ""edit-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Edit"" />
                        <span style=""color: yellow;"">
                            @item.Name (@item.Id)
                        </span>
                    </div>
                }
                else if (item.Id == ""delete-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Delete"" />
                        <span style=""color: red;"">
                            @item.Name (@item.Id)
                        </span>
                    </div>
                }
            </ItemTemplate>
        </BitSplitButtonList>
        <div class=""selected-item"">Clicked item: @TemplateStandardSelectedItem</div>
    </div>
</div>
";
    private readonly string example4CSharpCode = @"
private string TemplatePrimarySelectedItem;
private string TemplateStandardSelectedItem;

private List<SplitButtonActionItem> example4Items = new()
{
    new SplitButtonActionItem()
    {
        Name = ""Add"",
        Id = ""add-key""
    },
    new SplitButtonActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key""
    },
    new SplitButtonActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key""
    }
};
";
}
