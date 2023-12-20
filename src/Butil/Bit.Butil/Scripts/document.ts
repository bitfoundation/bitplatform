var BitButil = BitButil || {};

(function (butil: any) {
    butil.document = {
        characterSet() { return document.characterSet },
        compatMode() { return document.compatMode },
        contentType() { return document.contentType },
        documentURI() { return document.documentURI },
        getDesignMode() { return document.designMode },
        setDesignMode(value: string) { document.designMode = value },
        getDir() { return document.dir },
        setDir(value: string) { document.dir = value },
        referrer() { return document.referrer },
        getTitle() { return document.title },
        URL() { return document.URL },
        setTitle(value: string) { document.title = value },
        exitFullscreen() { return document.exitFullscreen() },
        exitPointerLock() { return document.exitPointerLock() }
    };
}(BitButil));