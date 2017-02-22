/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    @Core.ObjectDependency({
        name: "AppEvent"
    })
    export class TestDefaultAngularAppInitialization extends ViewModel.Implementations.DefaultAngularAppInitialization {

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            let modules = ["pascalprecht.translate", "ngComponentRouter", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
            if (this.clientAppProfileManager.getClientAppProfile().screenSize == "DesktopAndTablet")
                modules.push("kendo.directives");
            return modules;
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {
            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
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