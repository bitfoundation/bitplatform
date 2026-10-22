namespace BitBlazorUI {
    export class AiArtifactView {
        // Offers the text of an artifact to the user as a file. A blob behind an object URL rather than a data URL, so
        // an artifact of any length downloads whole, and the URL is revoked once the click has been dispatched - a
        // moment later, since revoking it at once cancels the download in some engines.
        public static downloadText(fileName: string, text: string, mimeType?: string) {
            const blob = new Blob([text], { type: mimeType || 'text/plain' });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName || 'artifact.txt';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    }
}
