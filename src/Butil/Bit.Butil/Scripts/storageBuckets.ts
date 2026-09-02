var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Buckets are addressed by name on every call rather than kept in a handle registry: open() is
    // idempotent - it returns the existing bucket when one already exists under that name - so a
    // registry would only add a way for .NET and the browser to disagree about what is still there.

    function manager() { return (window.navigator as any).storageBuckets; }

    function open(name: string, options: any) {
        const buckets = manager();
        if (!buckets?.open) return Promise.reject(new Error('Storage Buckets are not supported'));
        return buckets.open(name, options ?? undefined);
    }

    async function describe(bucket: any) {
        // Every field is its own promise on the bucket, and each is optional across engines - one
        // that is missing must not lose the rest, hence the individual guards.
        const persisted = bucket.persisted ? await bucket.persisted() : false;
        const durability = bucket.durability ? await bucket.durability() : null;
        const expires = bucket.expires ? await bucket.expires() : null;
        const estimate = bucket.estimate ? await bucket.estimate() : null;

        return {
            name: bucket.name ?? '',
            persisted: !!persisted,
            durability: durability ?? null,
            expires: typeof expires === 'number' ? expires : null,
            quota: typeof estimate?.quota === 'number' ? estimate.quota : null,
            usage: typeof estimate?.usage === 'number' ? estimate.usage : null
        };
    }

    butil.storageBuckets = {
        isSupported() { return !!(window.navigator as any).storageBuckets; },
        async open(name: string, persisted: boolean, durability: string | null, quota: number | null, expires: number | null) {
            const options: any = {};
            if (persisted) options.persisted = true;
            if (durability) options.durability = durability;
            if (quota !== null && quota !== undefined) options.quota = quota;
            if (expires !== null && expires !== undefined) options.expires = expires;

            try { return await describe(await open(name, options)); }
            catch { return null; }
        },
        async keys() {
            const buckets = manager();
            if (!buckets?.keys) return [];
            try { return await buckets.keys(); } catch { return []; }
        },
        async delete(name: string) {
            const buckets = manager();
            if (!buckets?.delete) return false;
            try { await buckets.delete(name); return true; } catch { return false; }
        },
        async get(name: string) {
            try { return await describe(await open(name, undefined)); } catch { return null; }
        },
        async persist(name: string) {
            try {
                const bucket = await open(name, undefined);
                return bucket.persist ? await bucket.persist() : false;
            } catch {
                return false;
            }
        },
        async persisted(name: string) {
            try {
                const bucket = await open(name, undefined);
                return bucket.persisted ? await bucket.persisted() : false;
            } catch {
                return false;
            }
        },
        async estimate(name: string) {
            try {
                const bucket = await open(name, undefined);
                const estimate = bucket.estimate ? await bucket.estimate() : null;
                return {
                    quota: typeof estimate?.quota === 'number' ? estimate.quota : null,
                    usage: typeof estimate?.usage === 'number' ? estimate.usage : null,
                    // A bucket reports one number for the whole bucket; the per-API breakdown of
                    // navigator.storage.estimate() has no equivalent here.
                    usageDetails: []
                };
            } catch {
                return { quota: null, usage: null, usageDetails: [] };
            }
        },
        async setExpires(name: string, expires: number) {
            try {
                const bucket = await open(name, undefined);
                if (!bucket.setExpires) return false;
                await bucket.setExpires(expires);
                return true;
            } catch {
                return false;
            }
        },
        async getExpires(name: string) {
            try {
                const bucket = await open(name, undefined);
                const expires = bucket.expires ? await bucket.expires() : null;
                return typeof expires === 'number' ? expires : null;
            } catch {
                return null;
            }
        },

        // A bucket owns a whole OPFS tree of its own, reached the same way as the origin's - so the
        // file operations are butil.originPrivateFileSystem's, run against the bucket's root.
        async list(name: string, path: string) {
            try { return await butil.originPrivateFileSystem.listUnder(await bucketRoot(name), path); }
            catch { return []; }
        },
        async readText(name: string, path: string) {
            try { return await butil.originPrivateFileSystem.readTextUnder(await bucketRoot(name), path); }
            catch { return null; }
        },
        async write(name: string, path: string, text: string | null, bytes: Uint8Array | null) {
            try { return await butil.originPrivateFileSystem.writeUnder(await bucketRoot(name), path, text, bytes); }
            catch { return false; }
        },
        async remove(name: string, path: string, recursive: boolean) {
            try { return await butil.originPrivateFileSystem.removeUnder(await bucketRoot(name), path, recursive); }
            catch { return false; }
        }
    };

    async function bucketRoot(name: string) {
        const bucket = await open(name, undefined);
        if (!bucket.getDirectory) throw new Error('this bucket exposes no file system');
        return await bucket.getDirectory();
    }
}(BitButil));
