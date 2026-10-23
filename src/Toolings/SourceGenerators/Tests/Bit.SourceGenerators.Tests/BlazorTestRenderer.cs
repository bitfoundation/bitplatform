using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.SourceGenerators.Tests;

internal static class BlazorTestRenderer
{
    public static Task<string> RenderAsync(Type componentType, IDictionary<string, object?>? parameters = null, Action<IServiceCollection>? configureServices = null)
    {
        return RenderCoreAsync(componentType, ParameterView.FromDictionary(parameters ?? new Dictionary<string, object?>()), configureServices);
    }

    public static Task<string> RenderWithCascadingValueAsync<TValue>(TValue value, Type componentType, IDictionary<string, object?>? parameters = null)
    {
        RenderFragment childContent = builder =>
        {
            builder.OpenComponent(0, componentType);
            foreach (var parameter in parameters ?? new Dictionary<string, object?>())
            {
                builder.AddComponentParameter(1, parameter.Key, parameter.Value);
            }
            builder.CloseComponent();
        };

        return RenderCoreAsync(typeof(CascadingValue<TValue>), ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(CascadingValue<TValue>.Value)] = value,
            [nameof(CascadingValue<TValue>.ChildContent)] = childContent
        }), null);
    }

    private static async Task<string> RenderCoreAsync(Type componentType, ParameterView parameters, Action<IServiceCollection>? configureServices)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(serviceProvider, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(componentType, parameters);
            return output.ToHtmlString();
        });
    }
}
