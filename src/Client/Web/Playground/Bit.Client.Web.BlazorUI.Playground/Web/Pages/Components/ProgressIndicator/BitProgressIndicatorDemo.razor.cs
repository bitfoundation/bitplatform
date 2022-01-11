using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ProgressIndicator
{
    public partial class BitProgressIndicatorDemo
    {
        private string description = "Push button to start!";

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AriaValueText",
                Type = "string",
                DefaultValue = "",
                Description = "Text alternative of the progress status, used by screen readers for reading the value of the progress.",
            },
            new ComponentParameter()
            {
                Name = "BarHeight",
                Type = "int",
                DefaultValue = "2",
                Description = "Height of the ProgressIndicator.",
            },
            new ComponentParameter()
            {
                Name = "Description",
                Type = "string",
                DefaultValue = "",
                Description = "Text describing or supplementing the operation.",
            },
            new ComponentParameter()
            {
                Name = "IsProgressHidden",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not to hide the progress state.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Label to display above the component.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Custom label template to display above the component.",
            },
            new ComponentParameter()
            {
                Name = "PercentComplete",
                Type = "double",
                DefaultValue = "0",
                Description = "Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead.",
            },
            new ComponentParameter()
            {
                Name = "ProgressTemplate",
                Type = "RenderFragment<BitProgressIndicator>",
                DefaultValue = "",
                Description = "A custom template for progress track.",
            },
        };

        public int CompletedPercent { get; set; }

        private async Task StartProgress()
        {
            CompletedPercent = 0;

            while (CompletedPercent <= 100)
            {
                if (CompletedPercent == 100)
                {
                    description = $"Completed !";
                    break;
                }
                else
                {
                    CompletedPercent++;
                    description = $"{CompletedPercent}%";
                }

                StateHasChanged();
                await Task.Delay(100);
            }
        }

        private readonly string example1HTMLCode = @"<BitProgressIndicator Label=""Example title"" Description=""@description"" PercentComplete=""@CompletedPercent"" BarHeight=""50""></BitProgressIndicator>
<div>
    <BitButton OnClick=""@StartProgress"">Start Progress</BitButton>
</div>";

        private readonly string example1CSharpCode = @"@code {
    private string description = ""Push button to start!"";
    public int CompletedPercent { get; set; }
    private async Task StartProgress()
    {
            CompletedPercent = 0;
            while &#40;CompletedPercent &lt= 100)
            {
                    if &#40;CompletedPercent == 100)
                    {
                            description = $""Completed !"";
                            break;
                    }
                    else
                    {
                            CompletedPercent++;
                            description = $""{CompletedPercent}%"";
                    }

                    StateHasChanged();
                    await Task.Delay(100);
            }
    }
}";

        private readonly string example2HTMLCode = @"<BitProgressIndicator Label=""Example title"" Description=""Example description"" BarHeight=""20""></BitProgressIndicator>";
    }
}
