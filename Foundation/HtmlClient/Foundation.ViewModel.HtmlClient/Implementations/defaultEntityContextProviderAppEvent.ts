module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultEntityContextProviderAppEvent implements Foundation.Core.Contracts.IAppEvents {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Implementations.DefaultEntityContextProvider) {

        }

        public async onAppStartup(): Promise<void> {
            if (this.entityContextProvider instanceof ViewModel.Implementations.DefaultEntityContextProvider)
                this.entityContextProvider.oDataJSInit();
        }
    }
}
