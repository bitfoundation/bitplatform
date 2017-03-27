module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultEntityContextProviderAppEvent implements Core.Contracts.IAppEvents {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: Implementations.DefaultEntityContextProvider) {

        }

        public async onAppStartup(): Promise<void> {
            if (this.entityContextProvider instanceof Implementations.DefaultEntityContextProvider)
                this.entityContextProvider.oDataJSInit();
        }
    }
}
