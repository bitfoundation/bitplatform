var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Every AbortSignal Butil hands to .NET, under the id .NET knows it by, with whatever the
    // abortController module tracks alongside it (its controller, its listeners, the sources of a
    // hand-wired composite).
    //
    // Its own module rather than a corner of abortController, for the same reason abortable is one:
    // fetch and scheduler need nothing from that module except the ability to resolve one of these
    // ids, and a lazy-loaded module file inlines its dependencies - so reaching into abortController
    // for a one-line lookup would put the whole AbortController surface in their downloads.
    const _entries: { [id: string]: any } = {};

    butil.abortSignals = {
        put(id: string, entry: any) { _entries[id] = entry; },
        entryOf(id: string) { return _entries[id]; },
        remove(id: string) { delete _entries[id]; },
        ids() { return Object.keys(_entries); },
        signalOf(id: string) { return _entries[id]?.signal; }
    };
}(BitButil));
