var BitButil = BitButil || {};

(function (butil: any) {
    // Map of dotnet-watchId (string) -> browser numeric watchId (number).
    const _watches: { [id: string]: number } = {};

    butil.geolocation = {
        isSupported() { return 'geolocation' in window.navigator; },
        getCurrentPosition,
        watchPosition,
        clearWatch
    };

    function toPosition(p: GeolocationPosition) {
        const c = p.coords;
        return {
            timestamp: p.timestamp,
            coords: {
                latitude: c.latitude,
                longitude: c.longitude,
                accuracy: c.accuracy,
                altitude: c.altitude,
                altitudeAccuracy: c.altitudeAccuracy,
                heading: c.heading,
                speed: c.speed
            }
        };
    }

    function toJsOptions(options: any) {
        if (!options) return undefined;
        return {
            enableHighAccuracy: !!options.enableHighAccuracy,
            maximumAge: options.maximumAge,
            timeout: options.timeout
        };
    }

    function getCurrentPosition(options: any) {
        return new Promise<any>(resolve => {
            if (!('geolocation' in window.navigator)) {
                resolve({ position: null, errorCode: 0, errorMessage: 'Geolocation is not supported in this runtime.' });
                return;
            }

            window.navigator.geolocation.getCurrentPosition(
                p => resolve({ position: toPosition(p), errorCode: 0, errorMessage: null }),
                err => resolve({ position: null, errorCode: err.code, errorMessage: err.message }),
                toJsOptions(options));
        });
    }

    function watchPosition(positionMethod: string, errorMethod: string, listenerId: string, options: any) {
        if (!('geolocation' in window.navigator)) {
            DotNet.invokeMethodAsync('Bit.Butil', errorMethod, listenerId, 0, 'Geolocation is not supported in this runtime.');
            return;
        }

        const watchId = window.navigator.geolocation.watchPosition(
            p => DotNet.invokeMethodAsync('Bit.Butil', positionMethod, listenerId, toPosition(p)),
            err => DotNet.invokeMethodAsync('Bit.Butil', errorMethod, listenerId, err.code, err.message),
            toJsOptions(options));

        _watches[listenerId] = watchId;
    }

    function clearWatch(listenerId: string) {
        const watchId = _watches[listenerId];
        if (watchId === undefined) return;
        delete _watches[listenerId];
        window.navigator.geolocation.clearWatch(watchId);
    }
}(BitButil));
