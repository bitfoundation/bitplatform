module Bit.Implementations {

    @Injectable()
    export class DefaultAppStartup extends AppStartupBase {

        public constructor(@InjectAll("AppEvent") public appEvents: Contracts.IAppEvents[]) {
            super(appEvents);
        }

    }
}