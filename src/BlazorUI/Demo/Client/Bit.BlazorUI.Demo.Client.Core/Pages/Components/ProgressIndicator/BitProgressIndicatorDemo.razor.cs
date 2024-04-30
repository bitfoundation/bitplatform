namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ProgressIndicator;

public partial class BitProgressIndicatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text alternative of the progress status, used by screen readers for reading the value of the progress.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitProgressIndicatorClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressIndicator-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProgressIndicator.",
        },
        new()
        {
            Name = "BarColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "Color of the BitProgressIndicator.",
        },
        new()
        {
            Name = "BarHeight",
            Type = "int",
            DefaultValue = "2",
            Description = "Height of the BitProgressIndicator.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text describing or supplementing the operation.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "Label to display above the BitProgressIndicator.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label template to display above the BitProgressIndicator.",
        },
        new()
        {
            Name = "PercentComplete",
            Type = "double?",
            DefaultValue = "null",
            Description = "Percentage of the operation's completeness, numerically between 0 and 100. If this is not set, the indeterminate progress animation will be shown instead.",
        },
        new()
        {
            Name = "ProgressTemplate",
            Type = "RenderFragment<BitProgressIndicator>?",
            DefaultValue = "null",
            Description = "A custom template for progress track.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProgressIndicatorClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressIndicator-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS Styles for different parts of the BitProgressIndicator.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "progressIndicator-class-styles",
            Title = "BitProgressIndicatorClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgressIndicator."
               },
               new()
               {
                   Name = "LabelContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label container of the BitProgressIndicator."
               },
               new()
               {
                   Name = "IndicatorWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the indicator wrapper of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Tracker",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tracker of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Bar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar of the BitProgressIndicator."
               },
               new()
               {
                   Name = "DescriptionContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description container of the BitProgressIndicator."
               }
            }
        }
    ];

    private int completedPercent;
    private int completedPercentStyleClass;
    private int completedPercentStylesClasses;
    private int completedPercentRtl;
    private string description = "Push button to start!";

    private async Task StartProgress()
    {
        completedPercent = 0;

        while (completedPercent <= 100)
        {
            if (completedPercent == 100)
            {
                description = "Completed !";
                break;
            }
            else
            {
                completedPercent++;
                description = $"{completedPercent}%";
            }

            StateHasChanged();
            await Task.Delay(100);
        }
    }

    private async Task StartProgressStyleClass()
    {
        completedPercentStyleClass = 0;

        while (completedPercentStyleClass <= 100)
        {
            if (completedPercentStyleClass == 100)
            {
                break;
            }
            else
            {
                completedPercentStyleClass++;
            }

            StateHasChanged();
            await Task.Delay(100);
        }
    }

    private async Task StartProgressStylesClasses()
    {
        completedPercentStylesClasses = 0;

        while (completedPercentStylesClasses <= 100)
        {
            if (completedPercentStylesClasses == 100)
            {
                break;
            }
            else
            {
                completedPercentStylesClasses++;
            }

            StateHasChanged();
            await Task.Delay(100);
        }
    }

    private async Task StartProgressRtl()
    {
        completedPercentRtl = 0;

        while (completedPercentRtl <= 100)
        {
            if (completedPercentRtl == 100)
            {
                break;
            }
            else
            {
                completedPercentRtl++;
            }

            StateHasChanged();
            await Task.Delay(100);
        }
    }


    private readonly string example1RazorCode = @"
<BitProgressIndicator Label=""Example title""
                                  Description=""@description""
                                  PercentComplete=""@completedPercent""
                                  BarHeight=""50"" />

<BitButton OnClick=""StartProgress"">Start Progress</BitButton>";
    private readonly string example1CsharpCode = @"
private int completedPercent;
private string description = ""Push button to start!"";

private async Task StartProgress()
{
    completedPercent = 0;

    while (completedPercent <= 100)
    {
        if (completedPercent == 100)
        {
            description = ""Completed !"";
            break;
        }
        else
        {
            completedPercent++;
            description = $""{completedPercent}%"";
        }

        StateHasChanged();
        await Task.Delay(100);
    }
}";

    private readonly string example2RazorCode = @"
<BitProgressIndicator Label=""Example title""
                      Description=""Example description"" 
                      BarHeight=""20"" />";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        background-color: darkred;
        border-radius: 0.5rem;
        padding: 0.2rem;
        margin-bottom: 1rem;
    }

    .custom-tracker {
        background-color: #ff6a00;
    }

    .custom-bar {
        background-color: #ff2700;
    }
</style>

<BitProgressIndicator Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" BarHeight=""20"" />

<BitProgressIndicator Class=""custom-class""
                      PercentComplete=""@completedPercentStyleClass""
                      BarHeight=""20"" />
<BitButton OnClick=""StartProgressStyleClass"">Start Progress</BitButton>


<BitProgressIndicator Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"" ,
                                      Tracker = ""background-color: green;"" })""
                      BarHeight=""20"" />

<BitProgressIndicator Classes=""@(new() { Bar = ""custom-bar"",
                                          Tracker = ""custom-tracker""})""
                      PercentComplete=""@completedPercentStylesClasses""
                      BarHeight=""20"" />
<BitButton OnClick=""StartProgressStylesClasses"">Start Progress</BitButton>";
    private readonly string example3CsharpCode = @"
private int completedPercentStyleClass;
private int completedPercentStylesClasses;

private async Task StartProgressStyleClass()
{
    completedPercentStyleClass = 0;

    while (completedPercentStyleClass <= 100)
    {
        if (completedPercentStyleClass == 100)
        {
            break;
        }
        else
        {
            completedPercentStyleClass++;
        }

        StateHasChanged();
        await Task.Delay(100);
    }
}

private async Task StartProgressStylesClasses()
{
    completedPercentStylesClasses = 0;

    while (completedPercentStylesClasses <= 100)
    {
        if (completedPercentStylesClasses == 100)
        {
            break;
        }
        else
        {
            completedPercentStylesClasses++;
        }

        StateHasChanged();
        await Task.Delay(100);
    }
}";

    private readonly string example4RazorCode = @"
<BitProgressIndicator Dir=""BitDir.Rtl""
                      BarHeight=""20"" />


<BitProgressIndicator Dir=""BitDir.Rtl""
                      PercentComplete=""@completedPercentRtl""
                      BarHeight=""20"" />
<BitButton OnClick=""StartProgressRtl"">Start Progress</BitButton>";
    private readonly string example4CsharpCode = @"
private int completedPercentRtl;

private async Task StartProgressRtl()
{
    completedPercentRtl = 0;

    while (completedPercentRtl <= 100)
    {
        if (completedPercentRtl == 100)
        {
            break;
        }
        else
        {
            completedPercentRtl++;
        }

        StateHasChanged();
        await Task.Delay(100);
    }
}";
}
