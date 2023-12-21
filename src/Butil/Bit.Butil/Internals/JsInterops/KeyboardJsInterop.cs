﻿using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class KeyboardJsInterop
{
    internal static async Task KeyboardAdd(this IJSRuntime js,
        string methodName,
        Guid listenerId,
        string code,
        bool alt,
        bool ctrl,
        bool meta,
        bool shift,
        bool preventDefault = false,
        bool stopPropagation = false,
        bool repeat = false)
        => await js.InvokeVoidAsync("BitButil.keyboard.add",
            methodName,
            listenerId,
            code,
            alt,
            ctrl,
            meta,
            shift,
            preventDefault,
            stopPropagation,
            repeat);

    internal static async Task KeyboardRemove(this IJSRuntime js, Guid[] ids)
        => await js.InvokeVoidAsync("BitButil.keyboard.remove", ids);
}
