using System;

namespace Bit.Butil;

public static class Console
{
    public static void Log(params object?[]? data)
    {
        ConsoleJsInterop.Log(data);
    }
}
