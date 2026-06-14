var BitButil = BitButil || {};

(function (butil: any) {
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
                const list = await c.select(properties || ['name'], { multiple: !!multiple });
                return (list || []).map((entry: any) => ({
                    name: entry.name ?? [],
                    email: entry.email ?? [],
                    tel: entry.tel ?? [],
                    // Addresses come back as ContactAddress objects - flatten to single-line strings.
                    address: (entry.address ?? []).map(stringifyAddress),
                    icon: (entry.icon ?? []).map((blob: any) => {
                        try { return URL.createObjectURL(blob); } catch { return ''; }
                    }).filter((u: string) => u.length > 0)
                }));
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
}(BitButil));
