namespace Boilerplate.Tests.Extensions;

public static class PlaywrightInitialScriptExtensions
{
    public static async Task SetBlazorWebAssemblyServerAddress(this IBrowserContext context, string serverAddress)
    {
        await context.SetBlazorWebAssemblyConfiguration(new() { ["ServerAddress"] = serverAddress });
    }

    public static async Task SetBlazorWebAssemblyConfiguration(this IBrowserContext context, Dictionary<string, string?> configs)
    {
        //Pass configuration to BlazorWebAssembly
        //More info: https://stackoverflow.com/questions/60831359/how-are-string-args-passed-to-program-main-in-a-blazor-webassembly-app

        var arrayString = string.Join(',', configs.Select(p => $"'{p.Key}={p.Value}'"));
        await context.AddInitScriptAsync($"window.startupParams = function() {{ return [ {arrayString} ]; }};");
    }
}
