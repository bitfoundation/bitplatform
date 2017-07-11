module BitChangeSetManager.ViewModel.ViewModels {

    @SecureFormViewModelDependency({
        name: "App",
        templateUrl: `view/views/app.html`,
        $routeConfig: [
            { path: "/change-sets-page", name: "ChangeSetsViewModel", useAsDefault: true }
        ]
    })
    export class App extends FormViewModel {


        public constructor( @Inject("MessageReceiver") public messageReceiver: IMessageReceiver,
            @Inject("$mdSidenav") public $mdSidenav: ng.material.ISidenavService,
            @Inject("SecurityService") public securityService: ISecurityService) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {
            await this.messageReceiver.start();
        }

        @Command()
        public async openMenu(): Promise<void> {
            await this.$mdSidenav("menu").open();
        }

        @Command()
        public async closeMenu(): Promise<void> {
            await this.$mdSidenav("menu").close();
        }

        @Command()
        public async logout(): Promise<void> {
            this.securityService.logout();
        }
    }
}