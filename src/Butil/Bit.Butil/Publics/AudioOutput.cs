using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Audio_Output_Devices_API">Audio Output Devices API</see>
/// (<c>HTMLMediaElement.setSinkId</c> and <c>navigator.mediaDevices.selectAudioOutput</c>): send a
/// media element's sound to a chosen speaker or headset instead of the system default.
/// </summary>
/// <remarks>
/// Needs HTTPS. <see cref="SelectDevice"/> opens the browser's output chooser and so must run
/// inside a user gesture; it is also the only way to learn a device's label without first asking
/// for a microphone, which is why enumerating usually comes back unlabelled.
/// <br/>
/// Routing applies per media element, not per page, so an app playing several sounds sets the sink
/// on each element it wants moved.
/// </remarks>
[ButilService(typeof(AudioOutput))]
public class AudioOutput(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>HTMLMediaElement.setSinkId</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.audioOutput.isSupported");

    /// <summary>
    /// True when the runtime exposes <c>navigator.mediaDevices.selectAudioOutput</c>. Narrower than
    /// <see cref="IsSupported"/>: an engine can route audio without offering the chooser.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSelectionSupported() => js.Invoke<bool>("BitButil.audioOutput.isSelectionSupported");

    /// <summary>
    /// The audio output devices this origin can see - the <see cref="MediaDeviceInfo.Kind"/>
    /// <c>"audiooutput"</c> entries of <see cref="MediaDevices.EnumerateDevices"/>. Labels are empty
    /// until the origin has been granted a device permission, so a picker built on this alone shows
    /// unnamed entries - use <see cref="SelectDevice"/> instead where the chooser is available.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaDeviceInfo))]
    public ValueTask<MediaDeviceInfo[]> GetDevices()
        => js.Invoke<MediaDeviceInfo[]>("BitButil.audioOutput.getDevices");

    /// <summary>
    /// Opens the browser's audio-output chooser and returns the device the user picked, or null
    /// when they dismissed it. Must be called from a user gesture.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaDeviceInfo))]
    public ValueTask<MediaDeviceInfo?> SelectDevice()
        => js.Invoke<MediaDeviceInfo?>("BitButil.audioOutput.selectDevice");

    /// <summary>
    /// Routes one <c>&lt;audio&gt;</c> or <c>&lt;video&gt;</c> element's sound to the given device.
    /// </summary>
    /// <param name="mediaElement">The element to re-route.</param>
    /// <param name="deviceId">
    /// The device to route to. An empty string routes back to the system default. False means the
    /// browser refused - typically because the origin has no permission for that device.
    /// </param>
    public ValueTask<bool> SetSinkId(ElementReference mediaElement, string deviceId)
        => js.Invoke<bool>("BitButil.audioOutput.setSinkId", mediaElement, deviceId);

    /// <summary>
    /// The device an element is currently playing through. An empty string means the system
    /// default - which is also what an element that has never been re-routed reports.
    /// </summary>
    public ValueTask<string> GetSinkId(ElementReference mediaElement)
        => js.Invoke<string>("BitButil.audioOutput.getSinkId", mediaElement);
}
