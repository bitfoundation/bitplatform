namespace BitBlazorUI {
    export class MediaQuery {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static async setup(id: string, query: string, dotnetObj: DotNetObject) {
            if (!query || !dotnetObj) return;

            MediaQuery.dispose(id);

            const ac = new AbortController();
            MediaQuery._abortControllers[id] = ac;

            const queryList = window.matchMedia(query);

            queryList.addEventListener('change', async e => {
                await handleMatchChange(e.matches);
            }, { signal: ac.signal });

            await handleMatchChange(queryList.matches);

            async function handleMatchChange(matches: boolean) {
                await dotnetObj.invokeMethodAsync("OnMatchChange", matches);
            }
        }

        public static dispose(id: string) {
            const ac = MediaQuery._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete MediaQuery._abortControllers[id];
        }
    }
}