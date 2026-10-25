// The one thing a page of this demo needs from JavaScript that the site kit (site.js) does not
// provide: handing the bytes of an exported database to the browser as a file to save
// (Pages/Admin.razor). The library's own modules - the worker host, the engine, the VFSes - are
// imported on demand and need no tag.
window.bsqliteDemo = {
    download(filename, bytes) {
        const blob = new Blob([bytes], { type: "application/vnd.sqlite3" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = filename;
        link.click();
        URL.revokeObjectURL(url);
    },
};
