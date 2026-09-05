var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _sensors: { [id: string]: any } = {};

    // The eight Generic Sensor classes, by the kebab-case name .NET sends: the constructor to look
    // up, the Permissions-API names the sensor is gated on (orientation sensors fuse several
    // physical sensors and need every one of them granted), and whether it takes a referenceFrame.
    // Every sensor that reports along axes takes one - the motion sensors read out in the frame
    // just as the orientation sensors do; ambient-light is the one reading with no axes at all.
    // Everything else about them is uniform - construct, start(), read on 'reading' - so one module
    // covers all of them, and adding a sensor is one row here.
    const SENSORS: { [type: string]: { ctor: string, permissions: string[], referenceFrame?: boolean } } = {
        'accelerometer': { ctor: 'Accelerometer', permissions: ['accelerometer'], referenceFrame: true },
        'gyroscope': { ctor: 'Gyroscope', permissions: ['gyroscope'], referenceFrame: true },
        'magnetometer': { ctor: 'Magnetometer', permissions: ['magnetometer'], referenceFrame: true },
        'absolute-orientation': { ctor: 'AbsoluteOrientationSensor', permissions: ['accelerometer', 'gyroscope', 'magnetometer'], referenceFrame: true },
        'relative-orientation': { ctor: 'RelativeOrientationSensor', permissions: ['accelerometer', 'gyroscope'], referenceFrame: true },
        'gravity': { ctor: 'GravitySensor', permissions: ['accelerometer'], referenceFrame: true },
        'linear-acceleration': { ctor: 'LinearAccelerationSensor', permissions: ['accelerometer'], referenceFrame: true },
        'ambient-light': { ctor: 'AmbientLightSensor', permissions: ['ambient-light-sensor'] }
    };

    butil.sensors = {
        isSupported,
        requestPermission,
        start,
        stop
    };

    function constructorOf(type: string) {
        const name = SENSORS[type]?.ctor;
        return name ? (window as any)[name] : undefined;
    }

    function isSupported(type: string) { return typeof constructorOf(type) === 'function'; }

    // Sensor permissions are not requestable on their own - querying is all the Permissions API
    // offers, and the actual prompt (where there is one) happens on the first start(). The
    // aggregate is the least-granted of the names the sensor needs.
    async function requestPermission(type: string) {
        const names = SENSORS[type]?.permissions ?? [];
        if (!names.length || !navigator.permissions) return 'prompt';
        try {
            const states = await Promise.all(names.map(name => navigator.permissions.query({ name } as any).then(s => s.state, () => 'prompt')));
            if (states.indexOf('denied') >= 0) return 'denied';
            if (states.indexOf('prompt') >= 0) return 'prompt';
            return 'granted';
        } catch { return 'prompt'; }
    }

    function reading(type: string, sensor: any) {
        return {
            type,
            // DOMHighResTimeStamp, or null before the first reading has landed.
            timestamp: sensor.timestamp ?? 0,
            x: sensor.x ?? null,
            y: sensor.y ?? null,
            z: sensor.z ?? null,
            quaternion: sensor.quaternion ? Array.prototype.slice.call(sensor.quaternion) : null,
            illuminance: sensor.illuminance ?? null
        };
    }

    // Returns null when the sensor started, or the reason it did not. A failure to start is
    // reported through the return value rather than InvokeSensorError: .NET is still inside the
    // call, so it can raise the reason once - a dispatched error would race the call's own result.
    function start(subscriptionId: string, dotNetRef: any, type: string, frequency: number | null, referenceFrame: string | null, minIntervalMs: number) {
        const Constructor = constructorOf(type);
        if (typeof Constructor !== 'function') return `${type} is not supported.`;

        const options: any = {};
        if (frequency) options.frequency = frequency;
        // Every spatial sensor accepts referenceFrame; ambient-light has no axes to frame, so it
        // is left out there so the constructed options match what the sensor documents.
        if (referenceFrame && SENSORS[type]?.referenceFrame) options.referenceFrame = referenceFrame;

        let sensor: any;
        try {
            sensor = new Constructor(options);
        } catch (e: any) {
            // SecurityError when a Permissions-Policy blocks the sensor, ReferenceError when the
            // flag is off - both surface here rather than as an unhandled construction throw.
            return e?.message ?? String(e);
        }

        // frequency is only a hint the platform is free to ignore, and its default is 60 Hz on the
        // motion sensors - which is 60 interop round-trips a second for readings a UI can show a
        // handful of. The rate limit is applied here, before the round-trip is paid for, exactly as
        // deviceOrientation.ts does for the legacy event streams.
        sensor.addEventListener('reading', butil.utils.throttle(minIntervalMs, () => {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorReading', subscriptionId, reading(type, sensor));
        }));
        sensor.addEventListener('error', (event: any) => {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorError', subscriptionId, event?.error?.message ?? 'sensor error');
        });

        _sensors[subscriptionId] = sensor;
        try { sensor.start(); } catch (e: any) {
            stop(subscriptionId);
            return e?.message ?? String(e);
        }
        return null;
    }

    function stop(subscriptionId: string) {
        const sensor = _sensors[subscriptionId];
        if (!sensor) return;
        delete _sensors[subscriptionId];
        try { sensor.stop(); } catch { /* never started, or already stopped */ }
    }
}(BitButil));
