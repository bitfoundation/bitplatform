module BitChangeSetManager.ViewModel.ViewModels {

    @ComponentDependency({
        name: "app",
        templateUrl: `view/views/app.html`
    })
    export class App {


        public constructor( @Inject("MessageReceiver") public messageReceiver: IMessageReceiver,
            @Inject("$mdSidenav") public $mdSidenav: ng.material.ISidenavService,
            @Inject("SecurityService") public securityService: ISecurityService,
            @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider) {
        }

        public user: BitChangeSetManager.Dto.UserDto;

        @Command()
        public async $onInit(): Promise<void> {

            let context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");
            this.user = await context.users.getCurrentUser().getValue();

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