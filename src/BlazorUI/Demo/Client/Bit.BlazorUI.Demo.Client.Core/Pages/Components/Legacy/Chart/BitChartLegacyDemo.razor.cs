using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Legacy.Chart;

public partial class BitChartLegacyDemo
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
            Type = "BitChartLegacyConfigBase",
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
            Title="BitChartLegacyConfigBase",
            Parameters=
            [
                new()
                {
                    Name = "Type",
                    Type = "BitChartLegacyChartType",
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

    // BitChartLegacy serializes its config with Newtonsoft.Json, so the samples need assemblies that the
    // WebAssembly client keeps out of its initial payload through its BlazorWebAssemblyLazyLoad items. They
    // are fetched on demand here, before the samples render. The client's project file hands this option the
    // marked assemblies, joined by ';', and an empty value in an AOT build, which marks none and loads them
    // with the app. A build check there keeps its copy of the name in step with this one.
    private const string LazyAssembliesOption = "Bit.BlazorUI.Demo.LazyLoadedAssemblies";

    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected async override Task OnInitializedAsync()
    {
        try
        {
            // Only the WebAssembly client sets the option: every other host (Blazor Server, MAUI, Windows)
            // has the assemblies already. In the browser a missing value means the project file sets the
            // option under another name, and the samples would fail to serialize, so it throws here instead.
            if (OperatingSystem.IsBrowser())
            {
                var lazyAssemblies = AppContext.GetData(LazyAssembliesOption) as string
                    ?? throw new InvalidOperationException($"The runtime option '{LazyAssembliesOption}' is not set. Bit.BlazorUI.Demo.Client.Web.csproj sets it with the assemblies it lazy-loads.");

                var names = lazyAssemblies.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (names.Length > 0)
                {
                    await lazyAssemblyLoader.LoadAssembliesAsync(names);
                }
            }
        }
        finally
        {
            isLoadingAssemblies = false;
        }

        await base.OnInitializedAsync();
    }
}
