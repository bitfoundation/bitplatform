namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class BitButtonGroupDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The content inside the item can be customized.",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render ButtonGroup children vertically."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#button-group-items",
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
            Type = "BitButtonGroupNameSelectors<TItem>?",
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
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "button-group-items",
            Title = "BitButtonGroupItem",
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
            Id = "button-group-options",
            Title = "BitButtonGroupOption",
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
            Id = "name-selectors",
            Title = "BitButtonGroupNameSelectors",
            Parameters = new()
            {
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Template",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Template))",
                    Description = "Template field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Text))",
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
                },
                new()
                {
                    Name= "Text",
                    Description="The button for less-pronounced actions.",
                    Value="2",
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

    private readonly string example1RazorCode = @"
<BitButtonGroup TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example2RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption IsEnabled=""false"" Text=""Add (Disabled)"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example3RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption IsEnabled=""false"" Text=""Edit (Disabled)"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example4RazorCode = @"
<BitButtonGroup ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption IsEnabled=""false"" Text=""Delete (Disabled)"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example5RazorCode = @"
<div>Click count: @clickCounter</div>
<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption OnClick=""@(() => clickCounter++)"" Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption OnClick=""@(() => clickCounter = 0)"" Text=""Reset"" Key=""edit-key"" IconName=""@BitIconName.Refresh"" />
    <BitButtonGroupOption OnClick=""@(() => clickCounter--)"" Text=""Remove"" Key=""delete-key"" IconName=""@BitIconName.Remove"" />
</BitButtonGroup>";
    private readonly string example5CsharpCode = @"
private int clickCounter;";

    private readonly string example6RazorCode = @"
<BitButtonGroup Color=""BitButtonColor.Info"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Success"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Warning"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.SevereWarning"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>


<BitButtonGroup Color=""BitButtonColor.Error"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example7RazorCode = @"
<BitButtonGroup Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        background-color: goldenrod;
    }
</style>

<BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Style=""color:darkblue; font-weight:bold"" Text=""Styled"" Key=""add-key"" IconName=""@BitIconName.Brush"" />
    <BitButtonGroupOption Class=""custom-class"" Text=""Classed"" Key=""edit-key"" IconName=""@BitIconName.FormatPainter"" />
</BitButtonGroup>";

    private readonly string example9RazorCode = @"
<BitButtonGroup Vertical TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Standard"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>

<BitButtonGroup Vertical ButtonStyle=""BitButtonStyle.Text"" TItem=""BitButtonGroupOption"">
    <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitButtonGroupOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitButtonGroup>";

    private readonly string example10RazorCode = @"
Visible: [ <BitButtonGroup ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
               <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
               <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
               <BitButtonGroupOption Text=""Ok"" Key=""delete-key"" IconName=""@BitIconName.CheckMark"" />
           </BitButtonGroup> ]

Hidden: [ <BitButtonGroup Visibility=""BitVisibility.Hidden"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
              <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
              <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
              <BitButtonGroupOption Text=""Ok"" Key=""delete-key"" IconName=""@BitIconName.CheckMark"" />
          </BitButtonGroup> ]

Collapsed: [ <BitButtonGroup Visibility=""BitVisibility.Collapsed"" ButtonStyle=""BitButtonStyle.Primary"" TItem=""BitButtonGroupOption"">
                 <BitButtonGroupOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
                 <BitButtonGroupOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
                 <BitButtonGroupOption Text=""Ok"" Key=""delete-key"" IconName=""@BitIconName.CheckMark"" />
             </BitButtonGroup> ]";
}
