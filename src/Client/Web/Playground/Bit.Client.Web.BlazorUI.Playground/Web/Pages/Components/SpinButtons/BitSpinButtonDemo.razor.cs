using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SpinButtons
{
    public partial class BitSpinButtonDemo
    {
        private double BasicSpinButtonValue = 5;
        private double BasicSpinButtonDisableValue = 20;
        private double SpinButtonWithCustomHandlerValue = 14;
        private double SpinButtonWithLabelAboveValue = 7;
        private double BitSpinButtonBindValue = 8;
        private double BitSpinButtonValueChanged = 16;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the input for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "AriaPositionInSet",
                Type = "int",
                DefaultValue = "0",
                Description = "The position in the parent set (if in a set).",
            },
            new ComponentParameter()
            {
                Name = "AriaSetSize",
                Type = "int",
                DefaultValue = "0",
                Description = "The total size of the parent set (if in a set).",
            },
            new ComponentParameter()
            {
                Name = "AriaValueNow",
                Type = "double",
                DefaultValue = "0",
                Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
            },
            new ComponentParameter()
            {
                Name = "AriaValueText",
                Type = "string",
                DefaultValue = "",
                Description = "Sets the control's aria-valuetext.",
            },
            new ComponentParameter()
            {
                Name = "DecrementButtonAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Accessible label text for the decrement button (for screen reader users).",
            },
            new ComponentParameter()
            {
                Name = "DefaultValue",
                Type = "double",
                DefaultValue = "0",
                Description = "Initial value of the spin button.",
            },
            new ComponentParameter()
            {
                Name = "DecrementButtonIconName",
                Type = "string",
                DefaultValue = "ChevronDownSmall",
                Description = "Custom icon name for the decrement button.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "string",
                DefaultValue = "",
                Description = "Icon name for an icon to display alongside the spin button's label.",
            },
            new ComponentParameter()
            {
                Name = "IconAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "The aria label of the icon for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "IncrementButtonAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Accessible label text for the increment button (for screen reader users).",
            },
            new ComponentParameter()
            {
                Name = "IncrementButtonIconName",
                Type = "string",
                DefaultValue = "ChevronUpSmall",
                Description = "Custom icon name for the increment button.",
            },
            new ComponentParameter()
            {
                Name = "InputHtmlAttributes",
                Type = "Dictionary<string, object>",
                DefaultValue = "",
                Description = "Additional props for the input field.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Descriptive label for the spin button, Label displayed above the spin button and read by screen readers.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Shows the custom Label for spin button. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id..",
            },
            new ComponentParameter()
            {
                Name = "LabelPosition",
                Type = "BitSpinButtonLabelPosition",
                LinkType = LinkType.Link,
                Href = "#labelPosition-enum",
                DefaultValue = "BitSpinButtonLabelPosition.Left",
                Description = "The position of the label in regards to the spin button.",
            },
            new ComponentParameter()
            {
                Name = "Max",
                Type = "double",
                DefaultValue = "0",
                Description = "Max value of the spin button. If not provided, the spin button has max value of double type.",
            },
            new ComponentParameter()
            {
                Name = "Min",
                Type = "double",
                DefaultValue = "0",
                Description = "Min value of the spin button. If not provided, the spin button has minimum value of double type.",
            },
            new ComponentParameter()
            {
                Name = "OnBlur",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the control loses focus.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback for when the spin button value change.",
            },
            new ComponentParameter()
            {
                Name = "OnDecrement",
                Type = "EventCallback<BitSpinButtonChangeEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the decrement button or down arrow key is pressed.",
            },
            new ComponentParameter()
            {
                Name = "OnIncrement",
                Type = "EventCallback<BitSpinButtonChangeEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the increment button or up arrow key is pressed.",
            },
            new ComponentParameter()
            {
                Name = "OnFocus",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the input.",
            },
            new ComponentParameter()
            {
                Name = "Precision",
                Type = "int",
                DefaultValue = "",
                Description = "How many decimal places the value should be rounded to.",
            },
            new ComponentParameter()
            {
                Name = "Step",
                Type = "double",
                DefaultValue = "1",
                Description = "Difference between two adjacent values of the spin button.",
            },
            new ComponentParameter()
            {
                Name = "Suffix",
                Type = "string",
                DefaultValue = "",
                Description = "A text is shown after the spin button value.",
            },
            new ComponentParameter()
            {
                Name = "Title",
                Type = "string",
                DefaultValue = "ChevronUpSmall",
                Description = "A more descriptive title for the control, visible on its tooltip.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "double",
                DefaultValue = "0",
                Description = "Current value of the spin button.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback for when the spin button value change.",
            }, 
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "labelPosition-enum",
                Title = "BitSpinButtonLabelPosition Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Top",
                        Description="The label shows on the top of the spin button.",
                        Value="Top = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Left",
                        Description="The label shows on the left side of the spin button.",
                        Value="Left = 1",
                    }
                }
            }
        };

        private void HandleSpinButtonValueChange(double value)
        {
            SpinButtonWithCustomHandlerValue = value;
        }

        private void HandleControlledSpinButtonValueChange(double value)
        {
            BitSpinButtonValueChanged = value;
        }
    }
}
