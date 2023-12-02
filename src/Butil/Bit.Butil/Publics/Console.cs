using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Console(IJSRuntime js)
{
    public async Task Assert(bool? condition, params object?[]? args)
    {
        await js.Assert(condition, args);
    }

    public async Task Clear()
    {
        await js.Clear();
    }

    public async Task Count(string? label)
    {
        await js.Count(label);
    }

    public async Task CountReset(string? label)
    {
        await js.CountReset(label);
    }

    public async Task Debug(params object?[]? args)
    {
        await js.Debug(args);
    }

    public async Task Dir(object? item, object? options)
    {
        await js.Dir(item, options);
    }

    public async Task Dirxml(params object?[]? args)
    {
        await js.Dirxml(args);
    }

    public async Task Error(params object?[]? args)
    {
        await js.Error(args);
    }

    public async Task Group(params object?[]? args)
    {
        await js.Group(args);
    }

    public async Task GroupCollapsed(params object?[]? args)
    {
        await js.GroupCollapsed(args);
    }

    public async Task GroupEnd()
    {
        await js.GroupEnd();
    }

    public async Task Info(params object?[]? args)
    {
        await js.Info(args);
    }

    public async Task Log(params object?[]? args)
    {
        await js.Log(args);
    }

    public async Task Memory()
    {
        await js.Memory();
    }

    public async Task Profile()
    {
        await js.Profile();
    }

    public async Task ProfileEnd()
    {
        await js.ProfileEnd();
    }

    public async Task Table(object? data, object? properties)
    {
        await js.Table(data, properties);
    }

    public async Task Time(string? label)
    {
        await js.Time(label);
    }

    public async Task TimeEnd(string? label)
    {
        await js.TimeEnd(label);
    }

    public async Task TimeLog(string? label, params object?[]? args)
    {
        await js.TimeLog(label, args);
    }

    public async Task TimeStamp(string? label)
    {
        await js.TimeStamp(label);
    }

    public async Task Trace(params object?[]? args)
    {
        await js.Trace(args);
    }

    public async Task Warn(params object?[]? args)
    {
        await js.Warn(args);
    }
}
