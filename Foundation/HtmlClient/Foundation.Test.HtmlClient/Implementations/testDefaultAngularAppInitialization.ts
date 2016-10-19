/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    export class TestDefaultAngularAppInitialization extends Foundation.ViewModel.Implementations.DefaultAngularAppInitialization {

        constructor() {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngComponentRouter", "kendo.directives", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }

        protected async onAppRun(app: angular.IModule): Promise<void> {

            const dependencyManager = Core.DependencyManager.getCurrent();

            app.run(['$rootRouter', '$mdToast', '$translate', ($rootRouter: angular.Router, $mdToast: angular.material.IToastService, $translate: angular.translate.ITranslateService) => {
                dependencyManager.registerInstanceDependency({ instance: $rootRouter, name: "$rootRouter" });
                dependencyManager.registerInstanceDependency({ instance: $mdToast, name: "$mdToast" });
                dependencyManager.registerInstanceDependency({ instance: $translate, name: "$translate" });
            }]);

            await super.onAppRun(app);

        }

        protected async configureAppModule(app: angular.IModule): Promise<void> {
            app.config(['$locationProvider', ($locationProvider: angular.ILocationProvider) => {
                $locationProvider.html5Mode(true);
            }]);
            await super.configureAppModule(app);
        }
    }
}