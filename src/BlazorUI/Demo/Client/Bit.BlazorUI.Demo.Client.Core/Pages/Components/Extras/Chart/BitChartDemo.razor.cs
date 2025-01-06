using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class BitChartDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
        {
            Name = "SetupCompletedCallback",
            Type = "EventCallback",
            Description = "This event is fired when the chart has been setup through interop and the JavaScript chart object is available. Use this callback if you need to setup custom JavaScript options or register plugins.",
        },

         new()
        {
            Name = "Config",
            Type = "BitChartConfigBase",
            Description = "Gets or sets the configuration of the chart.",
            LinkType = LinkType.Link,
            Href = "#chart-config"
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
            Description = "Gets or sets the height of the canvas HTML element.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id="chart-config",
            Title="BitChartConfigBase",
            Parameters=
            [
                new()
                {
                    Name = "Type",
                    Type = "BitChartChartType",
                    DefaultValue = "null",
                    Description = "Gets the type of chart this config is for."
                },
                new()
                {
                    Name = "CanvasId",
                    Type = "string",
                    DefaultValue = "Guid.NewGuid().ToString()",
                    Description = "Gets the id for the html canvas element associated with this chart."
                },
                new()
                {
                    Name = "Plugins",
                    Type = "IList<object>",
                    DefaultValue = "new List<object>()",
                    Description = "Gets the list of inline plugins for this chart."
                },
            ]
        }
    ];

    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected async override Task OnInitializedAsync()
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm", "System.Private.Xml.wasm", "System.Data.Common.wasm"]);
            }
        }
        finally
        {
            isLoadingAssemblies = false;
        }

        await base.OnInitializedAsync();
    }
}
