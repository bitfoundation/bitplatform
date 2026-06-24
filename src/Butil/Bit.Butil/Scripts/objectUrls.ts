var BitButil = BitButil || {};

(function (butil: any) {
    butil.objectUrls = {
        create(data: Uint8Array, mimeType: string) {
            const buf = butil.utils.arrayToBuffer(data);
            const blob = new Blob([buf], { type: mimeType || 'application/octet-stream' });
            return URL.createObjectURL(blob);
        },
        revoke(url: string) {
            try { URL.revokeObjectURL(url); } catch { /* already revoked */ }
        }
    };
}(BitButil));
