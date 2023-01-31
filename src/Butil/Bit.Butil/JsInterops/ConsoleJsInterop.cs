using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ConsoleJsInterop
{
    private static bool _isInitialized;
    private static IJSRuntime _js = default!;

    public static void Init(IJSRuntime jsRuntime)
    {
        if (_isInitialized) return;

        _isInitialized = true;
        _js = jsRuntime;
    }

    internal static void Assert(bool? condition, params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.assert", condition, args);
    }

    internal static void Clear()
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.clear");
    }

    internal static void Count(string? label)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.count", label);
    }

    internal static void CountReset(string? label)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.countReset", label);
    }

    internal static void Debug(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.debug", args);
    }

    internal static void Dir(object? item, object? options)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.dir", item, options);
    }

    internal static void Dirxml(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.dirxml", args);
    }

    internal static void Error(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.error", args);
    }

    internal static void Group(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.group", args);
    }

    internal static void GroupCollapsed(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.groupCollapsed", args);
    }

    internal static void GroupEnd()
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.groupEnd");
    }

    internal static void Info(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.info", args);
    }

    internal static void Log(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.log", args);
    }

    internal static void Memory()
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.memory");
    }

    internal static void Profile()
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.profile");
    }

    internal static void ProfileEnd()
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.profileEnd");
    }

    internal static void Table(object? data, object? properties)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.table", data, properties);
    }

    internal static void Time(string? label)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.time", label);
    }

    internal static void TimeEnd(string? label)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.timeEnd", label);
    }

    internal static void TimeLog(string? label, params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.timeLog", label, args);
    }

    internal static void TimeStamp(string? label)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.timeStamp", label);
    }

    internal static void Trace(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.trace", args);
    }

    internal static void Warn(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.warn", args);
    }
}
