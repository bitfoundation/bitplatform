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
            Name = "ChevronDownIcon",
            Type = "string",
            DefaultValue = "ChevronDown",
            Description = "Icon name of the chevron down part of the BitSplitButton.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitSplitButton, that are BitSplitButtonOption components.",
        },
        new()
        {
            Name = "ClassStyles",
            Type = "BitSplitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes/styles for different parts of the BitSplitButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
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
            Name = "IsSticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the current item is going to be change selected item."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the SplitButton.",
            LinkType = LinkType.Link,
            Href = "#split-button-items",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The content inside the item can be customized."
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitSplitButtonNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<TItem>",
            Description = "The callback is called when the button or button item is clicked."
        }
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
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the item.",
               },
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
                   DefaultValue = "null",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the item.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
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
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the option.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the option text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the option is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key of the option.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the option.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the option.",
               }
            }
        },
        new()
        {
            Id = "class-styles",
            Title = "BitSplitButtonClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Callout",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for the callout container of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "ChevronDown",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for the chevron down of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "ItemButton",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for each item of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for each item icon of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "ItemText",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for each item text of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Overlay",
                   Type = "BitClassStylePair",
                   DefaultValue = "new()",
                   Description = "Custom CSS classes/styles for each overlay of the BitSplitButton.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
               }
            },
        },
        new()
        {
            Id = "class-style-pair",
            Title = "BitClassStylePair",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "Custom CSS classes."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom CSS styles."
               }
            }
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitSplitButtonNameSelectors",
            Parameters = new()
            {
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitSplitButtonItem.Text))",
                    Description = "Text field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
            }
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair",
            Parameters = new()
            {
               new()
               {
                   Name = "Name",
                   Type = "string",
                   Description = "Custom class property name."
               },
               new()
               {
                   Name = "Selector",
                   Type = "Func<TItem, TProp?>?",
                   Description = "Custom class property selector."
               }
            }
        },
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



    private void OnTabClick()
    {
        basicClickedItem = string.Empty;
        isStickyClickedItem = string.Empty;
        disabledClickedItem = string.Empty;
        templateClickedItem = string.Empty;
    }

    private readonly string example1BitSplitButtonItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""BasicItems"" OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />
    
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""BasicItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Disabled</BitLabel>
<BitSplitButton Items=""BasicItems"" IsEnabled=""false"" />

<BitLabel>Item disabled</BitLabel>
<BitSplitButton Items=""BasicItemsDisabled"" OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

<BitLabel>Item disabled</BitLabel>
<BitSplitButton Items=""BasicItemsDisabled""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => BasicClickedItem = item.Text"" />

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

    private readonly string example2BitSplitButtonItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""IsStickyItems""
                OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""IsStickyItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => IsStickyClickedItem = item.Text"" />

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

    private readonly string example3BitSplitButtonItemHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""DisabledItems""
                OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton Items=""DisabledItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => DisabledClickedItem = item.Text"" />

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

    private readonly string example4BitSplitButtonItemHTMLCode = @"
<style>
    .item-template-box {
        gap: 0.5rem;
        width: 100%;
        display: flex;
    }
</style>

<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""TemplateItems""
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
<BitSplitButton IsSticky=""true""
                Items=""TemplateItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonItem item) => TemplateClickedItem = item.Text"">
    <ItemTemplate Context=""item"">
        @if (item.Key == ""add-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Add"" />
                <span style=""color: green;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""edit-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Edit"" />
                <span style=""color: yellow;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""delete-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Delete"" />
                <span style=""color: red;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
    </ItemTemplate>
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



    private readonly string example1CustomItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton Items=""BasicCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<BitLabel>Disabled</BitLabel>
<BitSplitButton IsEnabled=""false""
                Items=""BasicCustomItems""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />

<BitLabel>Item disabled</BitLabel>
<BitSplitButton Items=""BasicCustomItemsDisabled""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />

<BitLabel>Item disabled</BitLabel>
<BitSplitButton Items=""BasicCustomItemsDisabled""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => BasicClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                          Key = { Name = nameof(SplitActionItem.Id) },
                                          Text = { Name = nameof(SplitActionItem.Name) }})"" />

<div>Clicked item: @BasicClickedItem</div>";
    private readonly string example1CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
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

    private readonly string example2CustomItemHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""IsStickyCustomItems""
                OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""IsStickyCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => IsStickyClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<div>Clicked item: @IsStickyClickedItem</div>";
    private readonly string example2CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
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

    private readonly string example3CustomItemHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""DisabledCustomItems""
                OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Name = nameof(SplitActionItem.Icon) },
                                         Key = { Name = nameof(SplitActionItem.Id) },
                                         Text = { Name = nameof(SplitActionItem.Name) }})"" />
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton Items=""DisabledCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => DisabledClickedItem = item.Name""
                NameSelectors=""@(new() { IconName = { Selector = item => item.Icon },
                                         Key = { Selector = item => item.Id },
                                         Text = { Selector = item => item.Name }})"" />

<div>Clicked item: @DisabledClickedItem</div>";
    private readonly string example3CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
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

    private readonly string example4CustomItemHTMLCode = @"
<style>
    .item-template-box {
        gap: 0.5rem;
        width: 100%;
        display: flex;
    }
</style>

<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                Items=""TemplateCustomItems""
                OnClick=""(SplitActionItem item) => TemplateClickedItem = item.Name""
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
                Items=""TemplateCustomItems""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(SplitActionItem item) => TemplateClickedItem = item.Name""
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

<div>Clicked item: @TemplateClickedItem</div>";
    private readonly string example4CustomItemCSharpCode = @"
public class SplitActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
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



    private readonly string example1BitSplitButtonOptionHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Disabled</BitLabel>
<BitSplitButton IsEnabled=""false"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Item disabled</BitLabel>
<BitSplitButton OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IsEnabled=""false"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Item disabled</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => BasicClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IsEnabled=""false"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @BasicClickedItem</div>";
    private readonly string example1BitSplitButtonOptionCSharpCode = @"
private string BasicClickedItem;
";

    private readonly string example2BitSplitButtonOptionHTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => IsStickyClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<div>Clicked item: @IsStickyClickedItem</div>";
    private readonly string example2BitSplitButtonOptionCSharpCode = @"
private string IsStickyClickedItem;
";

    private readonly string example3BitSplitButtonOptionHTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => DisabledClickedItem = item.Text"">
    <BitSplitButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @DisabledClickedItem</div>";
    private readonly string example3BitSplitButtonOptionCSharpCode = @"
private string DisabledClickedItem;
";

    private readonly string example4BitSplitButtonOptionHTMLCode = @"
<style>
    .item-template-box {
        gap: 0.5rem;
        width: 100%;
        display: flex;
    }
</style>

<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => TemplateClickedItem = item.Text"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => TemplateClickedItem = item.Text"">
    <ItemTemplate Context=""item"">
        @if (item.Key == ""add-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Add"" />
                <span style=""color: green;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""edit-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Edit"" />
                <span style=""color: yellow;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""delete-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Delete"" />
                <span style=""color: red;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
    </ItemTemplate>
    <ChildContent>
        <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitSplitButton>

<div>Clicked item: @TemplateClickedItem</div>";
    private readonly string example4BitSplitButtonOptionCSharpCode = @"
private string TemplateClickedItem;
";
}
