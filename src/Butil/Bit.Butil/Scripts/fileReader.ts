var BitButil = BitButil || {};

(function (butil: any) {
    butil.fileReader = {
        getFileInfo,
        getFileInfos,
        readAsBytes,
        readAsText,
        readAsDataUrl,
        clear
    };

    function info(f: File | null | undefined) {
        if (!f) return null;
        return {
            name: f.name,
            type: f.type ?? '',
            size: f.size,
            lastModified: f.lastModified ?? 0
        };
    }

    function file(input: HTMLInputElement, index: number): File | null {
        return input?.files?.[index] ?? null;
    }

    function getFileInfo(input: HTMLInputElement, index: number) {
        return info(file(input, index));
    }

    function getFileInfos(input: HTMLInputElement) {
        const list = input?.files;
        if (!list) return [];
        const out = [];
        for (let i = 0; i < list.length; i++) out.push(info(list[i]));
        return out;
    }

    async function readAsBytes(input: HTMLInputElement, index: number) {
        const f = file(input, index);
        if (!f) return null;
        const buf = await f.arrayBuffer();
        return new Uint8Array(buf);
    }

    async function readAsText(input: HTMLInputElement, index: number, encoding: string) {
        const f = file(input, index);
        if (!f) return '';
        // Blob.text() forces UTF-8; fall back to a FileReader when a different encoding is requested.
        if (!encoding || encoding.toLowerCase() === 'utf-8') return f.text();
        return await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve((reader.result as string) ?? '');
            reader.onerror = () => reject(reader.error);
            reader.readAsText(f, encoding);
        });
    }

    async function readAsDataUrl(input: HTMLInputElement, index: number) {
        const f = file(input, index);
        if (!f) return '';
        return await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve((reader.result as string) ?? '');
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(f);
        });
    }

    function clear(input: HTMLInputElement) {
        if (input) input.value = '';
    }
}(BitButil));
