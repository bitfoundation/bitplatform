using System;

namespace Bit.Butil;

public static class Console
{
    public static void Assert(bool? condition, params object?[]? args)
    {
        ConsoleJsInterop.Assert(condition, args);
    }

    public static void Clear()
    {
        ConsoleJsInterop.Clear();
    }

    public static void Count(string? label)
    {
        ConsoleJsInterop.Count(label);
    }

    public static void CountReset(string? label)
    {
        ConsoleJsInterop.CountReset(label);
    }

    public static void Debug(params object?[]? args)
    {
        ConsoleJsInterop.Debug(args);
    }

    public static void Dir(object? item, object? options)
    {
        ConsoleJsInterop.Dir(item, options);
    }

    public static void Dirxml(params object?[]? args)
    {
        ConsoleJsInterop.Dirxml(args);
    }

    public static void Error(params object?[]? args)
    {
        ConsoleJsInterop.Error(args);
    }

    public static void Group(params object?[]? args)
    {
        ConsoleJsInterop.Group(args);
    }

    public static void GroupCollapsed(params object?[]? args)
    {
        ConsoleJsInterop.GroupCollapsed(args);
    }

    public static void GroupEnd()
    {
        ConsoleJsInterop.GroupEnd();
    }

    public static void Info(params object?[]? args)
    {
        ConsoleJsInterop.Info(args);
    }

    public static void Log(params object?[]? args)
    {
        ConsoleJsInterop.Log(args);
    }

    public static void Memory()
    {
        ConsoleJsInterop.Memory();
    }

    public static void Profile()
    {
        ConsoleJsInterop.Profile();
    }

    public static void ProfileEnd()
    {
        ConsoleJsInterop.ProfileEnd();
    }

    public static void Table(object? data, object? properties)
    {
        ConsoleJsInterop.Table(data, properties);
    }

    public static void Time(string? label)
    {
        ConsoleJsInterop.Time(label);
    }

    public static void TimeEnd(string? label)
    {
        ConsoleJsInterop.TimeEnd(label);
    }

    public static void TimeLog(string? label, params object?[]? args)
    {
        ConsoleJsInterop.TimeLog(label, args);
    }

    public static void TimeStamp(string? label)
    {
        ConsoleJsInterop.TimeStamp(label);
    }

    public static void Trace(params object?[]? args)
    {
        ConsoleJsInterop.Trace(args);
    }

    public static void Warn(params object?[]? args)
    {
        ConsoleJsInterop.Warn(args);
    }
}
