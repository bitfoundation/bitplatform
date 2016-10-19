/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    export class DefaultAppStartup implements Core.Contracts.IAppStartup {
        @Core.Log()
        public async configuration(): Promise<void> {

            const appEvents = Core.DependencyManager.getCurrent().resolveAllObjects<Core.Contracts.IAppEvents>("AppEvent");

            for (let i = 0; i < appEvents.length; i++) {
                await appEvents[i].onAppStartup();
            }
        }
    }
}