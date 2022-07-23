interface IFiles { name: string; size: number, type: string }

class BitFileUpload {
    static bitFileUploaders: BitFileUploader[];
    static headers: Record<string, string>;

    static setupDropzone(dropZoneElement: HTMLElement, inputFile: HTMLInputElement) {

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

    static init(inputElement: HTMLInputElement,
        dotnetReference: DotNetObject,
        uploadEndpointUrl: string,
        headers: Record<string, string>): IFiles[] {

        let filesArray: IFiles[] = Array.from<IFiles>(inputElement.files!).map(file => ({
            name: file.name,
            size: file.size,
            type: file.type
        }));

        this.bitFileUploaders = [];
        this.headers = headers;

        filesArray.forEach((value, index) => {
            let uploader: BitFileUploader = new BitFileUploader(dotnetReference, uploadEndpointUrl, inputElement, index, this.headers);
            this.bitFileUploaders.push(uploader);
        });

        return filesArray;
    }

    static upload(from: number, to: number, index: number): void {
        if (index === -1) {
            this.bitFileUploaders.forEach(bitFileUpload => {
                bitFileUpload.upload(from, to);
            });
        } else {
            const uploader = this.bitFileUploaders.filter(f => f.index === index)[0];
            uploader.upload(from, to);
        }
    }

    static pause(index: number): void {
        if (index === -1) {
            this.bitFileUploaders.forEach(uploader => {
                uploader.pause();
            });
        } else {
            const uploader = this.bitFileUploaders.filter(u => u.index === index)[0];
            uploader.pause();
        }
    }
}

class BitFileUploader {
    dotnetReference: DotNetObject;
    uploadEndpointUrl: string;
    inputElement: HTMLInputElement;
    index: number;
    headers: Record<string, string>;
    xhr: XMLHttpRequest = new XMLHttpRequest();

    constructor(dotnetReference: DotNetObject, uploadEndpointUrl: string, inputElement: HTMLInputElement, index: number, headers: Record<string, string>) {
        this.dotnetReference = dotnetReference;
        this.uploadEndpointUrl = uploadEndpointUrl;
        this.inputElement = inputElement;
        this.index = index;
        this.headers = headers;

        if (index < 0) return;

        this.xhr.upload.onprogress = function (e: ProgressEvent) {
            if (e.lengthComputable) {
                dotnetReference.invokeMethodAsync("HandleUploadProgress", index, e.loaded);
            }
        };

        const me = this;
        this.xhr.onreadystatechange = function (event) {
            if (me.xhr.readyState === 4) {
                dotnetReference.invokeMethodAsync("HandleFileUpload", index, me.xhr.status, me.xhr.responseText);
            }
        };
    }

    upload(from: number, to: number): void {
        const files = this.inputElement.files;
        if (files === null) return;

        const file = files[this.index];
        const data: FormData = new FormData();
        const chunk = file.slice(from, to);
        data.append('file', chunk, file.name);

        this.xhr.open('POST', this.uploadEndpointUrl, true);

        Object.keys(this.headers).forEach(h => {
            this.xhr.setRequestHeader(h, this.headers[h]);
        });

        this.xhr.send(data);
    }

    pause(): void {
        this.xhr.abort();
    }
}