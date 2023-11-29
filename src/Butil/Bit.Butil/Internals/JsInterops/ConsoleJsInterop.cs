using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class ConsoleJsInterop
{
    internal static async Task Assert(this IJSRuntime js, bool? condition, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.assert", [condition, ..args]);
    }

    internal static async Task Clear(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.console.clear");
    }

    internal static async Task Count(this IJSRuntime js, string? label)
    {
        await js.InvokeVoidAsync("BitButil.console.count", label);
    }

    internal static async Task CountReset(this IJSRuntime js, string? label)
    {
        await js.InvokeVoidAsync("BitButil.console.countReset", label);
    }

    internal static async Task Debug(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.debug", args);
    }

    internal static async Task Dir(this IJSRuntime js, object? item, object? options)
    {
        await js.InvokeVoidAsync("BitButil.console.dir", item, options);
    }

    internal static async Task Dirxml(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.dirxml", args);
    }

    internal static async Task Error(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.error", args);
    }

    internal static async Task Group(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.group", args);
    }

    internal static async Task GroupCollapsed(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.groupCollapsed", args);
    }

    internal static async Task GroupEnd(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.console.groupEnd");
    }

    internal static async Task Info(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.info", args);
    }

    internal static async Task Log(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.log", args);
    }

    internal static async Task Memory(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.console.memory");
    }

    internal static async Task Profile(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.console.profile");
    }

    internal static async Task ProfileEnd(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.console.profileEnd");
    }

    internal static async Task Table(this IJSRuntime js, object? data, object? properties)
    {
        await js.InvokeVoidAsync("BitButil.console.table", data, properties);
    }

    internal static async Task Time(this IJSRuntime js, string? label)
    {
        await js.InvokeVoidAsync("BitButil.console.time", label);
    }

    internal static async Task TimeEnd(this IJSRuntime js, string? label)
    {
        await js.InvokeVoidAsync("BitButil.console.timeEnd", label);
    }

    internal static async Task TimeLog(this IJSRuntime js, string? label, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.timeLog", [label, ..args]);
    }

    internal static async Task TimeStamp(this IJSRuntime js, string? label)
    {
        await js.InvokeVoidAsync("BitButil.console.timeStamp", label);
    }

    internal static async Task Trace(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.trace", args);
    }

    internal static async Task Warn(this IJSRuntime js, params object?[]? args)
    {
        await js.InvokeVoidAsync("BitButil.console.warn", args);
    }
}
