module BitChangeSetManager.ViewModels {

    @SecureFormViewModelDependency({
        name: "App",
        templateUrl: `view/app.html`,
        $routeConfig: [
            { path: "/change-sets-page", name: "ChangeSetsViewModel", useAsDefault: true }
        ]
    })
    export class App extends FormViewModel {


        public constructor( @Inject("MessageReceiver") public messageReceiver: IMessageReceiver) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {
            await this.messageReceiver.start();
        }

    }

}