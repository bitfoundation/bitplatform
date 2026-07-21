namespace BitBlazorUI {
    export class FileInput {
        private static _fileInputs: BitFileInputItem[] = [];

        public static setup(
            id: string,
            inputElement: HTMLInputElement,
            append: boolean,
            showPreview: boolean) {

            if (!append) {
                FileInput.clear(id);
            }

            const lastIndex = append ? FileInput._fileInputs.filter(f => f.id === id).length : 0;
            const files = Array.from(inputElement.files!).map((file, index) => ({
                name: file.name,
                size: file.size,
                type: file.type,
                lastModified: file.lastModified,
                previewUrl: (showPreview && file.type.startsWith('image/')) ? URL.createObjectURL(file) : null,
                fileId: Utils.uuidv4(),
                file: file,
                index: (index + lastIndex)
            }));

            files.forEach((f) => {
                const inputItem = new BitFileInputItem(id, f.fileId, f.file, f.index, f.previewUrl);
                FileInput._fileInputs.push(inputItem);
            });

            inputElement.value = '';

            return files;
        }

        public static setupDragDrop(dropZoneElement: HTMLElement, inputElement: HTMLInputElement, dragClass: string) {
            let dragCounter = 0;
            const dragClasses = dragClass.split(' ').filter(c => c.length > 0);

            function hasFiles(e: DragEvent) {
                return !!e.dataTransfer && Array.prototype.includes.call(e.dataTransfer.types, 'Files');
            }

            function onDragEnter(e: DragEvent) {
                e.preventDefault();
                if (!hasFiles(e)) return;

                dragCounter++;
                dropZoneElement.classList.add(...dragClasses);
            }

            function onDragOver(e: DragEvent) {
                e.preventDefault();
            }

            function onDragLeave(e: DragEvent) {
                e.preventDefault();
                if (!hasFiles(e)) return;

                dragCounter--;
                if (dragCounter <= 0) {
                    dragCounter = 0;
                    dropZoneElement.classList.remove(...dragClasses);
                }
            }

            function setFiles(files: FileList) {
                if (files.length === 0) return;

                if (!inputElement.multiple && files.length > 1) {
                    const dataTransfer = new DataTransfer();
                    dataTransfer.items.add(files[0]);
                    inputElement.files = dataTransfer.files;
                } else {
                    inputElement.files = files;
                }

                const event = new Event('change', { bubbles: true });
                inputElement.dispatchEvent(event);
            }

            function onDrop(e: DragEvent) {
                e.preventDefault();
                dragCounter = 0;
                dropZoneElement.classList.remove(...dragClasses);

                if (inputElement.disabled) return;

                setFiles(e.dataTransfer!.files);
            }

            function onPaste(e: ClipboardEvent) {
                if (inputElement.disabled) return;
                if (!e.clipboardData || e.clipboardData.files.length === 0) return;

                setFiles(e.clipboardData.files);
            }

            dropZoneElement.addEventListener("dragenter", onDragEnter);
            dropZoneElement.addEventListener("dragover", onDragOver);
            dropZoneElement.addEventListener("dragleave", onDragLeave);
            dropZoneElement.addEventListener("drop", onDrop);
            dropZoneElement.addEventListener('paste', onPaste);

            return {
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
