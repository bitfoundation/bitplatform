module Bit.Implementations {

    export class AppStartupBase implements Contracts.IAppStartup {

        public constructor(public appEvents: Contracts.IAppEvents[]) {

        }

        @Log()
        public async configuration(): Promise<void> {

            for (let appEvent of this.appEvents) {
                await appEvent.onAppStartup();
            }

        }
    }
}