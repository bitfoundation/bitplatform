var BitButil = BitButil || {};

(function (butil: any) {
    butil.console = {
        assert, clear, count, countReset, debug, dir, dirxml, error, group, groupCollapsed, groupEnd,
        info, log, memory, profile, profileEnd, table, time, timeEnd, timeLog, timeStamp, trace, warn
    };

    function assert(condition?: boolean, ...data: any[]) {
        console.assert(condition, ...data);
    }

    function clear() {
        console.clear();
    }

    function count(label?: string) {
        console.count(label);
    }

    function countReset(label?: string) {
        console.countReset(label);
    }

    function debug(...data: any[]) {
        console.debug(...data);
    }

    function dir(item?: any, options?: any) {
        console.dir(item, options);
    }

    function dirxml(...data: any[]) {
        console.dirxml(...data);
    }

    function error(...data: any[]) {
        console.error(...data);
    }

    function group(...data: any[]) {
        console.group(...data);
    }

    function groupCollapsed(...data: any[]) {
        console.groupCollapsed(...data);
    }

    function groupEnd() {
        console.groupEnd();
    }

    function info(...data: any[]) {
        console.info(...data);
    }

    function log(...data: any[]) {
        console.log(...data);
    }

    function memory() {
        (console as any).memory;
    }

    function profile() {
        (console as any).profile();
    }

    function profileEnd() {
        (console as any).profileEnd();
    }

    function table(data?: any, properties?: string[]) {
        console.table(data, properties);
    }

    function time(label?: string) {
        console.time(label);
    }

    function timeEnd(label?: string) {
        console.timeEnd(label);
    }

    function timeLog(label?: string, ...data: any[]) {
        console.timeLog(label, ...data);
    }

    function timeStamp(label?: string) {
        console.timeStamp(label);
    }

    function trace(...data: any[]) {
        console.trace(...data);
    }

    function warn(...data: any[]) {
        console.warn(...data);
    }
}(BitButil));