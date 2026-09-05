var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Everything here is addressed by path rather than by handle id. Unlike the File System Access
    // pickers, an OPFS handle is not a capability the user granted - the whole tree is reachable
    // from navigator.storage.getDirectory() at any time, so keeping handles alive across calls
    // would buy nothing and leak.

    // The dedicated worker's source. createSyncAccessHandle() only exists on a worker thread, so
    // the synchronous file API is unreachable from the window no matter how it is called - the
    // worker is not an optimization, it is the only place the API exists. Built from a blob URL so
    // the library still ships as one bundle with no extra file for a consumer to copy or serve.
    const SYNC_WORKER_SOURCE = `
self.onmessage = async function (e) {
    var m = e.data, id = m.id;
    try {
        if (m.op === 'probe') {
            // Only the worker can answer this: createSyncAccessHandle is [Exposed=DedicatedWorker],
            // so on the window the method is simply not on the prototype.
            self.postMessage({
                id: id, ok: true, result: typeof FileSystemFileHandle !== 'undefined'
                    && typeof FileSystemFileHandle.prototype.createSyncAccessHandle === 'function'
            });
            return;
        }
        var dir = await navigator.storage.getDirectory();
        var parts = String(m.path || '').split('/').filter(function (p) { return p && p !== '.'; });
        var name = parts.pop();
        if (!name) throw new Error('a file path is required');
        for (var i = 0; i < parts.length; i++) {
            dir = await dir.getDirectoryHandle(parts[i], { create: m.op !== 'read' && m.op !== 'size' ? true : false });
        }
        var file = await dir.getFileHandle(name, { create: m.op !== 'read' && m.op !== 'size' });
        var access = await file.createSyncAccessHandle();
        try {
            var result;
            if (m.op === 'read') {
                var size = access.getSize();
                var offset = Math.min(m.offset || 0, size);
                var length = m.length > 0 ? Math.min(m.length, size - offset) : size - offset;
                var buffer = new Uint8Array(length);
                access.read(buffer, { at: offset });
                result = buffer;
            } else if (m.op === 'write') {
                // truncate first: writing 3 bytes over a 10-byte file otherwise leaves 7 behind.
                if (m.truncate) access.truncate(m.offset || 0);
                result = access.write(m.data, { at: m.offset || 0 });
                access.flush();
            } else if (m.op === 'append') {
                result = access.write(m.data, { at: access.getSize() });
                access.flush();
            } else if (m.op === 'truncate') {
                access.truncate(m.size || 0);
                access.flush();
                result = true;
            } else if (m.op === 'size') {
                result = access.getSize();
            } else {
                throw new Error('unknown operation: ' + m.op);
            }
            self.postMessage({ id: id, ok: true, result: result });
        } finally {
            // An access handle is an exclusive lock on the file for as long as it is open, so a
            // handle left behind by a failed operation would deadlock every later one.
            access.close();
        }
    } catch (err) {
        self.postMessage({ id: id, ok: false, error: String(err && err.message ? err.message : err) });
    }
};
`;

    let _worker: Worker | null = null;
    let _workerUrl: string | null = null;
    let _nextRequest = 1;
    const _pending: { [id: string]: { resolve: (value: any) => void; reject: (reason: any) => void } } = {};

    function root(): Promise<any> {
        const storage: any = (window.navigator as any).storage;
        if (!storage?.getDirectory) return Promise.reject(new Error('OPFS is not supported'));
        return storage.getDirectory();
    }

    function segments(path: string) {
        return String(path ?? '').split('/').filter(part => part && part !== '.');
    }

    // Walks to the directory holding `path`, creating the intermediate directories when asked.
    // OPFS has no path traversal of its own: every segment is one getDirectoryHandle call.
    async function walk(from: any, path: string, create: boolean) {
        let dir = from;
        for (const part of segments(path)) {
            dir = await dir.getDirectoryHandle(part, { create });
        }
        return dir;
    }

    async function fileIn(from: any, path: string, create: boolean) {
        const parts = segments(path);
        const name = parts.pop();
        if (!name) throw new Error('a file path is required');
        const dir = await walk(from, parts.join('/'), create);
        return await dir.getFileHandle(name, { create });
    }

    // The operations below take the directory they start from so the same code serves the origin's
    // own OPFS root and a storage bucket's - the storage buckets module calls straight into these.
    async function listUnder(from: any, path: string) {
        const dir = await walk(from, path, false);
        const prefix = segments(path).join('/');
        const entries: any[] = [];
        for await (const entry of dir.values()) {
            entries.push({
                name: entry.name,
                path: prefix ? `${prefix}/${entry.name}` : entry.name,
                isDirectory: entry.kind === 'directory'
            });
        }
        return entries;
    }

    async function readTextUnder(from: any, path: string) {
        const handle = await fileIn(from, path, false);
        return await (await handle.getFile()).text();
    }

    async function readBytesUnder(from: any, path: string) {
        const handle = await fileIn(from, path, false);
        return new Uint8Array(await (await handle.getFile()).arrayBuffer());
    }

    async function writeUnder(from: any, path: string, text: string | null, bytes: Uint8Array | null) {
        const handle = await fileIn(from, path, true);
        const writable = await handle.createWritable();
        try {
            await writable.write(text !== null && text !== undefined ? text : (bytes ?? new Uint8Array()));
        } finally {
            // close() is what commits the swap file over the original.
            await writable.close();
        }
        return true;
    }

    async function removeUnder(from: any, path: string, recursive: boolean) {
        const parts = segments(path);
        const name = parts.pop();
        if (!name) throw new Error('a path is required');
        const dir = await walk(from, parts.join('/'), false);
        await dir.removeEntry(name, { recursive });
        return true;
    }

    async function infoUnder(from: any, path: string) {
        const handle = await fileIn(from, path, false);
        const file = await handle.getFile();
        return {
            name: file.name,
            path: segments(path).join('/'),
            size: file.size,
            type: file.type ?? '',
            lastModified: file.lastModified ?? 0
        };
    }

    function ensureWorker() {
        if (_worker) return _worker;

        const blob = new Blob([SYNC_WORKER_SOURCE], { type: 'text/javascript' });
        _workerUrl = URL.createObjectURL(blob);
        _worker = new Worker(_workerUrl);
        _worker.onmessage = (e: MessageEvent) => {
            const { id, ok, result, error } = e.data ?? {};
            const request = _pending[id];
            if (!request) return;
            delete _pending[id];
            if (ok) request.resolve(result);
            else request.reject(new Error(error));
        };
        _worker.onerror = () => {
            // The worker died; drop it so the next call builds a fresh one.
            terminateWorker('the OPFS sync worker failed');
        };
        return _worker;
    }

    function terminateWorker(reason = 'the OPFS sync worker was disposed') {
        // Nothing can answer a request once the worker is gone, so fail everything waiting on it
        // rather than leaving those promises pending forever.
        for (const id of Object.keys(_pending)) {
            const request = _pending[id];
            delete _pending[id];
            request.reject(new Error(reason));
        }
        try { _worker?.terminate(); } catch { /* already gone */ }
        if (_workerUrl) URL.revokeObjectURL(_workerUrl);
        _worker = null;
        _workerUrl = null;
    }

    function ask(message: any) {
        return new Promise<any>((resolve, reject) => {
            const worker = ensureWorker();
            const id = String(_nextRequest++);
            _pending[id] = { resolve, reject };
            worker.postMessage({ ...message, id });
        });
    }

    butil.originPrivateFileSystem = {
        // Shared with the storage buckets module, which runs these against a bucket's own root.
        walk,
        listUnder,
        readTextUnder,
        readBytesUnder,
        writeUnder,
        removeUnder,
        infoUnder,

        isSupported() { return typeof (window.navigator as any).storage?.getDirectory === 'function'; },
        async isSyncAccessSupported() {
            if (typeof (window.navigator as any).storage?.getDirectory !== 'function') return false;
            if (typeof Worker !== 'function') return false;
            // The window cannot see createSyncAccessHandle even where it exists, so the worker is
            // asked - which also proves the worker itself can be created and reached.
            try { return await ask({ op: 'probe' }) === true; } catch { return false; }
        },
        async list(path: string) {
            try { return await listUnder(await root(), path); } catch { return []; }
        },
        async createDirectory(path: string) {
            try { await walk(await root(), path, true); return true; } catch { return false; }
        },
        async exists(path: string) {
            const parts = segments(path);
            const name = parts.pop();
            if (!name) return true; // the root itself
            try {
                const dir = await walk(await root(), parts.join('/'), false);
                for await (const entry of dir.values()) {
                    if (entry.name === name) return true;
                }
                return false;
            } catch {
                return false;
            }
        },
        async getInfo(path: string) {
            try { return await infoUnder(await root(), path); } catch { return null; }
        },
        async readText(path: string) {
            try { return await readTextUnder(await root(), path); } catch { return null; }
        },
        async readBytes(path: string) {
            try { return await readBytesUnder(await root(), path); } catch { return null; }
        },
        async write(path: string, text: string | null, bytes: Uint8Array | null) {
            try { return await writeUnder(await root(), path, text, bytes); } catch { return false; }
        },
        async remove(path: string, recursive: boolean) {
            try { return await removeUnder(await root(), path, recursive); } catch { return false; }
        },
        async move(path: string, destination: string) {
            // FileSystemHandle.move() is Chromium-only; everywhere else this is a copy + delete
            // rather than a rename, which the caller cannot tell apart and does not need to.
            try {
                const from = await root();
                const handle = await fileIn(from, path, false);
                const parts = segments(destination);
                const name = parts.pop();
                if (!name) return false;

                if (typeof handle.move === 'function') {
                    const target = await walk(from, parts.join('/'), true);
                    await handle.move(target, name);
                    return true;
                }

                // Moving a file onto itself is a no-op, and has to be caught here: copy + delete
                // would write the file and then remove what it just wrote.
                if (segments(path).join('/') === segments(destination).join('/')) return true;

                await writeUnder(from, destination, null, new Uint8Array(await (await handle.getFile()).arrayBuffer()));
                await removeUnder(from, path, false);
                return true;
            } catch {
                return false;
            }
        },
        async clear() {
            try {
                const dir = await root();
                const names: string[] = [];
                for await (const entry of dir.values()) names.push(entry.name);
                for (const name of names) await dir.removeEntry(name, { recursive: true });
                return true;
            } catch {
                return false;
            }
        },

        async syncRead(path: string, offset: number, length: number) {
            try { return await ask({ op: 'read', path, offset, length }); } catch { return null; }
        },
        async syncWrite(path: string, data: Uint8Array, offset: number, truncate: boolean) {
            // The worker needs its own copy: the Uint8Array Blazor handed us is structured-cloned
            // rather than transferred, so the caller's buffer stays valid either way.
            try { return await ask({ op: 'write', path, data, offset, truncate }); } catch { return -1; }
        },
        async syncAppend(path: string, data: Uint8Array) {
            try { return await ask({ op: 'append', path, data }); } catch { return -1; }
        },
        async syncTruncate(path: string, size: number) {
            try { return await ask({ op: 'truncate', path, size }); } catch { return false; }
        },
        async syncSize(path: string) {
            try { return await ask({ op: 'size', path }); } catch { return -1; }
        },
        disposeAll() { terminateWorker(); }
    };
}(BitButil));
