var BitButil = BitButil || {};

(function (butil: any) {
    butil.location = {
        getHref() { return window.location.href },
        setHref(value: string) { window.location.href = value },
        getProtocol() { return window.location.protocol },
        setProtocol(value: string) { window.location.protocol = value },
        getHost() { return window.location.host },
        setHost(value: string) { window.location.host = value },
        getHostname() { return window.location.hostname },
        setHostname(value: string) { window.location.hostname = value },
        getPort() { return window.location.port },
        setPort(value: string) { window.location.port = value },
        getPathname() { return window.location.pathname },
        setPathname(value: string) { window.location.pathname = value },
        getSearch() { return window.location.search },
        setSearch(value: string) { window.location.search = value },
        getHash() { return window.location.hash },
        setHash(value: string) { window.location.hash = value },
        origin() { return window.location.origin },
        assign(url: string) { window.location.assign(url) },
        reload() { window.location.reload() },
        replace(url: string) { window.location.replace(url) },
    };
}(BitButil));