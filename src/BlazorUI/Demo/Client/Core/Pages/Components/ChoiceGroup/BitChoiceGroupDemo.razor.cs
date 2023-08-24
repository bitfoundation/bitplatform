namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ChoiceGroup;

public partial class BitChoiceGroupDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaLabelledBy",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Id of an element to use as the aria label for the ChoiceGroup."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the ChoiceGroup, a list of BitChoiceGroupOption components."
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default selected Value for ChoiceGroup."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Sets the data source that populates the items of the list."
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "Used to customize the label for the Item content."
        },
        new()
        {
            Name = "ItemLabelTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize the label for the Item Label content."
        },
        new()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, selecting an option is mandatory in the ChoiceGroup."
        },
        new()
        {
            Name = "IsRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change direction to RTL."
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label for the ChoiceGroup."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom RenderFragment for the label of the ChoiceGroup."
        },
        new()
        {
            Name = "LayoutFlow",
            Type = "BitLayoutFlow?",
            DefaultValue = "null",
            Description = "The render flow of the items in the ChoiceGroup, Horizontal or Vertical."
        },
        new()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "Guid.NewGuid().ToString()",
            Description = "Name of the ChoiceGroup, this unique name is used to group each item into the same logical component."
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitChoiceGroupNameSelectors<TItem, TValue>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the option clicked.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the option has been changed.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent."
        },
    };
    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "choice-group-item",
            Title = "BitChoiceGroupItem",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon to show as Option content.",
               },
               new()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The image address to show as Option content.",
               },
               new()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides alternative information for the Option image.",
               },
               new()
               {
                   Name = "ImageSize",
                   Type = "Size?",
                   DefaultValue = "null",
                   Description = "Provides Height and Width for the Option image.",
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Set attribute of Id for the GroupOption Option input.",
               },
               new()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides a new image for the selected Option in the Image-GroupOption.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to show as content of GroupOption Option.",
               },
               new()
               {
                   Name = "Value",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "This value is returned when GroupOption Option is Clicked.",
               }
            }
        },
        new()
        {
            Id = "choice-group-option",
            Title = "BitChoiceGroupOption",
            Parameters = new()
            {
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon to show as Option content.",
               },
               new()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The image address to show as Option content.",
               },
               new()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides alternative information for the Option image.",
               },
               new()
               {
                   Name = "ImageSize",
                   Type = "Size?",
                   DefaultValue = "null",
                   Description = "Provides Height and Width for the Option image.",
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Set attribute of Id for the GroupOption Option input.",
               },
               new()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides a new image for the selected Option in the Image-GroupOption.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to show as content of GroupOption Option.",
               },
               new()
               {
                   Name = "Value",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "This value is returned when GroupOption Option is Clicked.",
               }
            }
        }
    };
}
