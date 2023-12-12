using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Console(IJSRuntime js)
{
    public async Task Assert(bool? condition, params object?[]? args)
        => await js.ConsoleAssert(condition, args);

    public async Task Clear()
        => await js.ConsoleClear();

    public async Task Count(string? label)
        => await js.ConsoleCount(label);

    public async Task CountReset(string? label)
        => await js.ConsoleCountReset(label);

    public async Task Debug(params object?[]? args)
        => await js.ConsoleDebug(args);

    public async Task Dir(object? item, object? options)
        => await js.ConsoleDir(item, options);

    public async Task Dirxml(params object?[]? args)
        => await js.ConsoleDirxml(args);

    public async Task Error(params object?[]? args)
        => await js.ConsoleError(args);

    public async Task Group(params object?[]? args)
        => await js.ConsoleGroup(args);

    public async Task GroupCollapsed(params object?[]? args)
        => await js.ConsoleGroupCollapsed(args);

    public async Task GroupEnd()
        => await js.ConsoleGroupEnd();

    public async Task Info(params object?[]? args)
        => await js.ConsoleInfo(args);

    public async Task Log(params object?[]? args)
        => await js.ConsoleLog(args);

    public async Task Memory()
        => await js.ConsoleMemory();

    public async Task Profile()
        => await js.ConsoleProfile();

    public async Task ProfileEnd()
        => await js.ConsoleProfileEnd();

    public async Task Table(object? data, object? properties)
        => await js.ConsoleTable(data, properties);

    public async Task Time(string? label)
        => await js.ConsoleTime(label);

    public async Task TimeEnd(string? label)
        => await js.ConsoleTimeEnd(label);

    public async Task TimeLog(string? label, params object?[]? args)
        => await js.ConsoleTimeLog(label, args);

    public async Task TimeStamp(string? label)
        => await js.ConsoleTimeStamp(label);

    public async Task Trace(params object?[]? args)
        => await js.ConsoleTrace(args);

    public async Task Warn(params object?[]? args)
        => await js.ConsoleWarn(args);
}
