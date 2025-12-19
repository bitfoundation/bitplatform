var BitBesql = window.BitBesql || {};
BitBesql.version = window['bit-besql version'] = '10.2.1';

BitBesql.persist = async function besqlPersist(fileName) {

    if (BitBesql.dbCache == null) {
        BitBesql.dbCache = await caches.open('bit-Besql');
    }

    const dbCache = BitBesql.dbCache;

    const sqliteFilePath = `/${fileName}`;
    const cacheStorageFilePath = `/data/cache/${fileName}`;

    if (!window.Blazor.runtime.Module.FS.analyzePath(sqliteFilePath).exists)
        throw new Error(`Database ${fileName} not found.`);

    const data = window.Blazor.runtime.Module.FS.readFile(sqliteFilePath);

    const blob = new Blob([data], {
        type: 'application/octet-stream',
        status: 200
    });

    const headers = new Headers({
        'content-length': blob.size
    });

    const response = new Response(blob, {
        headers
    });

    await dbCache.put(cacheStorageFilePath, response);
}

BitBesql.load = async function besqlLoad(fileName) {
    const sqliteFilePath = `/${fileName}`;
    const cacheStorageFilePath = `/data/cache/${fileName}`;

    BitBesql.dbCache = await caches.open('bit-Besql');

    const dbCache = BitBesql.dbCache;

    const resp = await dbCache.match(cacheStorageFilePath);
    if (resp && resp.ok) {
        const res = await resp.arrayBuffer();
        if (res) {
            window.Blazor.runtime.Module.FS.writeFile(sqliteFilePath, new Uint8Array(res));
        }
    }
}
