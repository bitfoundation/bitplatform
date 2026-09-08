var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    async function index() {
        const reg: any = await window.navigator.serviceWorker?.getRegistration();
        return reg?.index ?? null;
    }

    function describe(entry: any) {
        return {
            id: entry.id ?? '',
            title: entry.title ?? '',
            description: entry.description ?? '',
            // The property was named launchUrl before the spec settled on url; read both so an
            // entry registered by an older browser still comes back with somewhere to go.
            url: entry.url ?? entry.launchUrl ?? '',
            category: entry.category ?? '',
            icons: (entry.icons ?? []).map((icon: any) => ({
                src: icon.src ?? '',
                sizes: icon.sizes ?? '',
                type: icon.type ?? '',
                label: icon.label ?? ''
            }))
        };
    }

    butil.contentIndex = {
        async isSupported() {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            return !!(reg && reg.index);
        },
        async add(entry: any) {
            const contentIndex = await index();
            if (!contentIndex?.add) return false;
            try {
                await contentIndex.add({
                    id: entry.id,
                    title: entry.title,
                    description: entry.description,
                    category: entry.category || undefined,
                    url: entry.url,
                    launchUrl: entry.url,
                    icons: (entry.icons ?? []).map((icon: any) => ({
                        src: icon.src,
                        sizes: icon.sizes || undefined,
                        type: icon.type || undefined,
                        label: icon.label || undefined
                    }))
                });
                return true;
            } catch {
                // The url has to be inside the worker's scope and the icons have to be fetchable;
                // both fail here as a TypeError carrying nothing a caller could act on.
                return false;
            }
        },
        async delete(id: string) {
            const contentIndex = await index();
            if (!contentIndex?.delete) return false;
            try { await contentIndex.delete(id); return true; } catch { return false; }
        },
        async getAll() {
            const contentIndex = await index();
            if (!contentIndex?.getAll) return [];
            try { return (await contentIndex.getAll()).map(describe); } catch { return []; }
        }
    };
}(BitButil));
