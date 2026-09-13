var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Entry { worker: Worker | any; shared: boolean; portId?: string; }

    const _workers: { [id: string]: Entry } = {};

    butil.worker = {
        isSupported() { return typeof (window as any).Worker === 'function'; },
        isSharedSupported() { return typeof (window as any).SharedWorker === 'function'; },

        // A dedicated worker: one owner, one message port, torn down by terminate().
        create(dotNetRef: any, id: string, scriptUrl: string, name: string | null, module: boolean, credentials: string | null) {
            if (typeof (window as any).Worker !== 'function') return false;

            const options: any = { type: module ? 'module' : 'classic' };
            if (name) options.name = name;
            if (credentials) options.credentials = credentials;

            let worker: Worker;
            try {
                worker = new Worker(scriptUrl, options);
            } catch {
                // A cross-origin script URL, or a blocked one: the constructor's only synchronous
                // failure. A script that 404s or throws surfaces as an error event instead.
                return false;
            }

            _workers[id] = { worker, shared: false };

            worker.addEventListener('message', (e: MessageEvent) =>
                butil.utils.dispatch(dotNetRef, 'InvokeWorkerMessage', id, ...butil.utils.encodeMessage(e.data)));

            worker.addEventListener('error', (e: any) =>
                butil.utils.dispatch(dotNetRef, 'InvokeWorkerError', id,
                    e?.message ?? 'Worker error', e?.filename ?? '', e?.lineno ?? 0, e?.colno ?? 0));

            // Fired when the worker posts something this context cannot deserialize. Rare, but it is
            // the difference between "the worker said nothing" and "the worker said something
            // unrepresentable", which are very different bugs.
            worker.addEventListener('messageerror', () =>
                butil.utils.dispatch(dotNetRef, 'InvokeWorkerError', id,
                    'A message from the worker could not be deserialized.', '', 0, 0));

            return true;
        },

        // A shared worker: one instance for every page of the origin that names the same script and
        // name, reached through a port rather than directly. The port is registered with the
        // messageChannel module so a single set of port APIs serves both.
        createShared(id: string, portId: string, scriptUrl: string, name: string | null, module: boolean, credentials: string | null) {
            if (typeof (window as any).SharedWorker !== 'function') return false;

            const options: any = { type: module ? 'module' : 'classic' };
            if (name) options.name = name;
            if (credentials) options.credentials = credentials;

            let worker: any;
            try {
                worker = new (window as any).SharedWorker(scriptUrl, options);
            } catch {
                return false;
            }

            _workers[id] = { worker, shared: true, portId };
            butil.messageChannel.adopt(portId, worker.port);
            return true;
        },

        postJson(id: string, json: string | null) {
            const entry = _workers[id];
            if (!entry || entry.shared) return false;
            try { entry.worker.postMessage(json === null ? null : JSON.parse(json)); return true; } catch { return false; }
        },

        postBytes(id: string, bytes: Uint8Array, transfer: boolean) {
            const entry = _workers[id];
            if (!entry || entry.shared) return false;
            const buffer = butil.utils.arrayToBuffer(bytes);
            try {
                // Transferring is the reason to send bytes rather than JSON: the buffer moves to the
                // worker instead of being copied, and is detached here afterwards.
                entry.worker.postMessage(buffer, transfer ? [buffer] : []);
                return true;
            } catch { return false; }
        },

        // Hands ports to the worker along with a message - the usual way to give a worker a private
        // channel back to some other part of the page.
        postWithPorts(id: string, json: string | null, transferredPortIds: string[]) {
            const entry = _workers[id];
            if (!entry || entry.shared) return false;

            const ports = (transferredPortIds ?? []).map(portId => butil.messageChannel.portOf(portId)).filter(Boolean) as MessagePort[];
            if (ports.length !== (transferredPortIds ?? []).length) return false;

            try {
                entry.worker.postMessage(json === null ? null : JSON.parse(json), ports);
                // The ports belong to the worker now; a reference kept here would be detached.
                for (const portId of transferredPortIds) butil.messageChannel.release(portId);
                return true;
            } catch { return false; }
        },

        // Only meaningful for a dedicated worker. A shared worker ends when the last page holding a
        // port to it goes away - no page gets to kill it for the others.
        terminate(id: string) {
            const entry = _workers[id];
            if (!entry) return;
            delete _workers[id];

            if (entry.shared) {
                // Dropping this page's connection is all a shared worker allows, and the connection
                // is the port - which was adopted by the messageChannel module, so forgetting the
                // entry without releasing it there would leave the port registered for good.
                if (entry.portId) {
                    butil.messageChannel.close(entry.portId);
                    butil.messageChannel.release(entry.portId);
                }
                return;
            }

            try { entry.worker.terminate(); } catch { /* already gone */ }
        },

        disposeAll() {
            for (const id of Object.keys(_workers)) butil.worker.terminate(id);
        }
    };
}(BitButil));
