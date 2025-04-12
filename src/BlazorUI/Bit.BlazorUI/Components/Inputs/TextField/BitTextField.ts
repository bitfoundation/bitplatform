namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static setupAutoHeight(id: string, inputElement: HTMLInputElement) {
            const ac = new AbortController();
            TextField._abortControllers[id] = ac;

            inputElement.addEventListener('input', function () {
                this.style.height = 'auto';
                this.style.height = this.scrollHeight + 'px';
            }, { signal: ac.signal });
        }

        public static dispose(id: string) {
            const ac = TextField._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete TextField._abortControllers[id];
        }
    }
}