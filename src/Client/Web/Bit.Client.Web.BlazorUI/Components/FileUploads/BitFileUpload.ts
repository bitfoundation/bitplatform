interface IFiles { name: string; size: number, type: string }

class BitFileUploader {
    static bitFileUploaded: BitFileUpload[];

    static init(inputElement: HTMLInputElement, dotnetReference: any, uploadEndpointUrl: string): IFiles[] {
        let filesArray: IFiles[] = Array.from<IFiles>(inputElement.files!).map(file => ({
            name: file.name,
            size: file.size,
            type: file.type
        }));
        this.bitFileUploaded = [];

        filesArray.forEach((value, index) => {
            let uploader: BitFileUpload = new BitFileUpload(dotnetReference, uploadEndpointUrl, inputElement, index);
            this.bitFileUploaded.push(uploader);
        });
        return filesArray;
    }

    static upload(index: number, to: number, from: number): void {
        if (index === -1) {
            this.bitFileUploaded.forEach((value) => {
                value.upload(to, from);
            })
        }
        else {
            var uploader = this.bitFileUploaded.filter(c => c.index === index)[0];
            uploader.upload(to, from);
        }
    }

    static pause(index: number): void {
        if (index === -1) {
            this.bitFileUploaded.forEach((value) => {
                value.pause();
            })
        }
        else {
            var uploader = this.bitFileUploaded.filter(c => c.index === index)[0];
            uploader.pause();
        }
    }
}

class BitFileUpload {
    dotnetReference: any;
    uploadEndpointUrl: string;
    inputElement: HTMLInputElement;
    index: number;
    request: XMLHttpRequest = new XMLHttpRequest();

    constructor(dotnetReference: any, uploadEndpointUrl: string, inputElement: HTMLInputElement, index: number) {
        this.dotnetReference = dotnetReference;
        this.uploadEndpointUrl = uploadEndpointUrl;
        this.inputElement = inputElement;
        this.index = index;

        if (index < 0) {
            return;
        }

        this.request.upload.onprogress = function (e: ProgressEvent) {
            dotnetReference.invokeMethodAsync("BitHandleUploadProgress", index, e.loaded);
        };

        this.request.onreadystatechange = (function (request: XMLHttpRequest, event: Event): any {
            if (request.readyState === 4) {
                dotnetReference.invokeMethodAsync("BitHandleFileUploaded", index, request.status);
            }
        }).bind(this, this.request);
    }

    upload(to: number, from: number): void {
        const files = this.inputElement.files;
        if (files === null) {
            return;
        }
        const file = files[this.index];
        let data: FormData = new FormData()
        let chunk = file.slice(to, from);
        data.append('file', chunk, file.name);
        this.request.open('POST', this.uploadEndpointUrl, true);
        this.request.send(data);
    }

    pause(): void {
        this.request.abort();
    }
}