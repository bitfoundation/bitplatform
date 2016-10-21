/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />
/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.core.htmlclient/typings.d.ts" />

module IdentityServer.ViewModel.Implementations {
    export class IdentityServerDefaultAngularAppInitialization extends Foundation.ViewModel.Implementations.DefaultAngularAppInitialization {

        constructor() {
            super();
        }

        protected getBaseModuleDependencies(): Array<string> {
            return ["pascalprecht.translate", "ngMessages", "ngMaterial", "ngAria", "ngAnimate"];
        }

        protected async registerComponents(app: angular.IModule): Promise<void> {
            await super.registerComponents(app);
            app.controller("LoginViewModel", IdentityServer.ViewModel.ViewModels.LoginViewModel);
        }

        protected async onAppRun(app: angular.IModule): Promise<void> {

            const dependencyManager = Foundation.Core.DependencyManager.getCurrent();

            app.run(['$mdToast', '$translate', ($mdToast: angular.material.IToastService, $translate: angular.translate.ITranslateService) => {
                dependencyManager.registerInstanceDependency({ instance: $mdToast, name: "$mdToast" });
                dependencyManager.registerInstanceDependency({ instance: $translate, name: "$translate" });
            }]);

            await super.onAppRun(app);
        }
    }
}