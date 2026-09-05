using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sensor_APIs">Generic Sensor API</see>:
/// <c>Accelerometer</c>, <c>Gyroscope</c>, <c>Magnetometer</c>, the two orientation sensors,
/// <c>GravitySensor</c>, <c>LinearAccelerationSensor</c> and <c>AmbientLightSensor</c>.
/// </summary>
/// <remarks>
/// All eight share one shape - construct, start, read on each <c>reading</c> event - so one service
/// covers them, with <see cref="SensorType"/> selecting which.
/// <br/>
/// This is the modern counterpart to <see cref="DeviceOrientation"/>, which wraps the legacy
/// <c>deviceorientation</c> / <c>devicemotion</c> events. What these add is an explicit sample rate,
/// a per-sensor permission, an error channel, and sensors the legacy events never had -
/// magnetometer, gravity and ambient light.
/// <br/>
/// Chromium only, and only over HTTPS. A sensor can also be blocked by a <c>Permissions-Policy</c>
/// on the document, in which case starting it reports an error rather than throwing.
/// </remarks>
[ButilService(typeof(Sensors))]
public class Sensors(IJSRuntime js) : IAsyncDisposable
{
    internal const string ReadingMethodName = nameof(InvokeSensorReading);
    internal const string ErrorMethodName = nameof(InvokeSensorError);

    private readonly ConcurrentDictionary<Guid, (Action<SensorReading> OnReading, Action<string>? OnError)> _handlers = new();

    // Per-instance callback reference (see Keyboard): sensors are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Sensors>? _dotNetRef;
    private DotNetObjectReference<Sensors> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>The name the browser gives a sensor - also what a <see cref="SensorReading.Type"/> carries.</summary>
    internal static string NameOf(SensorType type) => type switch
    {
        SensorType.Accelerometer => "accelerometer",
        SensorType.Gyroscope => "gyroscope",
        SensorType.Magnetometer => "magnetometer",
        SensorType.AbsoluteOrientation => "absolute-orientation",
        SensorType.RelativeOrientation => "relative-orientation",
        SensorType.Gravity => "gravity",
        SensorType.LinearAcceleration => "linear-acceleration",
        SensorType.AmbientLight => "ambient-light",
        // Deliberately not a fallback: a sensor added to the enum without a name here would
        // silently start the ambient-light sensor instead of failing.
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown sensor type.")
    };

    /// <summary>True when the runtime exposes the constructor behind <paramref name="type"/>.</summary>
    /// <remarks>
    /// Support is per sensor, not per API: a browser can ship <c>Accelerometer</c> and not
    /// <c>Magnetometer</c>, and a device can ship neither.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported(SensorType type)
        => js.Invoke<bool>("BitButil.sensors.isSupported", NameOf(type));

    /// <summary>The permission state for a sensor.</summary>
    /// <remarks>
    /// Sensor permissions cannot be requested on their own - the prompt, where an engine has one,
    /// happens on the first <see cref="Subscribe"/>. This is a query, so use it to decide whether
    /// starting is worth attempting, not to trigger a prompt. Sensors that fuse several physical
    /// ones report the least-granted of the permissions they need.
    /// </remarks>
    public async ValueTask<PermissionState> QueryPermission(SensorType type)
        => Permissions.ToState(await js.Invoke<string>("BitButil.sensors.requestPermission", NameOf(type)));

    /// <summary>
    /// Invoked from JS for each sensor reading. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ReadingMethodName)]
    public void InvokeSensorReading(Guid id, SensorReading reading)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnReading.Invoke(reading);
    }

    /// <summary>
    /// Invoked from JS when a running sensor errors. See <see cref="InvokeSensorReading"/>. A sensor
    /// that refuses to start reports through <see cref="Subscribe"/> instead, not through here.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeSensorError(Guid id, string message)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnError?.Invoke(message);
    }

    /// <summary>
    /// Starts a sensor and calls <paramref name="onReading"/> for each sample.
    /// </summary>
    /// <param name="type">Which sensor to start.</param>
    /// <param name="onReading">Called for every reading.</param>
    /// <param name="options">Sample rate, and the reference frame for every sensor but the ambient-light one.</param>
    /// <param name="onError">
    /// Called when the sensor cannot start or stops working - permission refused, blocked by a
    /// Permissions-Policy, or no such hardware. These arrive here rather than as an exception,
    /// because most of them happen after the call has already returned.
    /// </param>
    /// <returns>A subscription - dispose it to stop the sensor. Sensors keep the radio or the
    /// hardware awake, so a reading nobody is watching is a battery cost with no benefit.</returns>
    /// <exception cref="InvalidOperationException">
    /// The sensor did not start - unsupported, blocked, or refused. <paramref name="onError"/> is
    /// called with the same message first, so the failure reaches the error channel whether or not
    /// the caller supplied one.
    /// </exception>
    [DynamicDependency(nameof(InvokeSensorReading), typeof(Sensors))]
    [DynamicDependency(nameof(InvokeSensorError), typeof(Sensors))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SensorReading))]
    public async ValueTask<ButilSubscription> Subscribe(SensorType type,
                                                        Action<SensorReading> onReading,
                                                        SensorOptions? options = null,
                                                        Action<string>? onError = null)
    {
        // Defaulted here rather than read through ?. at each use, so the rate limit a caller passing
        // no options gets is the one SensorOptions documents instead of a second literal.
        options ??= new SensorOptions();

        var referenceFrame = options.ReferenceFrame switch
        {
            SensorReferenceFrame.Device => "device",
            SensorReferenceFrame.Screen => "screen",
            _ => null
        };

        return await ButilSubscriptionHelper.RegisterOrError(_handlers, (onReading, onError),
                                                             id => js.InvokeRegisterOrError("BitButil.sensors.start", id, DotNetRef, NameOf(type),
                                                                                            options.Frequency, referenceFrame, Math.Max(0, options.MinIntervalMs)),
                                                             id => js.InvokeVoid("BitButil.sensors.stop", id),
                                                             onError);
    }

    /// <summary>Stops every sensor this instance started and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var id in _handlers.Keys.ToArray())
            {
                _handlers.TryRemove(id, out _);
                await js.InvokeVoid("BitButil.sensors.stop", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }

        GC.SuppressFinalize(this);
    }
}
