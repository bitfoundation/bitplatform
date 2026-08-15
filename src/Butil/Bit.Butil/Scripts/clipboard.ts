var BitButil = BitButil || {};

(function (butil: any) {
    butil.clipboard = {
        // navigator.clipboard is only defined in a secure context, so this answers the
        // "am I on http://" question as well as the "did this engine ship it" one. The
        // item-based half is a separate check: Firefox has readText/writeText without read/write.
        isSupported() { return !!navigator.clipboard; },
        isItemSupported() {
            const c = navigator.clipboard as any;
            return !!c && typeof c.read === 'function' && typeof c.write === 'function'
                && typeof (window as any).ClipboardItem === 'function';
        },
        readText,
        writeText,
        read,
        write
    };

    async function readText() {
        return await window.navigator.clipboard.readText();
    }

    async function writeText(text: string) {
        return await window.navigator.clipboard.writeText(text);
    }

    async function read(formats) {
        const clipboardItems = await (navigator.clipboard as any).read(formats);
        const result = [];
        for (const item of clipboardItems) {
            for (const mimeType of item.types) {
                const blob = await item.getType(mimeType);
                const buffer = await blob.arrayBuffer();
                result.push({ mimeType: mimeType, data: new Uint8Array(buffer) });
            }
        }
        return result;
    }

    async function write(items) {
        const clipboardItems = [];
        for (const item of items) {
            const type = item.mimeType;
            const blob = new Blob([butil.utils.arrayToBuffer(item.data)], { type });
            clipboardItems.push(new ClipboardItem({ [type]: blob }));
        }
        await navigator.clipboard.write(clipboardItems);
    }
}(BitButil));