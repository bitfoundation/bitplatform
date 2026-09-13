var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Handles never cross into .NET - only ids do. A FileSystemHandle is a live object with no
    // serializable form, and it is also the capability: holding one is what lets a page keep
    // writing to a file the user picked once.
    const _files: { [id: string]: any } = {};
    const _dirs: { [id: string]: any } = {};

    function describeFile(id: string, handle: any) {
        return { id, name: handle.name, kind: 'file' };
    }

    // The picker's `types` list is what turns "*.*" into "Text files (*.txt)". Entries with no
    // extensions are dropped: the runtime throws on an empty accept map rather than ignoring it.
    function toTypes(accept: any[]) {
        if (!accept?.length) return undefined;
        const types = accept
            .filter(t => t?.extensions?.length)
            .map(t => ({
                description: t.description ?? '',
                accept: { [t.mimeType || '*/*']: t.extensions }
            }));
        return types.length ? types : undefined;
    }

    butil.fileSystem = {
        isSupported() { return typeof (window as any).showOpenFilePicker === 'function'; },
        isDirectorySupported() { return typeof (window as any).showDirectoryPicker === 'function'; },
        async openFilePicker(multiple: boolean, excludeAcceptAllOption: boolean, accept: any[], startIn: string) {
            const pick = (window as any).showOpenFilePicker;
            if (typeof pick !== 'function') return null;

            const options: any = { multiple, excludeAcceptAllOption };
            const types = toTypes(accept);
            if (types) options.types = types;
            if (startIn) options.startIn = startIn;

            try {
                const handles = await pick(options);
                return handles.map((h: any) => {
                    const id = butil.utils.randomUUID();
                    _files[id] = h;
                    return describeFile(id, h);
                });
            } catch {
                // AbortError when the user cancels - not an error worth propagating.
                return null;
            }
        },
        async saveFilePicker(suggestedName: string, excludeAcceptAllOption: boolean, accept: any[], startIn: string) {
            const pick = (window as any).showSaveFilePicker;
            if (typeof pick !== 'function') return null;

            const options: any = { excludeAcceptAllOption };
            if (suggestedName) options.suggestedName = suggestedName;
            const types = toTypes(accept);
            if (types) options.types = types;
            if (startIn) options.startIn = startIn;

            try {
                const handle = await pick(options);
                const id = butil.utils.randomUUID();
                _files[id] = handle;
                return describeFile(id, handle);
            } catch {
                return null;
            }
        },
        async directoryPicker(mode: string, startIn: string) {
            const pick = (window as any).showDirectoryPicker;
            if (typeof pick !== 'function') return null;

            const options: any = {};
            if (mode) options.mode = mode;
            if (startIn) options.startIn = startIn;

            try {
                const handle = await pick(options);
                const id = butil.utils.randomUUID();
                _dirs[id] = handle;
                return { id, name: handle.name, kind: 'directory' };
            } catch {
                return null;
            }
        },
        async listDirectory(dirId: string) {
            const dir = _dirs[dirId];
            if (!dir?.values) return [];
            const entries: any[] = [];
            try {
                for await (const entry of dir.values()) {
                    const id = butil.utils.randomUUID();
                    if (entry.kind === 'file') _files[id] = entry; else _dirs[id] = entry;
                    entries.push({ id, name: entry.name, kind: entry.kind });
                }
            } catch {
                // Permission was revoked between picking the directory and walking it.
                return entries;
            }
            return entries;
        },
        async readText(fileId: string) {
            const handle = _files[fileId];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return await file.text();
            } catch {
                return null;
            }
        },
        async readBytes(fileId: string) {
            const handle = _files[fileId];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return new Uint8Array(await file.arrayBuffer());
            } catch {
                return null;
            }
        },
        async getInfo(fileId: string) {
            const handle = _files[fileId];
            if (!handle?.getFile) return null;
            try {
                const file = await handle.getFile();
                return { name: file.name, size: file.size, type: file.type ?? '', lastModified: file.lastModified ?? 0 };
            } catch {
                return null;
            }
        },
        async write(fileId: string, text: string | null, bytes: Uint8Array | null, keepExistingData: boolean) {
            const handle = _files[fileId];
            if (!handle?.createWritable) return false;
            try {
                const writable = await handle.createWritable({ keepExistingData });
                try {
                    await writable.write(text !== null && text !== undefined ? text : (bytes ?? new Uint8Array()));
                } finally {
                    // close() is what actually commits; skipping it on a write failure would leave
                    // the file locked and the swap file behind.
                    await writable.close();
                }
                return true;
            } catch {
                // No write permission, or the user revoked it since the file was picked.
                return false;
            }
        },
        async queryPermission(handleId: string, write: boolean) {
            const handle = _files[handleId] ?? _dirs[handleId];
            if (!handle?.queryPermission) return 'unsupported';
            try { return await handle.queryPermission({ mode: write ? 'readwrite' : 'read' }); }
            catch { return 'denied'; }
        },
        async requestPermission(handleId: string, write: boolean) {
            const handle = _files[handleId] ?? _dirs[handleId];
            if (!handle?.requestPermission) return 'unsupported';
            try { return await handle.requestPermission({ mode: write ? 'readwrite' : 'read' }); }
            catch { return 'denied'; }
        },
        async getFileInDirectory(dirId: string, name: string, create: boolean) {
            const dir = _dirs[dirId];
            if (!dir?.getFileHandle) return null;
            try {
                const handle = await dir.getFileHandle(name, { create });
                const id = butil.utils.randomUUID();
                _files[id] = handle;
                return describeFile(id, handle);
            } catch {
                // NotFoundError when create is false and the file isn't there.
                return null;
            }
        },
        async removeFromDirectory(dirId: string, name: string, recursive: boolean) {
            const dir = _dirs[dirId];
            if (!dir?.removeEntry) return false;
            try { await dir.removeEntry(name, { recursive }); return true; } catch { return false; }
        },
        release(handleId: string) {
            delete _files[handleId];
            delete _dirs[handleId];
        },
        disposeAll() {
            for (const id of Object.keys(_files)) delete _files[id];
            for (const id of Object.keys(_dirs)) delete _dirs[id];
        }
    };
}(BitButil));
