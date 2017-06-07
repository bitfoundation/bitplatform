module Bit.Implementations {

    export class EntityContextProviderAppEventBase implements Contracts.IAppEvents {

        public constructor(public entityContextProvider: Implementations.EntityContextProviderBase) {

        }

        public async onAppStartup(): Promise<void> {
            await this.entityContextProvider.oDataJSInit();
        }
    }
}
