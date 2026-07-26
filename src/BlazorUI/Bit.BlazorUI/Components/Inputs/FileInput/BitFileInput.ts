namespace BitBlazorUI {
    export class FileInput {
        private static _fileInputs: BitFileInputItem[] = [];

        public static async setup(
            id: string,
            inputElement: HTMLInputElement,
            append: boolean,
            showPreview: boolean,
            readImageDimensions: boolean) {

            if (!append) {
                FileInput.clear(id);
            }

            const existingItems = append ? FileInput._fileInputs.filter(f => f.id === id) : [];
            const lastIndex = existingItems.length ? Math.max(...existingItems.map(f => f.index)) + 1 : 0;
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
                const inputItem = new BitFileInputItem(id, f.fileId, f.file, f.index, f.previewUrl);
                FileInput._fileInputs.push(inputItem);
            });

            // the input has to be emptied before awaiting anything, otherwise selecting the same file
            // right after would not raise a change event.
            inputElement.value = '';

            if (readImageDimensions) {
                await Promise.all(files
                    .filter(f => f.type.startsWith('image/'))
                    .map(async f => {
                        const size = await FileInput.readImageSize(f.file);
                        f.width = size.width;
                        f.height = size.height;
                    }));
            }

            return files;
        }

        public static setupDragDrop(
            dropZoneElement: HTMLElement,
            inputElement: HTMLInputElement,
            dragClass: string,
            dragStyle: string | null,
            allowDrop: boolean,
            allowPaste: boolean,
            expandDirectories: boolean) {

            let dragCounter = 0;
            let originalStyle: string | null = null;
            const dragClasses = dragClass.split(' ').filter(c => c.length > 0);

            function hasFiles(e: DragEvent) {
                return !!e.dataTransfer && Array.prototype.includes.call(e.dataTransfer.types, 'Files');
            }

            function canAcceptDrop(e: DragEvent) {
                return allowDrop && !inputElement.disabled && hasFiles(e);
            }

            function addDragState() {
                dragCounter++;
                if (dragCounter > 1) return;

                dropZoneElement.classList.add(...dragClasses);

                if (!dragStyle) return;
                originalStyle = dropZoneElement.getAttribute('style');
                dropZoneElement.setAttribute('style', [originalStyle, dragStyle].filter(s => s).join(';'));
            }

            function removeDragState(force: boolean) {
                if (dragCounter === 0) return;

                dragCounter = force ? 0 : dragCounter - 1;
                if (dragCounter > 0) return;

                dropZoneElement.classList.remove(...dragClasses);

                if (!dragStyle) return;
                if (originalStyle) {
                    dropZoneElement.setAttribute('style', originalStyle);
                } else {
                    dropZoneElement.removeAttribute('style');
                }
                originalStyle = null;
            }

            function onDragEnter(e: DragEvent) {
                e.preventDefault();
                if (!canAcceptDrop(e)) return;

                addDragState();
            }

            function onDragOver(e: DragEvent) {
                // the default must always be prevented, otherwise the browser navigates away
                // to the dropped file and the app state gets lost.
                e.preventDefault();

                if (!e.dataTransfer) return;

                // gives the OS the correct drag cursor (a copy badge or a no-drop sign).
                e.dataTransfer.dropEffect = canAcceptDrop(e) ? 'copy' : 'none';
            }

            function onDragLeave(e: DragEvent) {
                e.preventDefault();
                if (!hasFiles(e)) return;

                removeDragState(false);
            }

            function setFiles(files: File[] | FileList) {
                const list = Array.from(files as ArrayLike<File>);
                if (list.length === 0) return;

                // a directory input always hands over many files through the dialog,
                // so a dropped folder must not be trimmed down to a single file either.
                const acceptsMany = inputElement.multiple || inputElement.webkitdirectory;

                if (!acceptsMany && list.length > 1) {
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
                e.preventDefault();
                removeDragState(true);

                if (!allowDrop || inputElement.disabled || !e.dataTransfer) return;

                if (!expandDirectories) {
                    setFiles(e.dataTransfer.files);
                    return;
                }

                // the entries of a DataTransfer are only readable synchronously inside the event handler,
                // so they get collected first and walked afterwards.
                const entries = FileInput.readDroppedEntries(e.dataTransfer);
                const fallback = Array.from(e.dataTransfer.files);

                FileInput.collectEntries(entries).then(files => setFiles(files.length ? files : fallback));
            }

            function onPaste(e: ClipboardEvent) {
                if (!allowPaste || inputElement.disabled) return;
                if (!e.clipboardData || e.clipboardData.files.length === 0) return;

                setFiles(e.clipboardData.files);
            }

            dropZoneElement.addEventListener("dragenter", onDragEnter);
            dropZoneElement.addEventListener("dragover", onDragOver);
            dropZoneElement.addEventListener("dragleave", onDragLeave);
            dropZoneElement.addEventListener("drop", onDrop);
            dropZoneElement.addEventListener('paste', onPaste);

            return {
                update: (newAllowDrop: boolean, newAllowPaste: boolean, newExpandDirectories: boolean) => {
                    allowDrop = newAllowDrop;
                    allowPaste = newAllowPaste;
                    expandDirectories = newExpandDirectories;

                    if (!allowDrop) {
                        removeDragState(true);
                    }
                },
                dispose: () => {
                    dropZoneElement.removeEventListener('dragenter', onDragEnter);
                    dropZoneElement.removeEventListener('dragover', onDragOver);
                    dropZoneElement.removeEventListener('dragleave', onDragLeave);
                    dropZoneElement.removeEventListener("drop", onDrop);
                    dropZoneElement.removeEventListener('paste', onPaste);
                }
            }

        }

        public static browse(inputElement: HTMLInputElement) {
            inputElement.click();
        }

        public static removeFile(id: string, fileId: string) {
            const item = FileInput._fileInputs.find(f => f.id === id && f.fileId === fileId);
            if (!item) return;

            if (item.previewUrl) {
                URL.revokeObjectURL(item.previewUrl);
            }

            FileInput._fileInputs = FileInput._fileInputs.filter(f => f !== item);
        }

        public static clear(id: string) {
            FileInput._fileInputs.filter(f => f.id === id && f.previewUrl).forEach(f => URL.revokeObjectURL(f.previewUrl!));

            FileInput._fileInputs = FileInput._fileInputs.filter(f => f.id !== id);
        }

        public static async readContent(id: string, fileId: string): Promise<Uint8Array> {
            const item = FileInput._fileInputs.find(f => f.id === id && f.fileId === fileId);
            if (!item) {
                throw new Error(`File not found: ${fileId}`);
            }

            const buffer = await item.file.arrayBuffer();
            return new Uint8Array(buffer);
        }

        public static reset(id: string, inputElement: HTMLInputElement) {
            FileInput.clear(id);
            inputElement.value = '';
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
                await FileInput.collectEntry(entry, files);
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
                        await FileInput.collectEntry(child, files);
                    }
                }
            }
        }
    }

    class BitFileInputItem {
        id: string;
        fileId: string;
        file: File;
        index: number;
        previewUrl: string | null;

        constructor(id: string, fileId: string, file: File, index: number, previewUrl: string | null) {
            this.id = id;
            this.fileId = fileId;
            this.file = file;
            this.index = index;
            this.previewUrl = previewUrl;
        }
    }
}
