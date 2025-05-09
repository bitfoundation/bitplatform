﻿namespace BitBlazorUI {
    export class FileUpload {
        private static _fileUploaders: BitFileUploader[] = [];

        public static setup(
            id: string,
            dotnetReference: DotNetObject,
            inputElement: HTMLInputElement,
            append: boolean,
            uploadEndpointUrl: string | undefined,
            headers: Record<string, string> | undefined) {

            if (!append) {
                FileUpload.clear(id);
            }

            const lastIndex = append ? FileUpload._fileUploaders.filter(u => u.id === id).length : 0;
            const files = Array.from(inputElement.files!).map((file, index) => ({
                name: file.name,
                size: file.size,
                type: file.type,
                fileId: Utils.uuidv4(),
                file: file,
                index: (index + lastIndex)
            }));

            files.forEach((f) => {
                const h = { ...(headers || {}), ...{ 'BIT_FILE_ID': f.fileId } };
                const uploader = new BitFileUploader(id, dotnetReference, f.file, uploadEndpointUrl, h, f.index);
                FileUpload._fileUploaders.push(uploader);
            });

            inputElement.value = '';

            return files;
        }

        public static upload(id: string, from: number, to: number, index: number, uploadUrl: string, headers: Record<string, string> = {}): void {
            const uploaders = FileUpload._fileUploaders.filter(u => u.id === id);

            if (index === -1) {
                uploaders.forEach(u => u.upload(from, to, uploadUrl, headers));
            } else {
                const uploader = uploaders.filter(u => u.index === index)[0];
                uploader.upload(from, to, uploadUrl, headers);
            }
        }

        public static pause(id: string, index: number): void {
            const uploaders = FileUpload._fileUploaders.filter(u => u.id === id);

            if (index === -1) {
                uploaders.forEach(u => u.pause());
            } else {
                const uploader = uploaders.filter(u => u.index === index)[0];
                uploader.pause();
            }
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
            FileUpload._fileUploaders = FileUpload._fileUploaders.filter(u => u.id !== id);
        }

        public static reset(id: string, inputElement: HTMLInputElement,) {
            FileUpload.clear(id);
            inputElement.value = '';
        }
    }

    class BitFileUploader {
        id: string;
        dotnetReference: DotNetObject;
        file: File;
        uploadUrl: string | undefined;
        headers: Record<string, string>;
        index: number;

        private xhr: XMLHttpRequest = new XMLHttpRequest();

        constructor(id: string, dotnetReference: DotNetObject, file: File, uploadEndpointUrl: string | undefined, headers: Record<string, string>, index: number) {
            this.id = id;
            this.dotnetReference = dotnetReference;
            this.file = file;
            this.uploadUrl = uploadEndpointUrl;
            this.headers = headers;
            this.index = index;

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

        upload(from: number, to: number, uploadUrl: string, headers: Record<string, string>): void {
            const file = this.file;
            if (file === null) return;

            const data: FormData = new FormData();
            const chunk = file.slice(from, to);
            data.append('file', chunk, file.name);

            var url = uploadUrl || this.uploadUrl;

            if (!url) return;

            this.xhr.open('POST', url, true);

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
    }
}