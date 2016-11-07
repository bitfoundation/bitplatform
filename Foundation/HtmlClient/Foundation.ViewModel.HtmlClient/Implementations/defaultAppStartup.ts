/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {

    @Core.Injectable()
    export class DefaultAppStartup implements Core.Contracts.IAppStartup {

        public constructor( @Core.InjectAll("AppEvent") public appEvents: Core.Contracts.IAppEvents[]) {

        }

        @Core.Log()
        public async configuration(): Promise<void> {

            for (let appEvent of this.appEvents) {
                await appEvent.onAppStartup();
            }

        }
    }
}