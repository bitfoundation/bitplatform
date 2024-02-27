class BitFileUpload {
    private static fileUploaders: BitFileUploader[] = [];

    public static reset(
        id: string,
        dotnetReference: DotNetObject,
        inputElement: HTMLInputElement,
        uploadEndpointUrl: string,
        headers: Record<string, string>) {

        this.fileUploaders = this.fileUploaders.filter(u => u.id !== id);

        const files = Array.from(inputElement.files!).map((file, index) => ({
            name: file.name,
            size: file.size,
            type: file.type,
            fileId: Bit.uuidv4(),
            index
        }));

        files.forEach((f, index) => {
            const h = { ...headers, ...{ 'BIT_FILE_ID': f.fileId } };
            const uploader = new BitFileUploader(id, dotnetReference, inputElement, uploadEndpointUrl, h, index);
            this.fileUploaders.push(uploader);
        });

        return files;
    }

    public static upload(id: string, from: number, to: number, index: number, uploadUrl: string, headers: Record<string, string> = {}): void {
        const uploaders = this.fileUploaders.filter(u => u.id === id);

        if (index === -1) {
            uploaders.forEach(u => u.upload(from, to, uploadUrl, headers));
        } else {
            const uploader = uploaders.filter(u => u.index === index)[0];
            uploader.upload(from, to, uploadUrl, headers);
        }
    }

    public static pause(id: string, index: number): void {
        const uploaders = this.fileUploaders.filter(u => u.id === id);

        if (index === -1) {
            uploaders.forEach(u => u.pause());
        } else {
            const uploader = uploaders.filter(u => u.index === index)[0];
            uploader.pause();
        }
    }

    public static setupDropzone(dropZoneElement: HTMLElement, inputFile: HTMLInputElement) {

        function onDragHover(e: DragEvent) {
            e.preventDefault();
        }

        function onDragLeave(e: DragEvent) {
            e.preventDefault();
        }

        function onDrop(e: DragEvent) {
            e.preventDefault();
            inputFile.files = e.dataTransfer!.files;
            const event = new Event('change', { bubbles: true });
            inputFile.dispatchEvent(event);
        }

        function onPaste(e: ClipboardEvent) {
            inputFile.files = e.clipboardData!.files;
            const event = new Event('change', { bubbles: true });
            inputFile.dispatchEvent(event);
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

    public static browse(inputFile: HTMLInputElement) {
        inputFile.click();
    }

    public static dispose(id: string) {
        this.fileUploaders = this.fileUploaders.filter(u => u.id !== id);
    }
}

class BitFileUploader {
    id: string;
    dotnetReference: DotNetObject;
    inputElement: HTMLInputElement;
    uploadUrl: string;
    headers: Record<string, string>;
    index: number;

    private xhr: XMLHttpRequest = new XMLHttpRequest();

    constructor(id: string, dotnetReference: DotNetObject, inputElement: HTMLInputElement, uploadEndpointUrl: string, headers: Record<string, string>, index: number) {
        this.id = id;
        this.dotnetReference = dotnetReference;
        this.inputElement = inputElement;
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
        const files = this.inputElement.files;
        if (files === null) return;

        const file = files[this.index];
        const data: FormData = new FormData();
        const chunk = file.slice(from, to);
        data.append('file', chunk, file.name);

        this.xhr.open('POST', uploadUrl ? uploadUrl : this.uploadUrl, true);

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