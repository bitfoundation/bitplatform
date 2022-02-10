using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.SpinButton
{
    public partial class BitSpinButtonDemo
    {
        private double BasicSpinButtonValue = 5;
        private double BasicSpinButtonDisableValue = 20;
        private double SpinButtonWithLabelAboveValue = 7;
        private double BitSpinButtonBindValue = 8;
        private double BitSpinButtonValueChanged = 16;

        private void HandleControlledSpinButtonValueChange(double value)
        {
            BitSpinButtonValueChanged = value;
        }

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
                Type = "BitIconName",
                DefaultValue = "BitIconName.ChevronDownSmall",
                Description = "Custom icon name for the decrement button.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "BitIconName",
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
                Type = "BitIconName",
                DefaultValue = "BitIconName.ChevronUpSmall",
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
            new ComponentParameter()
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
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
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Left",
                        Description="The label shows on the left side of the spin button.",
                        Value="1",
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

        private readonly string example1HTMLCode = @"<BitSpinButton @bind-Value=""BasicSpinButtonValue""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Basic SpinButton"">
</BitSpinButton>
<BitSpinButton Min=""-10""
               Max=""10""
               Step=""0.1""
               Label=""Decimal SpinButton"">
</BitSpinButton>
<BitSpinButton @bind-Value=""BasicSpinButtonDisableValue""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Disabled SpinButton""
               IsEnabled=""false"">
</BitSpinButton>";

        private readonly string example1CSharpCode = @"
@code {
    private double BasicSpinButtonValue = 5;
    private double BasicSpinButtonDisableValue = 20;
}";

        private readonly string example2HTMLCode = @"<BitSpinButton IconName=""BitIconName.IncreaseIndentLegacy""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With Icon"">
</BitSpinButton>
<BitSpinButton IconName=""BitIconName.IncreaseIndentLegacy""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Disabled With Icon""
               IsEnabled=""false"">
</BitSpinButton>";

        private readonly string example3HTMLCode = @"<BitSpinButton Suffix=""Inch""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With suffix"">
</BitSpinButton>";

        private readonly string example4HTMLCode = @"<BitSpinButton @bind-Value=""SpinButtonWithLabelAboveValue""
               Suffix=""cm""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With Labal Above""
               LabelPosition=""@BitSpinButtonLabelPosition.Top"">
</BitSpinButton>";

        private readonly string example4CSharpCode = @"
@code {
    private double SpinButtonWithLabelAboveValue = 7;
}";

        private readonly string example5HTMLCode = @"<BitSpinButton Class=""custom-spb""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Custom Styled"">
</BitSpinButton>

<style>
    .custom-spb {
            .bit-spb-wrapper input {
                    background-color: #D7D7D7;
            }
    }
</style>";

        private readonly string example6CSharpCode = @"
@code {
    private double BitSpinButtonBindValue = 8;
    private double BitSpinButtonValueChanged = 16;        
    private void HandleControlledSpinButtonValueChange(double value)
    {
            BitSpinButtonValueChanged = value;
    } 
}";

        private readonly string example6HTMLCode = @"<BitSpinButton Label=""Controlled SpinButton with bind-value""
               @bind-Value=""BitSpinButtonBindValue""
               Min=""0""
               Max=""100""
               Step=""1"">
</BitSpinButton>
<BitSpinButton Label=""Controlled SpinButton with Value""
               Value=""BitSpinButtonValueChanged""
               ValueChanged=""HandleControlledSpinButtonValueChange""
               Min=""0""
               Max=""100""
               Step=""1"">
</BitSpinButton>";
    }
}
