using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ProgressIndicator;

public partial class BitProgressIndicatorDemo
{

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaValueText",
            Type = "string",
            DefaultValue = "",
            Description = "Text alternative of the progress status, used by screen readers for reading the value of the progress.",
        },
        new()
        {
            Name = "BarHeight",
            Type = "int",
            DefaultValue = "2",
            Description = "Height of the ProgressIndicator.",
        },
        new()
        {
            Name = "Description",
            Type = "string",
            DefaultValue = "",
            Description = "Text describing or supplementing the operation.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Custom template for describing or supplementing the operation.",
        },
        new()
        {
            Name = "IsProgressHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to hide the progress state.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Label to display above the component.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Custom label template to display above the component.",
        },
        new()
        {
            Name = "PercentComplete",
            Type = "double",
            DefaultValue = "0",
            Description = "Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead.",
        },
        new()
        {
            Name = "ProgressTemplate",
            Type = "RenderFragment<BitProgressIndicator>",
            DefaultValue = "",
            Description = "A custom template for progress track.",
        },
        new()
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

    private int CompletedPercent;
    private string Description = "Push button to start!";

    private async Task StartProgress()
    {
        CompletedPercent = 0;

        while (CompletedPercent <= 100)
        {
            if (CompletedPercent == 100)
            {
                Description = $"Completed !";
                break;
            }
            else
            {
                CompletedPercent++;
                Description = $"{CompletedPercent}%";
            }

            StateHasChanged();
            await Task.Delay(100);
        }
    }

    private readonly string example1HTMLCode = @"
<BitProgressIndicator Label=""Example title""
                      Description=""@Description""
                      PercentComplete=""@CompletedPercent""
                      BarHeight=""50"" />
<div>
    <BitButton OnClick=""@StartProgress"">Start Progress</BitButton>
</div>";
    private readonly string example1CSharpCode = @"
private int CompletedPercent;
private string Description = ""Push button to start!"";
private async Task StartProgress()
{
    CompletedPercent = 0;
    while (CompletedPercent <= 100)
    {
        if (CompletedPercent == 100)
        {
                Description = $""Completed !"";
                break;
        }
        else
        {
                CompletedPercent++;
                Description = $""{CompletedPercent}%"";
        }

        StateHasChanged();
        await Task.Delay(100);
    }
}";

    private readonly string example2HTMLCode = @"
<BitProgressIndicator Label=""Example title""
                      Description=""Example description"" 
                      BarHeight=""20"" />
";
}
