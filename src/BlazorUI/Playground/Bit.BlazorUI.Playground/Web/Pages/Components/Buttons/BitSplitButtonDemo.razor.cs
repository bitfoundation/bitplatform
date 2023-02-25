using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitSplitButtonDemo
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
        new ComponentParameter()
        {
            Name = "ButtonSize",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "BitButtonSize.Medium",
            Description = "The size of button, Possible values: Small | Medium | Large.",
        },
        new ComponentParameter
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitSplitButton, that are BitSplitButtonOption components.",
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitSplitButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "IsSticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the current item is going to be change selected item."
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<BitSplitButtonItem>",
            DefaultValue = "new List<BitSplitButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton.",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
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
            Name = "OnClick",
            Type = "EventCallback<BitSplitButtonItem>",
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
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "split-button-items",
            Title = "BitSplitButtonItem",
            Description = "BitSplitButtonItem is default type for item.",
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
                   Description = "A unique value to use as a key of the item.",
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
            Id = "split-button-options",
            Title = "BitSplitButtonOption",
            Description = "BitSplitButtonOption is a child component for BitSplitButton.",
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
                   Description = "A unique value to use as a key of the item.",
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
            Id = "button-size-enum",
            Title = "BitButtonSize Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Small",
                    Description="The button size is small.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Medium",
                    Description="The button size is medium.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Large",
                    Description="The button size is large.",
                    Value="2",
                }
            }
        },
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
    };


    private string BasicClickedItem;
    private string IsStickyClickedItem;
    private string DisabledClickedItem;
    private string TemplateClickedItem;

    private List<BitSplitButtonItem> BasicItems = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> IsStickyItems = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Add",
            Key = "add-key",
            IconName = BitIconName.Add
        },
        new BitSplitButtonItem()
        {
            Text = "Edit",
            Key = "edit-key",
            IconName = BitIconName.Edit
        },
        new BitSplitButtonItem()
        {
            Text = "Delete",
            Key = "delete-key",
            IconName = BitIconName.Delete
        }
    };
    private List<BitSplitButtonItem> DisabledItems = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitSplitButtonItem()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji2
        },
        new BitSplitButtonItem()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new BitSplitButtonItem()
        {
            Text = "Item D",
            Key = "D",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> TemplateItems = new()
    {
        new BitSplitButtonItem()
        {
            Text = "Add",
            Key = "add-key"
        },
        new BitSplitButtonItem()
        {
            Text = "Edit",
            Key = "edit-key"
        },
        new BitSplitButtonItem()
        {
            Text = "Delete",
            Key = "delete-key"
        }
    };

    private List<SplitActionItem> BasicCustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Item A",
            Id = "A"
        },
        new SplitActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji
        },
        new SplitActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitActionItem> IsStickyCustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Add",
            Id = "add-key",
            Icon = BitIconName.Add
        },
        new SplitActionItem()
        {
            Name = "Edit",
            Id = "edit-key",
            Icon = BitIconName.Edit
        },
        new SplitActionItem()
        {
            Name = "Delete",
            Id = "delete-key",
            Icon = BitIconName.Delete
        }
    };
    private List<SplitActionItem> DisabledCustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitActionItem()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji2
        },
        new SplitActionItem()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new SplitActionItem()
        {
            Name = "Item D",
            Id = "D",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitActionItem> TemplateCustomItems = new()
    {
        new SplitActionItem()
        {
            Name = "Add",
            Id = "add-key"
        },
        new SplitActionItem()
        {
            Name = "Edit",
            Id = "edit-key"
        },
        new SplitActionItem()
        {
            Name = "Delete",
            Id = "delete-key"
        }
    };

    private void OnTabClick()
    {
        BasicClickedItem = string.Empty;
        IsStickyClickedItem = string.Empty;
        DisabledClickedItem = string.Empty;
        TemplateClickedItem = string.Empty;
    }

    private readonly string example1BitSplitButtonItemHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Disabled</BitLabel>
        <BitSplitButton Items=""BasicItems"" IsEnabled=""false"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @BasicClickedItem</div>
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        TextFieldSelector=""item => item.Name""
                        KeyFieldSelector=""item => item.Id""
                        IconNameFieldSelector=""item => item.Icon""
                        ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Disabled</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        IsEnabled=""false"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @BasicClickedItem</div>
";
    private readonly string example1BitSplitButtonOptionHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Disabled</BitLabel>
        <BitSplitButton IsEnabled=""false""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @BasicClickedItem</div>
";
    private readonly string example1BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> BasicItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Item A"",
        Key = ""A""
    },
    new BitSplitButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new BitSplitButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji2
    }
};

private string BasicClickedItem;
";
    private readonly string example1CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> BasicCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Item A"",
        Id = ""A""
    },
    new SplitActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji
    },
    new SplitActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji2
    }
};

private string BasicClickedItem;
";
    private readonly string example1BitSplitButtonOptionCSharpCode = @"
private string BasicClickedItem;
";

    private readonly string example2BitSplitButtonItemHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""IsStickyItems""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""IsStickyItems""
                        ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @IsStickyClickedItem</div>
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""IsStickyCustomItems""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""IsStickyCustomItems""
                        TextFieldSelector=""item => item.Name""
                        KeyFieldSelector=""item => item.Id""
                        IconNameFieldSelector=""item => item.Icon""
                        ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @IsStickyClickedItem</div>
";
    private readonly string example2BitSplitButtonOptionHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
            <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
            <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
            <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
            <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @IsStickyClickedItem</div>
";
    private readonly string example2BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> IsStickyItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        Key = ""add-key"",
        IconName = BitIconName.Add
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        Key = ""edit-key"",
        IconName = BitIconName.Edit
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        Key = ""delete-key"",
        IconName = BitIconName.Delete
    }
};

private string IsStickyClickedItem;
";
    private readonly string example2CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> IsStickyCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Add"",
        Id = ""add-key"",
        Icon = BitIconName.Add
    },
    new SplitActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key"",
        Icon = BitIconName.Edit
    },
    new SplitActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key"",
        Icon = BitIconName.Delete
    }
};

private string IsStickyClickedItem;
";
    private readonly string example2BitSplitButtonOptionCSharpCode = @"
private string IsStickyClickedItem;
";

    private readonly string example3BitSplitButtonItemHTMLCode = @"
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
    <div>
        <BitLabel>Sticky Primary</BitLabel>
        <BitSplitButton Items=""DisabledItems""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Basic Standard</BitLabel>
        <BitSplitButton Items=""DisabledItems""
                        ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @DisabledClickedItem</div>
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
    <div>
        <BitLabel>Sticky Primary</BitLabel>
        <BitSplitButton Items=""DisabledCustomItems""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Basic Standard</BitLabel>
        <BitSplitButton Items=""DisabledCustomItems""
                        TextFieldSelector=""item => item.Name""
                        KeyFieldSelector=""item => item.Id""
                        IconNameFieldSelector=""item => item.Icon""
                        ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name"" />
    </div>
</div>
<div class=""clicked-item"">Clicked item: @DisabledClickedItem</div>
";
    private readonly string example3BitSplitButtonOptionHTMLCode = @"
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
    <div>
        <BitLabel>Sticky Primary</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji2"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Basic Standard</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                        OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji2"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @DisabledClickedItem</div>
";
    private readonly string example3BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> DisabledItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new BitSplitButtonItem()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji2
    },
    new BitSplitButtonItem()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new BitSplitButtonItem()
    {
        Text = ""Item D"",
        Key = ""D"",
        IconName = BitIconName.Emoji2
    }
};

private string DisabledClickedItem;
";
    private readonly string example3CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> DisabledCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new SplitActionItem()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji2
    },
    new SplitActionItem()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new SplitActionItem()
    {
        Name = ""Item D"",
        Id = ""D"",
        Icon = BitIconName.Emoji2
    }
};

private string DisabledClickedItem;
";
    private readonly string example3BitSplitButtonOptionCSharpCode = @"
private string DisabledClickedItem;
";

    private readonly string example4BitSplitButtonItemHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""TemplateItems""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonItem item) => TemplateClickedItem = item.Text"">
            <ItemTemplate Context=""item"">
                <div class=""item-template-box"">
                    <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                        @item.Text (@item.Key)
                    </span>
                </div>
            </ItemTemplate>
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""TemplateItems""
                        ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonItem item) => TemplateClickedItem = item.Text"">
            <ItemTemplate Context=""item"">
                @if (item.Key == ""add-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Add"" />
                        <span style=""color: green;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
                else if (item.Key == ""edit-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Edit"" />
                        <span style=""color: yellow;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
                else if (item.Key == ""delete-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Delete"" />
                        <span style=""color: red;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
            </ItemTemplate>
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @TemplateClickedItem</div>
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

    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<div class=""example-content"">
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton Items=""TemplateCustomItems""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(SplitActionItem item) => TemplateClickedItem = item.Name"">
            <ItemTemplate Context=""item"">
                <div class=""item-template-box"">
                    <span style=""color: @(item.Id == ""add-key"" ? ""green"" : item.Id == ""edit-key"" ? ""yellow"" : ""red"");"">
                        @item.Name (@item.Id)
                    </span>
                </div>
            </ItemTemplate>
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton Items=""TemplateCustomItems""
                        TextFieldSelector=""item => item.Name""
                        KeyFieldSelector=""item => item.Id""
                        IconNameFieldSelector=""item => item.Icon""
                        ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(SplitActionItem item) => TemplateClickedItem = item.Name"">
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
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @TemplateClickedItem</div>
";
    private readonly string example4BitSplitButtonOptionHTMLCode = @"
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
    <div>
        <BitLabel>Primary</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonOption item) => TemplateClickedItem = item.Text"">
            <ItemTemplate Context=""item"">
                <div class=""item-template-box"">
                    <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                        @item.Text (@item.Key)
                    </span>
                </div>
            </ItemTemplate>
            <ChildContent>
                <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
                <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
                <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
            </ChildContent>
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Standard</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                        IsSticky=""true""
                        OnClick=""(BitSplitButtonOption item) => TemplateClickedItem = item.Text"">
            <ItemTemplate Context=""item"">
                @if (item.Key == ""add-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Add"" />
                        <span style=""color: green;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
                else if (item.Key == ""edit-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Edit"" />
                        <span style=""color: yellow;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
                else if (item.Key == ""delete-key"")
                {
                    <div class=""item-template-box"">
                        <BitIcon IconName=""BitIconName.Delete"" />
                        <span style=""color: red;"">
                            @item.Text (@item.Key)
                        </span>
                    </div>
                }
            </ItemTemplate>
            <ChildContent>
                <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
                <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
                <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
            </ChildContent>
        </BitSplitButton>
    </div>
</div>
<div class=""clicked-item"">Clicked item: @TemplateClickedItem</div>
";
    private readonly string example4BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        Key = ""add-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        Key = ""edit-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        Key = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example4CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> TemplateCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Add"",
        Id = ""add-key""
    },
    new SplitActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key""
    },
    new SplitActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example4BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";

    private readonly string example5BitSplitButtonItemHTMLCode = @"
<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        ButtonSize=""BitButtonSize.Small""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        ButtonSize=""BitButtonSize.Medium""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        ButtonSize=""BitButtonSize.Large""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
</div>";
    private readonly string example5CustomItemHTMLCode = @"
 <div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        ButtonSize=""BitButtonSize.Small""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        ButtonSize=""BitButtonSize.Medium""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        ButtonSize=""BitButtonSize.Large""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
</div>";
    private readonly string example5BitSplitButtonOptionHTMLCode = @"
<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        ButtonSize=""BitButtonSize.Small""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        ButtonSize=""BitButtonSize.Medium""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        ButtonSize=""BitButtonSize.Large""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
</div>";
    private readonly string example5BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        Key = ""add-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        Key = ""edit-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        Key = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example5CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> TemplateCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Add"",
        Id = ""add-key""
    },
    new SplitActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key""
    },
    new SplitActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example5BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";

    private readonly string example6BitSplitButtonItemHTMLCode = @"
<style>
    .custom-btn-sm {
        &.small {
            height: 38px;
            font-size: 8px;
            line-height: 1.5;
        }
    }

    .custom-btn-md {
        &.medium {
            height: 48px;
            font-size: 16px;
            line-height: 1.4;
        }
    }

    .custom-btn-lg {
        &.large {
            height:50px;
            font-size: 30px;
            line-height: 1.33;
        }
    }
</style>

<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        Class=""custom-btn-sm""
                        ButtonSize=""BitButtonSize.Small""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        Class=""custom-btn-md""
                        ButtonSize=""BitButtonSize.Medium""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton Items=""BasicItems""
                        Class=""custom-btn-lg""
                        ButtonSize=""BitButtonSize.Large""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    </div>
</div>
";
    private readonly string example6CustomItemHTMLCode = @"
<style>
    .custom-btn-sm {
        &.small {
            height: 38px;
            font-size: 8px;
            line-height: 1.5;
        }
    }

    .custom-btn-md {
        &.medium {
            height: 48px;
            font-size: 16px;
            line-height: 1.4;
        }
    }

    .custom-btn-lg {
        &.large {
            height:50px;
            font-size: 30px;
            line-height: 1.33;
        }
    }
</style>

<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        Class=""custom-btn-sm""
                        ButtonSize=""BitButtonSize.Small""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        Class=""custom-btn-md""
                        ButtonSize=""BitButtonSize.Medium""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton Items=""BasicCustomItems""
                        Class=""custom-btn-lg""
                        ButtonSize=""BitButtonSize.Large""
                        TextField=""@nameof(SplitActionItem.Name)""
                        KeyField=""@nameof(SplitActionItem.Id)""
                        IconNameField=""@nameof(SplitActionItem.Icon)""
                        ButtonStyle=""BitButtonStyle.Primary""
                        OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
</div>
</div>";
    private readonly string example6BitSplitButtonOptionHTMLCode = @"
<style>
    .custom-btn-sm {
        &.small {
            height: 38px;
            font-size: 8px;
            line-height: 1.5;
        }
    }

    .custom-btn-md {
        &.medium {
            height: 48px;
            font-size: 16px;
            line-height: 1.4;
        }
    }

    .custom-btn-lg {
        &.large {
            height:50px;
            font-size: 30px;
            line-height: 1.33;
        }
    }
</style>

<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        Class=""custom-btn-sm""
                        ButtonSize=""BitButtonSize.Small""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        Class=""custom-btn-md""
                        ButtonSize=""BitButtonSize.Medium""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                        Class=""custom-btn-lg""
                        ButtonSize=""BitButtonSize.Large""
                        OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
            <BitSplitButtonOption Text=""Item A"" Key=""A"" />
            <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
            <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
        </BitSplitButton>
    </div>
</div>";
    private readonly string example6BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
{
    new BitSplitButtonItem()
    {
        Text = ""Add"",
        Key = ""add-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Edit"",
        Key = ""edit-key""
    },
    new BitSplitButtonItem()
    {
        Text = ""Delete"",
        Key = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example6CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<SplitActionItem> TemplateCustomItems = new()
{
    new SplitActionItem()
    {
        Name = ""Add"",
        Id = ""add-key""
    },
    new SplitActionItem()
    {
        Name = ""Edit"",
        Id = ""edit-key""
    },
    new SplitActionItem()
    {
        Name = ""Delete"",
        Id = ""delete-key""
    }
};

private string TemplateClickedItem;
";
    private readonly string example6BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";
}
