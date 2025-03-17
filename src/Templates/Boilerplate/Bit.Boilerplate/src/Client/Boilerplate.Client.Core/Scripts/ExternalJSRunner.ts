// Checkout external-js-runner.html
class ExternalJSRunner {
    public static async run() {
        const host = window.origin.replace('http://', '');
        const localWebSocket = new WebSocket(`ws://${host}/external-js-runner`);
        localWebSocket.onmessage = async (event) => {
            const request = JSON.parse(event.data);
            let result = null;
            try {
                if (request.type == 'getCredential') {
                    result = await WebAuthn.getCredential(request.options);
                } if (request.type == 'createCredential') {
                    result = await WebAuthn.createCredential(request.options);
                } else if (request.type == 'close') {
                    result = {};
                    setTimeout(window.close, 100);
                }
            }
            catch (e) {
                console.error(e);
            }
            finally {
                localWebSocket.send(JSON.stringify(result));
            }
        };
    }
}

(window as any).ExternalJSRunner = ExternalJSRunner;