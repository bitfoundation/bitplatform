module Bit.Implementations {

    export class AppStartupBase implements Contracts.IAppStartup {

        public constructor(public appEvents: Contracts.IAppEvents[]) {

        }

        @Log()
        public async configuration(): Promise<void> {
            try {
                if (typeof (performance) != "undefined") {
                    performance.mark("bit-app-configuration-start");
                }
                for (let appEvent of this.appEvents) {
                    await appEvent.onAppStartup();
                }
            }
            finally {
                if (typeof (performance) != "undefined") {
                    performance.mark("bit-app-configuration-finish");
                    performance.measure("bit-app-configuration", "bit-app-configuration-start", "bit-app-configuration-finish");
                }
            }

        }
    }
}