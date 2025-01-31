namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the checkbox for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaLabelledby",
            Type = "string?",
            DefaultValue = "null",
            Description = "ID for element that contains label information for the checkbox.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            DefaultValue = "null",
            Description = "The position in the parent set (if in a set) for aria-posinset.",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total size of the parent set (if in a set) for aria-setsize.",
        },
        new()
        {
            Name = "CheckIconName",
            Type = "string",
            DefaultValue = "Accept",
            Description = "Custom icon for the check mark rendered by the checkbox instead of default check mark icon.",
        },
        new()
        {
            Name = "CheckIconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the content of checkbox(Label and Box).",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "DefaultIndeterminate",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default indeterminate visual state for checkbox.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.",
        },
        new()
        {
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on Value state.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Descriptive label for the checkbox.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the checkbox.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the checkbox clicked.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the label and checkbox location."
        },
        new()
        {
            Name = "Styles",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title text applied to the root element and the hidden checkbox input.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCheckboxClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCheckBox.",
                },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitCheckbox."
               },
               new()
               {
                   Name = "Checked",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checked state of the BitCheckbox."
               },
               new()
               {
                   Name = "Box",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the box element of the BitCheckbox."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitCheckbox."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitCheckbox."
               }
            ]
        }
    ];



    private bool oneWayValue;
    private bool twoWayValue;
    private bool oneWayIndeterminate = true;
    private bool twoWayIndeterminate = true;

    private bool customCheckboxValue;

    private bool customContentValue;
    private bool customContentIndeterminate = true;

    private string SuccessMessage = string.Empty;
    private BitCheckboxValidationModel validationModel = new();

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }
}
