/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    @Core.ObjectDependency({
        name: 'AppEvent'
    })
    export class TestDefaultAngularAppInitialization extends Foundation.ViewModel.Implementations.DefaultAngularAppInitialization {

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngComponentRouter", "kendo.directives", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }

        protected async configureAppModule(app: angular.IModule): Promise<void> {
            app.config(['$locationProvider', ($locationProvider: angular.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);
            await super.configureAppModule(app);
        }
    }
}