interface IFiles { name: string; size: number, type: string }

class BitFileUploader {
    static bitFileUploads: BitFileUpload[];
    static headers: any;

    static init(inputElement: HTMLInputElement,
        dotnetReference: any,
        uploadEndpointUrl: string,
        headers: any[]): IFiles[] {
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
    dotnetReference: any;
    uploadEndpointUrl: string;
    inputElement: HTMLInputElement;
    index: number;
    headers: any;
    xhr: XMLHttpRequest = new XMLHttpRequest();

    constructor(dotnetReference: any, uploadEndpointUrl: string, inputElement: HTMLInputElement, index: number, headers: any) {
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
        this.xhr.onreadystatechange = function (event: Event): any {
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