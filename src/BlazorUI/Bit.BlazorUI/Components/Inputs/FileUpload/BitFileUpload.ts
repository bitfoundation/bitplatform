namespace BitBlazorUI {
    export class FileUpload {
        private static readonly IMAGE_SIZE_CONCURRENCY = 8;

        private static _fileUploaders: BitFileUploader[] = [];

        public static async setup(
            id: string,
            dotnetReference: DotNetObject,
            inputElement: HTMLInputElement,
            append: boolean,
            uploadEndpointUrl: string | undefined,
            headers: Record<string, string> | undefined,
            method: string | undefined,
            withCredentials: boolean,
            timeout: number,
            fieldName: string | undefined,
            showPreview: boolean,
            readImageDimensions: boolean) {

            if (!append) {
                FileUpload.clear(id);
            }

            const existingUploaders = append ? FileUpload._fileUploaders.filter(u => u.id === id) : [];
            // a reduce instead of a spread into Math.max, since a selection can carry
            // more items than the argument limit of a function call.
            const lastIndex = existingUploaders.reduce((max, u) => u.index + 1 > max ? u.index + 1 : max, 0);
            const files = Array.from(inputElement.files!).map((file, index) => ({
                name: file.name,
                size: file.size,
                type: file.type,
                lastModified: file.lastModified,
                previewUrl: (showPreview && file.type.startsWith('image/')) ? URL.createObjectURL(file) : null,
                fileId: Utils.uuidv4(),
                file: file,
                index: (index + lastIndex),
                width: null as number | null,
                height: null as number | null
            }));

            files.forEach((f) => {
                const h = { ...(headers || {}), ...{ 'BIT_FILE_ID': f.fileId } };
                const uploader = new BitFileUploader(id, dotnetReference, f.file, uploadEndpointUrl, h, f.index,
                    method || 'POST', withCredentials, timeout, fieldName || 'file', f.previewUrl);
                FileUpload._fileUploaders.push(uploader);
            });

            // the input has to be emptied before awaiting anything, otherwise selecting the same file
            // right after would not raise a change event.
            inputElement.value = '';

            if (readImageDimensions) {
                await FileUpload.readImageSizes(files.filter(f => f.type.startsWith('image/')));
            }

            // the File itself is only of use on this side, so it is left out of the interop payload.
            return files.map(({ file, ...info }) => info);
        }

        public static upload(
            id: string,
            from: number,
            to: number,
            index: number,
            uploadUrl: string | null,
            headers: Record<string, string> = {},
            formFields: Record<string, string> = {}): void {

            const uploaders = FileUpload._fileUploaders.filter(u => u.id === id);

            if (index === -1) {
                uploaders.forEach(u => u.upload(from, to, uploadUrl, headers, formFields));
            } else {
                const uploader = uploaders.find(u => u.index === index);
                uploader?.upload(from, to, uploadUrl, headers, formFields);
            }
        }

        public static pause(id: string, index: number): void {
            const uploaders = FileUpload._fileUploaders.filter(u => u.id === id);

            if (index === -1) {
                uploaders.forEach(u => u.pause());
            } else {
                const uploader = uploaders.find(u => u.index === index);
                uploader?.pause();
            }
        }

        public static setupDragDrop(
            dropZoneElement: HTMLElement,
            inputElement: HTMLInputElement,
            dragClass: string,
            dragStyle: string | null,
            allowDrop: boolean,
            allowPaste: boolean,
            expandDirectories: boolean,
            dropZoneSelector: string | null) {

            let dragCounter = 0;
            let dragClasses = dragClass.split(' ').filter(c => c.length > 0);
            // the inline style every zone carried before the drag state was applied to it, so that the
            // element the app owns gets exactly its own style back rather than losing it.
            const originalStyles = new Map<HTMLElement, string | null>();

            // a page can well point two uploaders at one shared zone, so a drop or a paste landing in it
            // is claimed by the first of them to see it rather than handed to every one of them.
            const handledFlag = '__bitUplHandled';

            function claim(e: Event) {
                if ((e as any)[handledFlag]) return false;

                (e as any)[handledFlag] = true;

                return true;
            }

            // the root is always a drop zone; the selector adds whatever else the app nominates, which is
            // how a whole form - or the page - accepts a drop while the browse button stays where it is.
            // the drag listeners sit on the document and ask this rather than being attached to the zones
            // themselves, so an element the app renders after this component - or replaces later on - is a
            // drop zone from the moment it matches the selector, with nothing to re-attach.
            function isInZone(target: EventTarget | null): boolean {
                if (!(target instanceof Node)) return false;

                if (dropZoneElement.contains(target)) return true;

                if (!dropZoneSelector) return false;

                const element = target instanceof Element ? target : target.parentElement;

                try {
                    return element?.closest(dropZoneSelector) != null;
                } catch {
                    // an invalid selector leaves the component with its own root as the only zone.
                    return false;
                }
            }

            function resolveZones(): HTMLElement[] {
                const list: HTMLElement[] = [dropZoneElement];

                if (!dropZoneSelector) return list;

                try {
                    document.querySelectorAll(dropZoneSelector).forEach(e => {
                        if (e !== dropZoneElement && e instanceof HTMLElement) list.push(e);
                    });
                } catch { /* see isInZone */ }

                return list;
            }

            // the zones the drag state is currently painted on, captured when it was applied so that it
            // comes off exactly the elements it went on, whatever the DOM does in between.
            let zones: HTMLElement[] = [];

            function hasFiles(e: DragEvent) {
                return !!e.dataTransfer && Array.prototype.includes.call(e.dataTransfer.types, 'Files');
            }

            function canAcceptDrop(e: DragEvent) {
                return allowDrop && !inputElement.disabled && hasFiles(e);
            }

            function paintZone(zone: HTMLElement) {
                zone.classList.add(...dragClasses);

                if (!dragStyle) return;
                const original = zone.getAttribute('style');
                originalStyles.set(zone, original);
                zone.setAttribute('style', [original, dragStyle].filter(s => s).join(';'));
            }

            function applyDragStyling() {
                zones = resolveZones();

                zones.forEach(zone => paintZone(zone));
            }

            // a zone can be rendered - or start matching the selector - while the drag is already in
            // flight, and a zone that takes the drop is a zone that shows it is about to. the ones already
            // painted are left alone so that the style they had before the drag is remembered once rather
            // than overwritten with the dragging one.
            // the painted zones are asked by containment rather than by running the selector again, so this
            // costs nothing on a dragover, which fires several times a second.
            function isPainted(target: EventTarget | null) {
                return target instanceof Node && zones.some(zone => zone.contains(target));
            }

            function syncDragStyling() {
                resolveZones().forEach(zone => {
                    if (zones.indexOf(zone) >= 0) return;

                    zones.push(zone);
                    paintZone(zone);
                });
            }

            function clearDragStyling() {
                zones.forEach(zone => {
                    zone.classList.remove(...dragClasses);

                    if (!dragStyle) return;
                    const original = originalStyles.get(zone);
                    if (original) {
                        zone.setAttribute('style', original);
                    } else {
                        zone.removeAttribute('style');
                    }
                    originalStyles.delete(zone);
                });

                zones = [];
            }

            function addDragState() {
                dragCounter++;
                if (dragCounter > 1) {
                    syncDragStyling();
                    return;
                }

                applyDragStyling();
            }

            function removeDragState(force: boolean) {
                if (dragCounter === 0) return;

                dragCounter = force ? 0 : dragCounter - 1;
                if (dragCounter > 0) return;

                clearDragStyling();
            }

            function onDragEnter(e: DragEvent) {
                if (!isInZone(e.target)) return;

                e.preventDefault();
                if (!canAcceptDrop(e)) return;

                addDragState();
            }

            function onDragOver(e: DragEvent) {
                if (!isInZone(e.target)) return;

                // the default must always be prevented, otherwise the browser navigates away
                // to the dropped file and the app state gets lost.
                e.preventDefault();

                if (!e.dataTransfer) return;

                const accepts = canAcceptDrop(e);

                // gives the OS the correct drag cursor (a copy badge or a no-drop sign).
                e.dataTransfer.dropEffect = accepts ? 'copy' : 'none';

                // a zone can also come up underneath a pointer that is already over it, which leaves no
                // dragenter of its own for it to be noticed by.
                if (accepts && dragCounter > 0 && !isPainted(e.target)) {
                    syncDragStyling();
                }
            }

            function onDragLeave(e: DragEvent) {
                if (!isInZone(e.target)) return;

                e.preventDefault();
                if (!hasFiles(e)) return;

                removeDragState(false);
            }

            // a drag that ends without a matching dragleave (cancelled with Escape, released outside the
            // window, or dropped on something else) would otherwise leave the counter above zero and
            // the drag classes and inline style stuck on the drop zone.
            function onDragCancel() {
                removeDragState(true);
            }

            function setFiles(files: File[] | FileList) {
                const list = Array.from(files as ArrayLike<File>);
                if (list.length === 0) return;

                // a directory input always hands over many files through the dialog,
                // so a dropped folder must not be trimmed down to a single file either.
                const acceptsMany = inputElement.multiple || inputElement.webkitdirectory;

                if (!acceptsMany && list.length > 1) {
                    // the file dialog can never hand over more than one file without the multiple attribute,
                    // so a multi-file drop or paste is trimmed down to its first file too.
                    const dataTransfer = new DataTransfer();
                    dataTransfer.items.add(list[0]);
                    inputElement.files = dataTransfer.files;
                } else if (files instanceof FileList) {
                    inputElement.files = files;
                } else {
                    const dataTransfer = new DataTransfer();
                    list.forEach(f => dataTransfer.items.add(f));
                    inputElement.files = dataTransfer.files;
                }

                const event = new Event('change', { bubbles: true });
                inputElement.dispatchEvent(event);
            }

            function onDrop(e: DragEvent) {
                if (!isInZone(e.target)) return;

                e.preventDefault();

                // two uploaders can well be pointed at one shared zone, and the files belong to
                // whichever of them takes the drop rather than to both of them at once.
                if (!claim(e)) return;

                removeDragState(true);

                if (!allowDrop || inputElement.disabled || !e.dataTransfer) return;

                if (!expandDirectories) {
                    setFiles(e.dataTransfer.files);
                    return;
                }

                // the entries of a DataTransfer are only readable synchronously inside the event handler,
                // so they get collected first and walked afterwards.
                const entries = FileUpload.readDroppedEntries(e.dataTransfer);
                const fallback = Array.from(e.dataTransfer.files);

                FileUpload.collectEntries(entries)
                    .then(files => setFiles(files.length ? files : fallback))
                    .catch(() => setFiles(fallback));
            }

            // a paste is delivered to whatever holds the focus, and neither the drop zone - a plain div -
            // nor the hidden file input can ever hold it, so the listener sits on the document and decides
            // for itself whether the paste was meant for this component: the focus being somewhere inside
            // it, or nothing on the page holding the focus at all. in that second case the paste belongs to
            // no one in particular, so whichever paste enabled upload on the page sees it first takes it.
            function onPaste(e: ClipboardEvent) {
                if (!allowPaste || inputElement.disabled) return;
                if (!e.clipboardData || e.clipboardData.files.length === 0) return;

                const focused = document.activeElement;
                const isFocusedHere = focused !== null && (focused === inputElement || isInZone(focused));

                // the focus sitting inside some unrelated element is a paste meant for that element.
                if (!isFocusedHere && focused !== null && focused !== document.body) return;

                // the focus can be inside a zone two uploaders share just as well as it can be nowhere at
                // all, so either way the files belong to one of them rather than to both at once.
                if (!claim(e)) return;

                setFiles(e.clipboardData.files);
            }

            document.addEventListener('dragenter', onDragEnter);
            document.addEventListener('dragover', onDragOver);
            document.addEventListener('dragleave', onDragLeave);
            document.addEventListener('drop', onDrop);
            document.addEventListener('dragend', onDragCancel);
            document.addEventListener('paste', onPaste);
            // the window listener only cleans the state up, it never prevents the default,
            // so a drop landing anywhere else on the page keeps behaving as it did.
            window.addEventListener('dragend', onDragCancel);
            window.addEventListener('drop', onDragCancel);

            return {
                update: (
                    newAllowDrop: boolean,
                    newAllowPaste: boolean,
                    newExpandDirectories: boolean,
                    newDragClass: string,
                    newDragStyle: string | null,
                    newDropZoneSelector: string | null) => {

                    allowDrop = newAllowDrop;
                    allowPaste = newAllowPaste;
                    expandDirectories = newExpandDirectories;

                    if (newDropZoneSelector !== dropZoneSelector) {
                        // the zones of the old selector are already wearing the drag state, and only they
                        // can be asked to take it off again.
                        const isDragging = dragCounter > 0;
                        if (isDragging) {
                            clearDragStyling();
                        }

                        dropZoneSelector = newDropZoneSelector;

                        if (isDragging) {
                            applyDragStyling();
                        }
                    }

                    if (newDragClass !== dragClass || newDragStyle !== dragStyle) {
                        // an ongoing drag is already showing the old class and style, which have to come off
                        // before they get replaced, otherwise nothing would ever take them off again.
                        const isDragging = dragCounter > 0;
                        if (isDragging) {
                            clearDragStyling();
                        }

                        dragClass = newDragClass;
                        dragClasses = dragClass.split(' ').filter(c => c.length > 0);
                        dragStyle = newDragStyle;

                        if (isDragging) {
                            applyDragStyling();
                        }
                    }

                    if (!allowDrop) {
                        removeDragState(true);
                    }
                },
                dispose: () => {
                    // a zone the app owns outlives this component, so anything painted on it while a drag
                    // was still in flight has to come off before the listeners that would have done it go.
                    removeDragState(true);

                    document.removeEventListener('dragenter', onDragEnter);
                    document.removeEventListener('dragover', onDragOver);
                    document.removeEventListener('dragleave', onDragLeave);
                    document.removeEventListener('drop', onDrop);
                    document.removeEventListener('dragend', onDragCancel);
                    document.removeEventListener('paste', onPaste);
                    window.removeEventListener('dragend', onDragCancel);
                    window.removeEventListener('drop', onDragCancel);
                }
            }

        }

        public static browse(inputElement: HTMLInputElement) {
            inputElement.click();
        }

        public static clear(id: string) {
            // an uploader dropped while its request is still in flight has to be fully detached first,
            // otherwise the late response of the old request could get attributed to a newly selected
            // file occupying the same index.
            FileUpload._fileUploaders.filter(u => u.id === id).forEach(u => u.detach());

            FileUpload._fileUploaders = FileUpload._fileUploaders.filter(u => u.id !== id);
        }

        // a file that was taken out of the list is never uploaded again, but its uploader stays in place
        // so that the indexes of the files after it keep pointing at the right uploader. what it does give
        // up is everything it was holding on to: the File itself, which the browser keeps in memory - and
        // for a picked file, on disk - for as long as anything references it, and its preview object URL.
        public static release(id: string, index: number) {
            const uploader = FileUpload._fileUploaders.find(u => u.id === id && u.index === index);

            uploader?.detach();
        }

        public static reset(id: string, inputElement: HTMLInputElement) {
            FileUpload.clear(id);
            inputElement.value = '';
        }

        private static async readImageSizes(images: { file: File, width: number | null, height: number | null }[]): Promise<void> {
            // a directory selection can carry thousands of images, and decoding them all at once would
            // spike the memory and stall the tab, so a fixed window of workers walks the list instead.
            let next = 0;

            const worker = async () => {
                while (next < images.length) {
                    const image = images[next++];
                    const size = await FileUpload.readImageSize(image.file);
                    image.width = size.width;
                    image.height = size.height;
                }
            };

            await Promise.all(Array.from({ length: Math.min(FileUpload.IMAGE_SIZE_CONCURRENCY, images.length) }, worker));
        }

        private static async readImageSize(file: File): Promise<{ width: number | null, height: number | null }> {
            // createImageBitmap decodes off the main thread and needs no DOM, so it is the fast path.
            if (typeof createImageBitmap === 'function') {
                try {
                    const bitmap = await createImageBitmap(file);
                    const size = { width: bitmap.width, height: bitmap.height };
                    bitmap.close();
                    return size;
                } catch { /* falls back to the image element below (e.g. for SVG on some browsers) */ }
            }

            return new Promise(resolve => {
                const url = URL.createObjectURL(file);
                const image = new Image();

                const finish = (width: number | null, height: number | null) => {
                    URL.revokeObjectURL(url);
                    resolve({ width, height });
                };

                image.onload = () => finish(image.naturalWidth, image.naturalHeight);
                image.onerror = () => finish(null, null);
                image.src = url;
            });
        }

        private static readDroppedEntries(dataTransfer: DataTransfer): any[] {
            const items = dataTransfer.items;
            if (!items || items.length === 0) return [];

            const entries: any[] = [];
            for (let i = 0; i < items.length; i++) {
                const item = items[i] as any;
                if (item.kind !== 'file') continue;

                entries.push(item.webkitGetAsEntry ? item.webkitGetAsEntry() : item.getAsFile());
            }

            return entries.filter(e => !!e);
        }

        private static async collectEntries(entries: any[]): Promise<File[]> {
            const files: File[] = [];

            for (const entry of entries) {
                await FileUpload.collectEntry(entry, files);
            }

            return files;
        }

        private static async collectEntry(entry: any, files: File[]): Promise<void> {
            if (entry instanceof File) {
                files.push(entry);
                return;
            }

            if (entry.isFile) {
                return new Promise<void>(resolve => entry.file(
                    (file: File) => { files.push(file); resolve(); },
                    () => resolve()));
            }

            if (entry.isDirectory) {
                const reader = entry.createReader();

                // readEntries only returns a batch at a time, so it must be called until it comes back empty.
                while (true) {
                    const batch: any[] = await new Promise(resolve => reader.readEntries(
                        (result: any[]) => resolve(result),
                        () => resolve([])));

                    if (batch.length === 0) break;

                    for (const child of batch) {
                        await FileUpload.collectEntry(child, files);
                    }
                }
            }
        }
    }

    class BitFileUploader {
        id: string;
        dotnetReference: DotNetObject;
        file: File | null;
        uploadUrl: string | undefined;
        headers: Record<string, string>;
        index: number;
        method: string;
        withCredentials: boolean;
        timeout: number;
        fieldName: string;
        previewUrl: string | null;

        private xhr: XMLHttpRequest = new XMLHttpRequest();

        constructor(
            id: string,
            dotnetReference: DotNetObject,
            file: File,
            uploadEndpointUrl: string | undefined,
            headers: Record<string, string>,
            index: number,
            method: string,
            withCredentials: boolean,
            timeout: number,
            fieldName: string,
            previewUrl: string | null) {

            this.previewUrl = previewUrl;
            this.id = id;
            this.dotnetReference = dotnetReference;
            this.file = file;
            this.uploadUrl = uploadEndpointUrl;
            this.headers = headers;
            this.index = index;
            this.method = method;
            this.withCredentials = withCredentials;
            this.timeout = timeout;
            this.fieldName = fieldName;

            if (index < 0) return;

            this.xhr.upload.onprogress = function (e: ProgressEvent) {
                if (e.lengthComputable) {
                    dotnetReference.invokeMethodAsync("HandleChunkUploadProgress", index, e.loaded);
                }
            };

            const me = this;
            this.xhr.onreadystatechange = function (event) {
                if (me.xhr.readyState === 4) {
                    dotnetReference.invokeMethodAsync("HandleChunkUpload", index, me.xhr.status, me.xhr.responseText);
                }
            };
        }

        upload(from: number, to: number, uploadUrl: string | null, headers: Record<string, string>, formFields: Record<string, string>): void {
            const file = this.file;
            if (file === null) return;

            const data: FormData = new FormData();
            const chunk = file.slice(from, to);
            data.append(this.fieldName, chunk, file.name);

            // the extra fields go in after the content, so a field accidentally named like the file field
            // cannot shadow the file itself for the servers that only read the first value of a name.
            Object.keys(formFields).forEach(f => {
                data.append(f, formFields[f]);
            });

            const url = uploadUrl || this.uploadUrl;

            if (!url) {
                // silently swallowing the missing URL would leave the file stuck as in-progress forever,
                // so it gets reported as a failed upload instead.
                this.dotnetReference.invokeMethodAsync("HandleChunkUpload", this.index, 0, 'The upload URL is not provided.');
                return;
            }

            this.xhr.open(this.method, url, true);

            this.xhr.withCredentials = this.withCredentials;

            if (this.timeout > 0) {
                // a timed out request aborts with the readyState 4 and the status 0,
                // which the .NET side then reports as a failed upload.
                this.xhr.timeout = this.timeout;
            }

            Object.keys(this.headers).forEach(h => {
                this.xhr.setRequestHeader(h, this.headers[h]);
            });

            Object.keys(headers).forEach(h => {
                this.xhr.setRequestHeader(h, headers[h]);
            });

            this.xhr.send(data);
        }

        pause(): void {
            this.xhr.abort();
        }

        detach(): void {
            this.xhr.upload.onprogress = null;
            this.xhr.onreadystatechange = null;
            this.xhr.abort();
            this.revokePreview();
            // the File is what the browser is really holding on to, so it goes last and it goes for good.
            this.file = null;
        }

        // an object URL keeps the whole file alive in memory until it is revoked, so a preview that is
        // not on screen anymore has to give it back rather than waiting for the tab to be closed.
        revokePreview(): void {
            if (!this.previewUrl) return;

            URL.revokeObjectURL(this.previewUrl);
            this.previewUrl = null;
        }
    }
}
