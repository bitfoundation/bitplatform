using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Legacy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Renders a component the way a server prerenders it, or a static SSR page renders it for good: through an
/// <see cref="HtmlRenderer"/>, which runs OnInitialized and OnParametersSet but never OnAfterRender, with a JS
/// runtime that is named and throws like the one a prerender is handed (IsRuntimeInvalid tells it apart by that
/// name). What it returns is the HTML a visitor sees before the page is interactive, which bUnit cannot show,
/// since it always runs the after-render pass.
/// </summary>
public static class Prerenderer
{
    public static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?>? parameters = null,
                                                             Action<IServiceCollection>? configureServices = null)
        where TComponent : IComponent
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IJSRuntime, UnsupportedJavaScriptRuntime>();
        services.AddSingleton<NavigationManager, PrerenderNavigationManager>();
        services.AddBitBlazorUIServices();
        services.AddBitBlazorUIExtrasServices();
        services.AddBitBlazorUILegacyServices();
        configureServices?.Invoke(services);

        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var view = parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters);
            var output = await renderer.RenderComponentAsync<TComponent>(view);
            await output.QuiescenceTask;
            return output.ToHtmlString();
        });
    }

    private sealed class UnsupportedJavaScriptRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time, because the component is being statically rendered.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class PrerenderNavigationManager : NavigationManager
    {
        public PrerenderNavigationManager() => Initialize("https://localhost/", "https://localhost/");
    }
}
