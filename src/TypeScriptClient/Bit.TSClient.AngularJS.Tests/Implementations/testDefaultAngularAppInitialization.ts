module Bit.Tests.Implementations {

    @ObjectDependency({
        name: "AppEvent"
    })
    export class TestDefaultAngularAppInitialization extends Bit.Implementations.DefaultAngularAppInitialization {

        public constructor( @Inject("ClientAppProfileManager") public clientAppProfileManager: ClientAppProfileManager) {
            super();
        }

        protected getModuleDependencies(): Array<string> {
            const modules = ["pascalprecht.translate", "ui.router", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
            if (this.clientAppProfileManager.getClientAppProfile().screenSize == "DesktopAndTablet")
                modules.push("kendo.directives");
            return modules;
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {
            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);

            app.config(["$stateProvider", "$urlRouterProvider", "$urlServiceProvider", ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider: ng.ui.IUrlRouterProvider, $urlServiceProvider) => {

                $urlServiceProvider.rules.otherwise({ state: "nestedRouteMainViewModel" });

                $stateProvider.state("radComboViewModel", {
                    url: "/rad-combo-page",
                    component: "radComboViewModel"
                }).state("angularServiceUsageViewModel", {
                    url: "/angular-service-usage-page",
                    component: "angularServiceUsageViewModel"
                }).state("angularTranslateViewModel", {
                    url: "/angular-translate-page",
                    component: "angularTranslateViewModel"
                }).state("asyncViewModel", {
                    url: "/async-page",
                    component: "asyncViewModel"
                }).state("dateTimeServiceViewModel", {
                    url: "/date-time-service-page",
                    component: "dateTimeServiceViewModel"
                }).state("entityContextUsageViewModel", {
                    url: "/entity-context-usage-page",
                    component: "entityContextUsageViewModel"
                }).state("formValidationViewModel", {
                    url: "/form-validation-page",
                    component: "formValidationViewModel"
                }).state("nestedRouteMainViewModel", {
                    url: "/nested-route-page",
                    redirectTo: 'nestedRouteMainViewModel.firstPartViewModel',
                    component: "nestedRouteMainViewModel"
                    }).state("nestedRouteMainViewModel.firstPartViewModel", {
                    url: "/first-part-page",
                    component: "firstPartViewModel"
                }).state("nestedRouteMainViewModel.secondPartViewModel", {
                    url: "/:parameter",
                    component: "secondPartViewModel"
                }).state("radGridViewModel", {
                    url: "/rad-grid-page",
                    component: "radGridViewModel"
                }).state("repeatViewModel", {
                    url: "/repeat-page",
                    component: "repeatViewModel"
                }).state("routeParameterViewModel", {
                    url: "/route-parameter-page/:to",
                    component: "routeParameterViewModel"
                }).state("simpleViewModel", {
                    url: "/simple-page",
                    component: "simpleViewModel"
                }).state("lookupsViewModel", {
                    url: "/lookups-page",
                    component: "lookupsViewModel"
                }).state("lookupsSearchViewModel", {
                    url: "/lookups-search-page",
                    component: "lookupsSearchViewModel"
                });

            }]);

            await super.configureAppModule(app);
        }

        protected async onAppRun(app: ng.IModule): Promise<void> {
            if (this.clientAppProfileManager.getClientAppProfile().culture == "FaIr")
                angular.element(document.body).addClass("k-rtl");
            await super.onAppRun(app);
        }
    }
}