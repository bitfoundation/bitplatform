var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _sensors: { [id: string]: any } = {};

    // The eight Generic Sensor classes, by the kebab-case name .NET sends. Everything else about
    // them is uniform - construct, start(), read on 'reading' - so one module covers all of them.
    const CONSTRUCTORS: { [type: string]: string } = {
        'accelerometer': 'Accelerometer',
        'gyroscope': 'Gyroscope',
        'magnetometer': 'Magnetometer',
        'absolute-orientation': 'AbsoluteOrientationSensor',
        'relative-orientation': 'RelativeOrientationSensor',
        'gravity': 'GravitySensor',
        'linear-acceleration': 'LinearAccelerationSensor',
        'ambient-light': 'AmbientLightSensor'
    };

    // The Permissions-API names each sensor is gated on. Orientation sensors fuse several
    // physical sensors and need every one of them granted.
    const PERMISSIONS: { [type: string]: string[] } = {
        'accelerometer': ['accelerometer'],
        'gyroscope': ['gyroscope'],
        'magnetometer': ['magnetometer'],
        'absolute-orientation': ['accelerometer', 'gyroscope', 'magnetometer'],
        'relative-orientation': ['accelerometer', 'gyroscope'],
        'gravity': ['accelerometer'],
        'linear-acceleration': ['accelerometer'],
        'ambient-light': ['ambient-light-sensor']
    };

    butil.sensors = {
        isSupported,
        requestPermission,
        start,
        stop
    };

    function constructorOf(type: string) {
        const name = CONSTRUCTORS[type];
        return name ? (window as any)[name] : undefined;
    }

    function isSupported(type: string) { return typeof constructorOf(type) === 'function'; }

    // Sensor permissions are not requestable on their own - querying is all the Permissions API
    // offers, and the actual prompt (where there is one) happens on the first start(). The
    // aggregate is the least-granted of the names the sensor needs.
    async function requestPermission(type: string) {
        const names = PERMISSIONS[type] ?? [];
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

    function start(subscriptionId: string, dotNetRef: any, type: string, frequency: number | null, referenceFrame: string | null) {
        const Constructor = constructorOf(type);
        if (typeof Constructor !== 'function') {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorError', subscriptionId, `${type} is not supported.`);
            return false;
        }

        const options: any = {};
        if (frequency) options.frequency = frequency;
        // Only the orientation sensors accept referenceFrame; passing it elsewhere is ignored,
        // but it is left out anyway so the constructed options match what the sensor documents.
        if (referenceFrame && (type === 'absolute-orientation' || type === 'relative-orientation')) options.referenceFrame = referenceFrame;

        let sensor: any;
        try {
            sensor = new Constructor(options);
        } catch (e: any) {
            // SecurityError when a Permissions-Policy blocks the sensor, ReferenceError when the
            // flag is off - both surface here rather than as an unhandled construction throw.
            butil.utils.dispatch(dotNetRef, 'InvokeSensorError', subscriptionId, e?.message ?? String(e));
            return false;
        }

        sensor.addEventListener('reading', () => {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorReading', subscriptionId, reading(type, sensor));
        });
        sensor.addEventListener('error', (event: any) => {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorError', subscriptionId, event?.error?.message ?? 'sensor error');
        });

        _sensors[subscriptionId] = sensor;
        try { sensor.start(); } catch (e: any) {
            butil.utils.dispatch(dotNetRef, 'InvokeSensorError', subscriptionId, e?.message ?? String(e));
            stop(subscriptionId);
            return false;
        }
        return true;
    }

    function stop(subscriptionId: string) {
        const sensor = _sensors[subscriptionId];
        if (!sensor) return;
        delete _sensors[subscriptionId];
        try { sensor.stop(); } catch { /* never started, or already stopped */ }
    }
}(BitButil));
