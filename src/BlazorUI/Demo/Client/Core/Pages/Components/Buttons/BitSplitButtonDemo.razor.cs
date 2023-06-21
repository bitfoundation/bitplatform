using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitSplitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
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
            Type = "BitButtonType",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitSplitButton, that are BitSplitButtonOption components.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitSplitButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new()
        {
            Name = "IsSticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the current item is going to be change selected item."
        },
        new()
        {
            Name = "Items",
            Type = "List<BitSplitButtonItem>",
            DefaultValue = "new List<BitSplitButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton.",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
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
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item text.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<BitSplitButtonItem>",
            Description = "The callback is called when the button or button item is clicked."
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
            Description = "A unique value to use as a key of the item.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "split-button-items",
            Title = "BitSplitButtonItem",
            Description = "BitSplitButtonItem is default type for item.",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
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
                   Description = "A unique value to use as a key of the item.",
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
            Id = "split-button-options",
            Title = "BitSplitButtonOption",
            Description = "BitSplitButtonOption is a child component for BitSplitButton.",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
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
                   Description = "A unique value to use as a key of the item.",
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
        },
    };



    private string BasicClickedItem;
    private string IsStickyClickedItem;
    private string DisabledClickedItem;
    private string TemplateClickedItem;

    private List<BitSplitButtonItem> BasicItems = new()
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
    private List<BitSplitButtonItem> IsStickyItems = new()
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
    private List<BitSplitButtonItem> DisabledItems = new()
    {
        new()
        {
            Text = "Item A",
            Key = "A",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Text = "Item B",
            Key = "B",
            IconName = BitIconName.Emoji2
        },
        new()
        {
            Text = "Item C",
            Key = "C",
            IconName = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Text = "Item D",
            Key = "D",
            IconName = BitIconName.Emoji2
        }
    };
    private List<BitSplitButtonItem> TemplateItems = new()
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

    private List<SplitActionItem> BasicCustomItems = new()
    {
        new()
        {
            Name = "Item A",
            Id = "A"
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
    private List<SplitActionItem> IsStickyCustomItems = new()
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
    private List<SplitActionItem> DisabledCustomItems = new()
    {
        new()
        {
            Name = "Item A",
            Id = "A",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Item B",
            Id = "B",
            Icon = BitIconName.Emoji2
        },
        new()
        {
            Name = "Item C",
            Id = "C",
            Icon = BitIconName.Emoji,
            IsEnabled = false
        },
        new()
        {
            Name = "Item D",
            Id = "D",
            Icon = BitIconName.Emoji2
        }
    };
    private List<SplitActionItem> TemplateCustomItems = new()
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

    private void OnTabClick()
    {
        BasicClickedItem = string.Empty;
        IsStickyClickedItem = string.Empty;
        DisabledClickedItem = string.Empty;
        TemplateClickedItem = string.Empty;
    }

    private readonly string example1BitSplitButtonItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Disabled</BitLabel>
<BitSplitButton Items=""BasicItems"" IsEnabled=""false"" />

<div>Clicked item: @BasicClickedItem</div>";
    private readonly string example1CustomItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                TextFieldSelector=""item => item.Name""
                KeyFieldSelector=""item => item.Id""
                IconNameFieldSelector=""item => item.Icon""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />

<BitLabel>Disabled</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                IsEnabled=""false"" />

<div>Clicked item: @BasicClickedItem</div>";
    private readonly string example1BitSplitButtonOptionHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Disabled</BitLabel>
<BitSplitButton IsEnabled=""false""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @BasicClickedItem</div>";
    private readonly string example1BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> BasicItems = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A""
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji
    },
    new()
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
    new()
    {
        Name = ""Item A"",
        Id = ""A""
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

private string BasicClickedItem;
";
    private readonly string example1BitSplitButtonOptionCSharpCode = @"
private string BasicClickedItem;
";

    private readonly string example2BitSplitButtonItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""IsStickyItems""
                ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""IsStickyItems""
                ButtonStyle=""BitButtonStyle.Standard""
                IsSticky=""true""
                OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />

<div>Clicked item: @IsStickyClickedItem</div>";
    private readonly string example2CustomItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""IsStickyCustomItems""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""IsStickyCustomItems""
                TextFieldSelector=""item => item.Name""
                KeyFieldSelector=""item => item.Id""
                IconNameFieldSelector=""item => item.Icon""
                ButtonStyle=""BitButtonStyle.Standard""
                IsSticky=""true""
                OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name"" />

<div>Clicked item: @IsStickyClickedItem</div>";
    private readonly string example2BitSplitButtonOptionHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
</BitSplitButton>

<div>Clicked item: @IsStickyClickedItem</div>";
    private readonly string example2BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> IsStickyItems = new()
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

private string IsStickyClickedItem;
";
    private readonly string example2BitSplitButtonOptionCSharpCode = @"
private string IsStickyClickedItem;
";

    private readonly string example3BitSplitButtonItemHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton Items=""DisabledItems""
                ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton Items=""DisabledItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />

<div>Clicked item: @DisabledClickedItem</div>";
    private readonly string example3CustomItemHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton Items=""DisabledCustomItems""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name"" />
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton Items=""DisabledCustomItems""
                TextFieldSelector=""item => item.Name""
                KeyFieldSelector=""item => item.Id""
                IconNameFieldSelector=""item => item.Icon""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name"" />

<div>Clicked item: @DisabledClickedItem</div>";
    private readonly string example3BitSplitButtonOptionHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @DisabledClickedItem</div>";
    private readonly string example3BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> DisabledItems = new()
{
    new()
    {
        Text = ""Item A"",
        Key = ""A"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Text = ""Item B"",
        Key = ""B"",
        IconName = BitIconName.Emoji2
    },
    new()
    {
        Text = ""Item C"",
        Key = ""C"",
        IconName = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
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
    new()
    {
        Name = ""Item A"",
        Id = ""A"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
    {
        Name = ""Item B"",
        Id = ""B"",
        Icon = BitIconName.Emoji2
    },
    new()
    {
        Name = ""Item C"",
        Id = ""C"",
        Icon = BitIconName.Emoji,
        IsEnabled = false
    },
    new()
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
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

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

<div>Clicked item: @TemplateClickedItem</div>";
    private readonly string example4CustomItemHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

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

<div>Clicked item: @TemplateClickedItem</div>";
    private readonly string example4BitSplitButtonOptionHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

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

<div>Clicked item: @TemplateClickedItem</div>";
    private readonly string example4BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
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

private string TemplateClickedItem;
";
    private readonly string example4BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";

    private readonly string example5BitSplitButtonItemHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonSize=""BitButtonSize.Small""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Medium size</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonSize=""BitButtonSize.Medium""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Large size</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonSize=""BitButtonSize.Large""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />";
    private readonly string example5CustomItemHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                ButtonSize=""BitButtonSize.Small""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />

<BitLabel>Medium size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                ButtonSize=""BitButtonSize.Medium""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />

<BitLabel>Large size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                ButtonSize=""BitButtonSize.Large""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />";
    private readonly string example5BitSplitButtonOptionHTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                ButtonSize=""BitButtonSize.Small""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Medium size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                ButtonSize=""BitButtonSize.Medium""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Large size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                ButtonSize=""BitButtonSize.Large""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>";
    private readonly string example5BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
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

private string TemplateClickedItem;
";
    private readonly string example5BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";

    private readonly string example6BitSplitButtonItemHTMLCode = @"
<style>
    .custom-btn-sm {
        height: 38px;
        font-size: 8px;
        line-height: 1.5;
    }

    .custom-btn-md {
        height: 48px;
        font-size: 16px;
        line-height: 1.4;
    }

    .custom-btn-lg {
        height:50px;
        font-size: 30px;
        line-height: 1.33;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitSplitButton Items=""BasicItems""
                Class=""custom-btn-sm""
                ButtonSize=""BitButtonSize.Small""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
        
<BitLabel>Medium size</BitLabel>
<BitSplitButton Items=""BasicItems""
                Class=""custom-btn-md""
                ButtonSize=""BitButtonSize.Medium""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Large size</BitLabel>
<BitSplitButton Items=""BasicItems""
                Class=""custom-btn-lg""
                ButtonSize=""BitButtonSize.Large""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />";
    private readonly string example6CustomItemHTMLCode = @"
<style>
    .custom-btn-sm {
        height: 38px;
        font-size: 8px;
        line-height: 1.5;
    }

    .custom-btn-md {
        height: 48px;
        font-size: 16px;
        line-height: 1.4;
    }

    .custom-btn-lg {
        height:50px;
        font-size: 30px;
        line-height: 1.33;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                Class=""custom-btn-sm""
                ButtonSize=""BitButtonSize.Small""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />
        
<BitLabel>Medium size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                Class=""custom-btn-md""
                ButtonSize=""BitButtonSize.Medium""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />

<BitLabel>Large size</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                Class=""custom-btn-lg""
                ButtonSize=""BitButtonSize.Large""
                TextField=""@nameof(SplitActionItem.Name)""
                KeyField=""@nameof(SplitActionItem.Id)""
                IconNameField=""@nameof(SplitActionItem.Icon)""
                ButtonStyle=""BitButtonStyle.Primary""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name"" />";
    private readonly string example6BitSplitButtonOptionHTMLCode = @"
<style>
    .custom-btn-sm {
        height: 38px;
        font-size: 8px;
        line-height: 1.5;
    }

    .custom-btn-md {
        height: 48px;
        font-size: 16px;
        line-height: 1.4;
    }

    .custom-btn-lg {
        height:50px;
        font-size: 30px;
        line-height: 1.33;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                Class=""custom-btn-sm""
                ButtonSize=""BitButtonSize.Small""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Medium size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                Class=""custom-btn-md""
                ButtonSize=""BitButtonSize.Medium""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Large size</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Primary""
                Class=""custom-btn-lg""
                ButtonSize=""BitButtonSize.Large""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" />
</BitSplitButton>";
    private readonly string example6BitSplitButtonItemCSharpCode = @"
private List<BitSplitButtonItem> TemplateItems = new()
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

private string TemplateClickedItem;
";
    private readonly string example6BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";
}
