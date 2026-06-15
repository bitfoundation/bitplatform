var BitButil = BitButil || {};

(function (butil: any) {
    const DEFAULT_PROPERTIES = ['name', 'email', 'tel'];

    butil.contactPicker = {
        isSupported() { return !!(window.navigator as any).contacts; },
        async getProperties() {
            const c: any = (window.navigator as any).contacts;
            if (!c?.getProperties) return [];
            try { return await c.getProperties(); }
            catch { return []; }
        },
        async select(properties: string[], multiple: boolean) {
            const c: any = (window.navigator as any).contacts;
            if (!c?.select) return [];
            try {
                const props = (properties && properties.length) ? properties : DEFAULT_PROPERTIES;
                const list = await c.select(props, { multiple: !!multiple });
                return await Promise.all((list || []).map(async (entry: any) => ({
                    name: entry.name ?? [],
                    email: entry.email ?? [],
                    tel: entry.tel ?? [],
                    // Addresses come back as ContactAddress objects - flatten to single-line strings.
                    address: (entry.address ?? []).map(stringifyAddress),
                    // Icons are Blobs. Inline them as data URLs so there's no object URL to leak;
                    // a data URL is self-contained and needs no revocation.
                    icon: (await Promise.all((entry.icon ?? []).map(blobToDataUrl)))
                        .filter((u: string) => u.length > 0)
                })));
            } catch {
                // Permission denied or no user gesture.
                return [];
            }
        }
    };

    function stringifyAddress(a: any) {
        if (!a) return '';
        const parts = [a.organization, a.recipient,
            ...(a.addressLine ?? []), a.dependentLocality, a.city, a.region,
            a.postalCode, a.country];
        return parts.filter((p: any) => !!p).join(', ');
    }

    function blobToDataUrl(blob: any): Promise<string> {
        return new Promise<string>(resolve => {
            try {
                const reader = new FileReader();
                reader.onloadend = () => resolve(typeof reader.result === 'string' ? reader.result : '');
                reader.onerror = () => resolve('');
                reader.readAsDataURL(blob);
            } catch {
                resolve('');
            }
        });
    }
}(BitButil));
