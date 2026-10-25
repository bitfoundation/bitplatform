// Lets the Circuit page drop Blazor Server's connection the way a network hiccup would, so the reconnect dialog, the
// circuit's disconnects and BevTools' reconnect diagnostics have something to show. Loaded before blazor.web.js: a
// socket is remembered on its first send, which every socket makes, Blazor's handshake included.
(() => {
    const sockets = new Set();
    const send = WebSocket.prototype.send;
    WebSocket.prototype.send = function (data) {
        sockets.add(this);
        return send.call(this, data);
    };

    window.bitDemoConnection = {
        // A moment later, so the interop call that asked gets its answer over the socket first.
        drop() {
            setTimeout(() => {
                for (const socket of sockets) {
                    if (socket.readyState === WebSocket.OPEN && /\/_blazor/.test(socket.url)) socket.close(4000, "Dropped by the demo");
                }
                for (const socket of [...sockets]) {
                    if (socket.readyState === WebSocket.CLOSED || socket.readyState === WebSocket.CLOSING) sockets.delete(socket);
                }
            }, 100);
        },
    };
})();
