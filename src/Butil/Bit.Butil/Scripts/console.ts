var BitButil = BitButil || {};

(function (butil: any) {
    butil.console = {
        assert(...data: any[]) { console.assert(...data) },
        clear() { console.clear() },
        count(label?: string) { console.count(label) },
        countReset(label?: string) { console.countReset(label) },
        debug(...data: any[]) { console.debug(...data) },
        dir(item?: any, options?: any) { console.dir(item, options) },
        dirxml(...data: any[]) { console.dirxml(...data) },
        error(...data: any[]) { console.error(...data) },
        group(...data: any[]) { console.group(...data) },
        groupCollapsed(...data: any[]) { console.groupCollapsed(...data) },
        groupEnd() { console.groupEnd() },
        info(...data: any[]) { console.info(...data) },
        log(...data: any[]) { console.log(...data) },
        profile(name: string) { (console as any).profile(name) },
        profileEnd(name: string) { (console as any).profileEnd(name) },
        table(data?: any, properties?: string[]) { console.table(data, properties) },
        time(label?: string) { console.time(label) },
        timeEnd(label?: string) { console.timeEnd(label) },
        timeLog(...data: any[]) { console.timeLog(...data) },
        timeStamp(label?: string) { console.timeStamp(label) },
        trace(...data: any[]) { console.trace(...data) },
        warn(...data: any[]) { console.warn(...data) }
    };
}(BitButil));