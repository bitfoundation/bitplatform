var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The launch the app was started with. `setConsumer` may only be called once per page, and the
    // browser delivers the launch as soon as a consumer exists - which can be before .NET has asked
    // for one. So the consumer is installed while this module is evaluated and the launch is parked
    // here until a .NET handler shows up.
    let _pending: any = null;
    let _consumer: { dotNetRef: any, listenerId: string, method: string } | null = null;

    // The launched files' handles, kept so their contents can be read (and written back) later.
    // Indexed by the position .NET sees in the reported file list.
    let _handles: any[] = [];

    const queue = (window as any).launchQueue;
    if (queue?.setConsumer) {
        try {
            queue.setConsumer((params: any) => deliver(params));
        } catch {
            // Another consumer was already set - nothing to do.
        }
    }

    async function describe(params: any) {
        _handles = Array.isArray(params?.files) ? [...params.files] : [];

        const files: any[] = [];
        for (let i = 0; i < _handles.length; i++) {
            const handle = _handles[i];
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
            files.push({ index: i, name, type, size, lastModified });
        }

        return { targetUrl: params?.targetURL ?? '', files };
    }

    async function deliver(params: any) {
        const payload = await describe(params);
        if (_consumer) butil.utils.dispatch(_consumer.dotNetRef, _consumer.method, _consumer.listenerId, payload);
        else _pending = payload;
    }

    butil.launchQueue = {
        isSupported() { return 'launchQueue' in window; },
        // File handling is a separate capability from launch handling: a runtime can deliver a
        // launch without ever putting files on it.
        supportsFiles() { return 'LaunchParams' in window && 'files' in ((window as any).LaunchParams.prototype ?? {}); },
        setConsumer(dotNetRef: any, listenerId: string, method: string) {
            _consumer = { dotNetRef, listenerId, method };
            // A launch that arrived before .NET was ready is replayed rather than dropped.
            if (_pending) {
                const payload = _pending;
                _pending = null;
                butil.utils.dispatch(dotNetRef, method, listenerId, payload);
            }
        },
        clearConsumer(listenerId: string) {
            if (_consumer?.listenerId === listenerId) _consumer = null;
        },
        async readText(index: number) {
            const handle = _handles[index];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return await file.text();
            } catch {
                return null;
            }
        },
        async readBytes(index: number) {
            const handle = _handles[index];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return new Uint8Array(await file.arrayBuffer());
            } catch {
                return null;
            }
        },
        async writeText(index: number, contents: string) {
            const handle = _handles[index];
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
        async writeBytes(index: number, contents: Uint8Array) {
            const handle = _handles[index];
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
