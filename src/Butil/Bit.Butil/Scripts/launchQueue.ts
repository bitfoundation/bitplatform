var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The launches the app was started with. `setConsumer` may only be called once per page, and the
    // browser delivers a launch as soon as a consumer exists - which can be before .NET has asked
    // for one. So the consumer is installed while this module is evaluated and the launches are
    // parked here, in arrival order, until a .NET handler shows up. A queue rather than a single
    // slot: a launch handler routing a second "open with" into this window would otherwise drop the
    // first one.
    let _pending: any[] = [];
    let _consumer: { dotNetRef: any, listenerId: string, method: string } | null = null;

    // The launched files' handles, kept so their contents can be read (and written back) later.
    // Keyed by an opaque launch id, and within a launch by the position .NET sees in the reported
    // file list - so a second launch cannot become a way to reach the first launch's files.
    const _launches: { [launchId: string]: any[] } = {};
    let _launchCount = 0;
    let _lastLaunchId = '';

    // A page that is launched over and over would otherwise hold every file handle it was ever
    // given; the recent ones are what a consumer can still be reading.
    const MAX_RETAINED_LAUNCHES = 8;
    const _launchIds: string[] = [];

    function handlesOf(launchId: string) {
        // An empty id is the by-index API talking about the most recent launch.
        return _launches[launchId || _lastLaunchId] ?? [];
    }

    const queue = (window as any).launchQueue;
    if (queue?.setConsumer) {
        try {
            queue.setConsumer((params: any) => deliver(params));
        } catch {
            // Another consumer was already set - nothing to do.
        }
    }

    async function describe(params: any) {
        const launchId = `launch-${++_launchCount}`;
        const handles = Array.isArray(params?.files) ? [...params.files] : [];

        _launches[launchId] = handles;
        _lastLaunchId = launchId;
        _launchIds.push(launchId);
        while (_launchIds.length > MAX_RETAINED_LAUNCHES) delete _launches[_launchIds.shift() as string];

        const files: any[] = [];
        for (let i = 0; i < handles.length; i++) {
            const handle = handles[i];
            let name = handle?.name ?? '';
            let size = 0;
            let type = '';
            let lastModified = 0;
            try {
                const file = await handle.getFile();
                name = file.name ?? name;
                size = file.size ?? 0;
                type = file.type ?? '';
                lastModified = file.lastModified ?? 0;
            } catch {
                // The handle went stale, or permission was revoked between launch and read.
            }
            files.push({ launchId, index: i, name, type, size, lastModified });
        }

        return { launchId, targetUrl: params?.targetURL ?? '', files };
    }

    async function deliver(params: any) {
        const payload = await describe(params);
        if (_consumer) butil.utils.dispatch(_consumer.dotNetRef, _consumer.method, _consumer.listenerId, payload);
        else _pending.push(payload);
    }

    butil.launchQueue = {
        isSupported() { return 'launchQueue' in window; },
        // File handling is a separate capability from launch handling: a runtime can deliver a
        // launch without ever putting files on it.
        supportsFiles() { return 'LaunchParams' in window && 'files' in ((window as any).LaunchParams.prototype ?? {}); },
        setConsumer(dotNetRef: any, listenerId: string, method: string) {
            _consumer = { dotNetRef, listenerId, method };
            // Launches that arrived before .NET was ready are replayed rather than dropped, oldest
            // first, so the handler sees them in the order the browser delivered them.
            const pending = _pending;
            _pending = [];
            for (const payload of pending) butil.utils.dispatch(dotNetRef, method, listenerId, payload);
        },
        clearConsumer(listenerId: string) {
            if (_consumer?.listenerId === listenerId) _consumer = null;
        },
        async readText(launchId: string, index: number) {
            const handle = handlesOf(launchId)[index];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return await file.text();
            } catch {
                return null;
            }
        },
        async readBytes(launchId: string, index: number) {
            const handle = handlesOf(launchId)[index];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return new Uint8Array(await file.arrayBuffer());
            } catch {
                return null;
            }
        },
        async writeText(launchId: string, index: number, contents: string) {
            const handle = handlesOf(launchId)[index];
            if (!handle?.createWritable) return false;
            try {
                // A file-handling launch grants read access; writing may still need an explicit
                // permission request, which createWritable triggers.
                const writable = await handle.createWritable();
                await writable.write(contents);
                await writable.close();
                return true;
            } catch {
                return false;
            }
        },
        async writeBytes(launchId: string, index: number, contents: Uint8Array) {
            const handle = handlesOf(launchId)[index];
            if (!handle?.createWritable) return false;
            try {
                const writable = await handle.createWritable();
                await writable.write(butil.utils.arrayToBuffer(contents));
                await writable.close();
                return true;
            } catch {
                return false;
            }
        }
    };
}(BitButil));
