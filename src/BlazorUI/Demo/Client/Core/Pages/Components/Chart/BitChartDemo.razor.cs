namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class BitChartDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
         new()
        {
            Name = "SetupCompletedCallback",
            Type = "EventCallback",
            Description = @"This event is fired when the chart has been setup through interop and the JavaScript chart object is available. Use this callback if you need to setup custom JavaScript options or register plugins.",
        },

         new()
        {
            Name = "Config",
            Type = "BitChartConfigBase",
            Description = "Gets or sets the configuration of the chart.",
        },

         new()
        {
            Name = "Width",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the width of the canvas HTML element.",
        },

         new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "null",
            Description = @"Gets or sets the height of the canvas HTML element. Use <see langword=""null""/> when using <see cref=""BitChartBaseConfigOptions.AspectRatio""/>.",
        }
    };

}
