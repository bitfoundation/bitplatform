/// <reference path="imports.ts" />

module BitChangeSetManager {

    @ObjectDependency({
        name: "AppEvent"
    })
    export class BitChangeSetManagerAngularAppInitialization extends FoundationVM.Implementations.DefaultAngularAppInitialization {

        public constructor( @Inject("ClientAppProfileManager") public clientAppProfileManager: ClientAppProfileManager) {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            let modules = ["pascalprecht.translate", "ngComponentRouter", "ngMessages", "ngMaterial", "ngAria", "ngAnimate", "kendo.directives"];
            return modules;
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {
            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);
            await super.configureAppModule(app);
        }

        protected async onAppRun(app: ng.IModule): Promise<void> {
            FoundationView.Directives.DefaultRadGridDirective.defaultRadGridDirectiveCustomizers.push(($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, gridOptions: kendo.ui.GridOptions): void => {
                gridOptions.groupable = true;
            });
            await super.onAppRun(app);
        }
    }
}