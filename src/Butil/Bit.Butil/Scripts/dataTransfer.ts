var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface DropEntry { element: any; handlers: { [type: string]: (e: DragEvent) => void }; }
    // draggable is remembered as it was found: an element that was already draggable (or explicitly
    // draggable="false") has to be left that way when the source is removed, not stripped bare.
    interface SourceEntry { element: any; handler: (e: DragEvent) => void; draggable: string | null; }

    const _drops: { [id: string]: DropEntry } = {};
    const _sources: { [id: string]: SourceEntry } = {};
    // Dropped files, kept alive past the event that delivered them. A DataTransfer's contents are
    // only readable during the event; the File objects themselves stay valid afterwards, so this is
    // what lets .NET read the bytes on its own schedule instead of inside the handler.
    const _files: { [id: string]: File } = {};

    butil.dataTransfer = {
        isSupported() { return typeof DataTransfer === 'function'; },

        // Wires up the whole drop target, including the part everyone forgets: a drop event only
        // fires on an element whose dragover handler called preventDefault. Doing it here means a
        // target that works, rather than one that silently never fires.
        listenForDrop(dotNetRef: any, id: string, element: any, dropEffect: string) {
            if (!element) return false;

            const handlers: { [type: string]: (e: DragEvent) => void } = {};

            const over = (e: DragEvent) => {
                e.preventDefault();
                // Setting dropEffect is what changes the cursor, and it has to be set on every
                // dragover - the browser resets it between events.
                if (e.dataTransfer) e.dataTransfer.dropEffect = dropEffect as any;
            };

            handlers['dragenter'] = over;
            handlers['dragover'] = over;

            handlers['drop'] = (e: DragEvent) => {
                e.preventDefault();

                const transfer = e.dataTransfer;
                const files: any[] = [];
                const items: any = {};

                if (transfer) {
                    for (const file of Array.from(transfer.files ?? [])) {
                        const fileId = butil.utils.randomUUID();
                        _files[fileId] = file;
                        files.push({
                            id: fileId,
                            name: file.name,
                            size: file.size,
                            type: file.type,
                            // Named for what it is - the .NET side exposes a DateTimeOffset built
                            // from it, and the two have to agree on the property name to bind.
                            lastModifiedMilliseconds: file.lastModified
                        });
                    }

                    // Everything that is not a file: text/plain, text/uri-list, text/html, and
                    // whatever custom type the drag source set. getData outside the drop event
                    // returns an empty string, so it is read here and handed over whole.
                    for (const type of Array.from(transfer.types ?? [])) {
                        if (type === 'Files') continue;
                        items[type] = transfer.getData(type);
                    }
                }

                butil.utils.dispatch(dotNetRef, 'InvokeDrop', id, files, items);
            };

            for (const type of Object.keys(handlers)) element.addEventListener(type, handlers[type]);
            _drops[id] = { element, handlers };
            return true;
        },

        removeDropListener(id: string) {
            const entry = _drops[id];
            if (!entry) return;
            delete _drops[id];
            for (const type of Object.keys(entry.handlers)) {
                entry.element.removeEventListener(type, entry.handlers[type]);
            }
        },

        // The drag source. The payload is configured up front rather than produced by a callback,
        // because dragstart's data has to be set synchronously and a round trip to .NET is not.
        configureSource(id: string, element: any, items: any, effectAllowed: string, dragImage: any, imageX: number, imageY: number) {
            if (!element) return false;

            const handler = (e: DragEvent) => {
                const transfer = e.dataTransfer;
                if (!transfer) return;

                transfer.effectAllowed = effectAllowed as any;
                for (const type of Object.keys(items ?? {})) transfer.setData(type, items[type]);

                // The drag image has to be an element that is rendered and visible at the moment of
                // the call - a display:none one produces no image at all, which is the usual reason
                // a custom drag image does not appear.
                if (dragImage) {
                    try { transfer.setDragImage(dragImage, imageX, imageY); } catch { /* not a valid image source */ }
                }
            };

            const draggable = element.getAttribute('draggable');
            element.setAttribute('draggable', 'true');
            element.addEventListener('dragstart', handler);
            _sources[id] = { element, handler, draggable };
            return true;
        },

        removeSource(id: string) {
            const entry = _sources[id];
            if (!entry) return;
            delete _sources[id];
            entry.element.removeEventListener('dragstart', entry.handler);
            if (entry.draggable === null) entry.element.removeAttribute('draggable');
            else entry.element.setAttribute('draggable', entry.draggable);
        },

        async readFile(fileId: string) {
            const file = _files[fileId];
            if (!file) return null;
            try { return new Uint8Array(await file.arrayBuffer()); } catch { return null; }
        },

        async readFileText(fileId: string) {
            const file = _files[fileId];
            if (!file) return null;
            try { return await file.text(); } catch { return null; }
        },

        // An object URL for a dropped file, for showing an image without reading its bytes into
        // .NET first. The caller owns it and has to revoke it.
        objectUrl(fileId: string) {
            const file = _files[fileId];
            return file ? URL.createObjectURL(file) : null;
        },

        releaseFile(fileId: string) { delete _files[fileId]; },

        disposeAll() {
            for (const id of Object.keys(_drops)) butil.dataTransfer.removeDropListener(id);
            for (const id of Object.keys(_sources)) butil.dataTransfer.removeSource(id);
            for (const id of Object.keys(_files)) delete _files[id];
        }
    };
}(BitButil));
