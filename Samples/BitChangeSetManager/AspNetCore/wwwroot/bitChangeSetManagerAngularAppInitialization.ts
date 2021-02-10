/// <reference path="imports.ts" />

module BitChangeSetManager {

    @ObjectDependency({
        name: "AppEvent"
    })
    export class BitChangeSetManagerAngularAppInitialization extends Bit.Implementations.DefaultAngularAppInitialization {

        public constructor( @Inject("ClientAppProfileManager") public clientAppProfileManager: ClientAppProfileManager) {
            super();
        }

        protected getModuleDependencies(): Array<string> {
            let modules = ["pascalprecht.translate", "ui.router", "ngMessages", "ngMaterial", "ngAria", "ngAnimate", "kendo.directives"];
            return modules;
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {

            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);

            app.config(["$stateProvider", "$urlRouterProvider", "$urlServiceProvider", ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $urlServiceProvider) => {

                $urlServiceProvider.rules.otherwise({ state: "changeSetsViewModel" });

                $stateProvider.state("changeSetsViewModel", {
                    url: "/change-sets-page",
                    component: "changeSetsViewModel"
                });

            }]);

            await super.configureAppModule(app);
        }

        protected async onAppRun(app: ng.IModule): Promise<void> {
            Bit.Directives.DefaultRadGridDirective.defaultRadGridDirectiveCustomizers.push(($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, gridOptions: kendo.ui.GridOptions): void => {
                gridOptions.groupable = true;
            });
            await super.onAppRun(app);
        }

        protected async registerFilters(app: ng.IModule): Promise<void> {
            await super.registerFilters(app);

            app.filter("html", ["$sce", ($sce: ng.ISCEService) => {

                return (html: string): string => {
                    return $sce.trustAsHtml(html);
                }

            }]);
        }
    }
}