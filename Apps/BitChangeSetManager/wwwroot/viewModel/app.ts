module BitChangeSetManager.ViewModels {

    @SecureFormViewModelDependency({
        name: "App",
        template: `<main ng-model-options="{ updateOn : 'default blur' , allowInvalid : true , debounce: { 'default': 250, 'blur': 0 } }">
                        <ng-outlet></ng-outlet>
                   </main>`,
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