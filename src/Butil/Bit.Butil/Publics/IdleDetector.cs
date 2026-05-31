using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdleDetector">IdleDetector API</see>.
/// Requires the <c>idle-detection</c> permission, which the browser will prompt for on first
/// <see cref="Start"/>.
/// </summary>
public class IdleDetector(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>IdleDetector</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.idleDetector.isSupported");

    /// <summary>
    /// Asks the browser for the <c>idle-detection</c> permission.
    /// </summary>
    /// <returns>The new permission state.</returns>
    public ValueTask<PermissionState> RequestPermission()
        => RequestPermissionInternal();

    private async ValueTask<PermissionState> RequestPermissionInternal()
    {
        var raw = await js.Invoke<string>("BitButil.idleDetector.requestPermission");
        return raw switch
        {
            "granted" => PermissionState.Granted,
            "denied" => PermissionState.Denied,
            "prompt" => PermissionState.Prompt,
            _ => PermissionState.Unknown,
        };
    }

    /// <summary>
    /// Starts watching for idle changes. The handler fires whenever user/screen state changes.
    /// </summary>
    /// <param name="threshold">Idle threshold in seconds. Spec minimum is 60.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IdleState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IdleDetectorListenersManager))]
    public async Task<ButilSubscription> Start(int threshold, Action<IdleState> handler)
    {
        if (threshold < 60) threshold = 60;

        var id = IdleDetectorListenersManager.AddListener(handler);
        await js.InvokeVoid("BitButil.idleDetector.start",
            IdleDetectorListenersManager.InvokeMethodName,
            id,
            threshold);

        return new ButilSubscription(id, async () =>
        {
            IdleDetectorListenersManager.RemoveListener(id);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.idleDetector.stop", id);
        });
    }
}
