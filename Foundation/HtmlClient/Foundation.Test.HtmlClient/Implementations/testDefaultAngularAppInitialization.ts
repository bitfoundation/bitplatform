/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    @Core.ObjectDependency({
        name: "AppEvent"
    })
    export class TestDefaultAngularAppInitialization extends ViewModel.Implementations.DefaultAngularAppInitialization {

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngComponentRouter", "kendo.directives", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }

        protected async configureAppModule(app: ng.IModule): Promise<void> {
            app.config(["$locationProvider", ($locationProvider: ng.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);
            await super.configureAppModule(app);
        }
    }
}