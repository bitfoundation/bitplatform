using System.Collections.Generic;
using System.Threading.Tasks;
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
            Type = "RenderFragment<BitMenuButtonItem>?",
            Description = "The content inside the item can be customized.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "List<BitMenuButtonItem>",
            LinkType = LinkType.Link,
            Href = "#menu-button-items",
            DefaultValue = "new List<BitMenuButtonItem>()",
            Description = "List of Item, each of which can be a Button with different action in the MenuButton."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitMenuButtonItem>?",
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
            Type = "EventCallback<BitMenuButtonItem>",
            Description = "OnClick of each item returns that item with its property."
        },
        new ComponentParameter
        {
            Name = "Text",
            Type = "string?",
            Description = "The text to show inside the header of MenuButton."
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
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

    protected override async Task OnInitializedAsync()
    {
        example1HTMLCode = example1InternalItemHTMLCode;
        example1CSharpCode = example1InternalItemCSharpCode;

        example2HTMLCode = example2InternalItemHTMLCode;
        example2CSharpCode = example2InternalItemCSharpCode;

        example3HTMLCode = example3InternalItemHTMLCode;
        example3CSharpCode = example3InternalItemCSharpCode;

        example4HTMLCode = example4InternalItemHTMLCode;
        example4CSharpCode = example4InternalItemCSharpCode;

        example5HTMLCode = example5InternalItemHTMLCode;
        example5CSharpCode = example5InternalItemCSharpCode;

        await base.OnInitializedAsync();
    }

    private void SetImplementationType(BitPivotItem item)
    {
        if (item.Key == "internal-item")
        {
            example1HTMLCode = example1InternalItemHTMLCode;
            example1CSharpCode = example1InternalItemCSharpCode;

            example2HTMLCode = example2InternalItemHTMLCode;
            example2CSharpCode = example2InternalItemCSharpCode;

            example3HTMLCode = example3InternalItemHTMLCode;
            example3CSharpCode = example3InternalItemCSharpCode;

            example4HTMLCode = example4InternalItemHTMLCode;
            example4CSharpCode = example4InternalItemCSharpCode;

            example5HTMLCode = example5InternalItemHTMLCode;
            example5CSharpCode = example5InternalItemCSharpCode;
        }
        else if (item.Key == "external-item")
        {
            example1HTMLCode = example1ExternalItemHTMLCode;
            example1CSharpCode = example1ExternalItemCSharpCode;

            example2HTMLCode = example2ExternalItemHTMLCode;
            example2CSharpCode = example2ExternalItemCSharpCode;

            example3HTMLCode = example3ExternalItemHTMLCode;
            example3CSharpCode = example3ExternalItemCSharpCode;

            example4HTMLCode = example4ExternalItemHTMLCode;
            example4CSharpCode = example4ExternalItemCSharpCode;

            example5HTMLCode = example5ExternalItemHTMLCode;
            example5CSharpCode = example5ExternalItemCSharpCode;
        }
        else if (item.Key == "razor-item")
        {
            example1HTMLCode = example1RazorItemHTMLCode;
            example1CSharpCode = example1RazorItemCSharpCode;

            example2HTMLCode = example2RazorItemHTMLCode;
            example2CSharpCode = example2RazorItemCSharpCode;

            example3HTMLCode = example3RazorItemHTMLCode;
            example3CSharpCode = example3RazorItemCSharpCode;

            example4HTMLCode = example4RazorItemHTMLCode;
            example4CSharpCode = example4RazorItemCSharpCode;

            example5HTMLCode = example5RazorItemHTMLCode;
            example5CSharpCode = example5RazorItemCSharpCode;
        }

        StateHasChanged();
    }

    private string example1HTMLCode;
    private string example1CSharpCode;

    private string example2HTMLCode;
    private string example2CSharpCode;

    private string example3HTMLCode;
    private string example3CSharpCode;

    private string example4HTMLCode;
    private string example4CSharpCode;

    private string example5HTMLCode;
    private string example5CSharpCode;

    private readonly string example1InternalItemHTMLCode = @"
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
    private readonly string example1ExternalItemHTMLCode = @"
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
    private readonly string example1RazorItemHTMLCode = @"
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

    <BitMenuButton Text=""Option Disabled""
                    OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""BitIconName.Emoji"" IsEnabled=""false"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""BitIconName.Emoji2"" IsEnabled=""false"" />
    </BitMenuButton>
</div>
<div class=""clicked-item"">Clicked Item: @example1SelectedItem</div>
";
    private readonly string example1InternalItemCSharpCode = @"
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
    private readonly string example1ExternalItemCSharpCode = @"
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
    private readonly string example1RazorItemCSharpCode = @"
private string example1SelectedItem;
";

    private readonly string example2InternalItemHTMLCode = @"
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
    private readonly string example2ExternalItemHTMLCode = @"
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
    private readonly string example2RazorItemHTMLCode = @"
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
    private readonly string example2InternalItemCSharpCode = @"
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
    private readonly string example2ExternalItemCSharpCode = @"
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
    private readonly string example2RazorItemCSharpCode = @"
private string example2SelectedItem;
";

    private readonly string example3InternalItemHTMLCode = @"
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
    private readonly string example3ExternalItemHTMLCode = @"
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
    private readonly string example3RazorItemHTMLCode = @"
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
    private readonly string example3InternalItemCSharpCode = @"
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
    private readonly string example3ExternalItemCSharpCode = @"
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
    private readonly string example3RazorItemCSharpCode = @"
private string example3SelectedItem;
";

    private readonly string example4InternalItemHTMLCode = @"
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
    private readonly string example4ExternalItemHTMLCode = @"
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
    private readonly string example4RazorItemHTMLCode = @"
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
    private readonly string example4InternalItemCSharpCode = @"
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
    private readonly string example4ExternalItemCSharpCode = @"
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
    private readonly string example4RazorItemCSharpCode = @"
private string example4SelectedItem;
";

    private readonly string example5InternalItemHTMLCode = @"
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
    private readonly string example5ExternalItemHTMLCode = @"
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
    private readonly string example5RazorItemHTMLCode = @"
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
    private readonly string example5InternalItemCSharpCode = @"
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
    private readonly string example5ExternalItemCSharpCode = @"
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
    private readonly string example5RazorItemCSharpCode = @"
private string example5SelectedItem;
";
}
