namespace BitBlazorUI {
    export class FileInput {
        private static _fileInputs: BitFileInputItem[] = [];

        public static setup(
            id: string,
            dotnetReference: DotNetObject,
            inputElement: HTMLInputElement,
            append: boolean) {

            if (!append) {
                FileInput.clear(id);
            }

            const lastIndex = append ? FileInput._fileInputs.filter(f => f.id === id).length : 0;
            const files = Array.from(inputElement.files!).map((file, index) => ({
                name: file.name,
                size: file.size,
                type: file.type,
                fileId: Utils.uuidv4(),
                file: file,
                index: (index + lastIndex)
            }));

            files.forEach((f) => {
                const inputItem = new BitFileInputItem(id, f.file, f.index);
                FileInput._fileInputs.push(inputItem);
            });

            inputElement.value = '';

            return files;
        }

        public static setupDragDrop(dropZoneElement: HTMLElement, inputElement: HTMLInputElement) {

            function onDragHover(e: DragEvent) {
                e.preventDefault();
            }

            function onDragLeave(e: DragEvent) {
                e.preventDefault();
            }

            function onDrop(e: DragEvent) {
                e.preventDefault();
                inputElement.files = e.dataTransfer!.files;
                const event = new Event('change', { bubbles: true });
                inputElement.dispatchEvent(event);
            }

            function onPaste(e: ClipboardEvent) {
                inputElement.files = e.clipboardData!.files;
                const event = new Event('change', { bubbles: true });
                inputElement.dispatchEvent(event);
            }

            dropZoneElement.addEventListener("dragenter", onDragHover);
            dropZoneElement.addEventListener("dragover", onDragHover);
            dropZoneElement.addEventListener("dragleave", onDragLeave);
            dropZoneElement.addEventListener("drop", onDrop);
            dropZoneElement.addEventListener('paste', onPaste);

            return {
                dispose: () => {
                    dropZoneElement.removeEventListener('dragenter', onDragHover);
                    dropZoneElement.removeEventListener('dragover', onDragHover);
                    dropZoneElement.removeEventListener('dragleave', onDragLeave);
                    dropZoneElement.removeEventListener("drop", onDrop);
                    dropZoneElement.removeEventListener('paste', onPaste);
                }
            }

        }

        public static browse(inputElement: HTMLInputElement) {
            inputElement.click();
        }

        public static clear(id: string) {
            FileInput._fileInputs = FileInput._fileInputs.filter(f => f.id !== id);
        }

        public static reset(id: string, inputElement: HTMLInputElement) {
            FileInput.clear(id);
            inputElement.value = '';
        }
    }

    class BitFileInputItem {
        id: string;
        file: File;
        index: number;

        constructor(id: string, file: File, index: number) {
            this.id = id;
            this.file = file;
            this.index = index;
        }
    }
}
