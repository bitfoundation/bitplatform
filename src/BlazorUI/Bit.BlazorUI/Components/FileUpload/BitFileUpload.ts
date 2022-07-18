interface IFiles { name: string; size: number, type: string }

class BitFileUploader {
    static bitFileUploads: BitFileUpload[];
    static headers: Record<string, string>;

    static initDropZone(dropZoneElement: HTMLElement, inputFile: HTMLInputElement) {

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

        this.bitFileUploads = [];
        this.headers = headers;

        filesArray.forEach((value, index) => {
            let uploader: BitFileUpload = new BitFileUpload(dotnetReference, uploadEndpointUrl, inputElement, index, this.headers);
            this.bitFileUploads.push(uploader);
        });

        return filesArray;
    }

    static upload(index: number, to: number, from: number): void {
        if (index === -1) {
            this.bitFileUploads.forEach(bitFileUpload => {
                bitFileUpload.upload(to, from);
            });
        } else {
            const uploader = this.bitFileUploads.filter(f => f.index === index)[0];
            uploader.upload(to, from);
        }
    }

    static pause(index: number): void {
        if (index === -1) {
            this.bitFileUploads.forEach(bitFileUpload => {
                bitFileUpload.pause();
            });
        } else {
            const uploader = this.bitFileUploads.filter(c => c.index === index)[0];
            uploader.pause();
        }
    }
}

class BitFileUpload {
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

    upload(to: number, from: number): void {
        const files = this.inputElement.files;
        if (files === null) return;

        const file = files[this.index];
        const data: FormData = new FormData();
        const chunk = file.slice(to, from);
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