/// <reference path="FormViewModel.ts" />
module Foundation.ViewModel.ViewModels {
    export class SecureFormViewModel extends FormViewModel {

        public async $routerCanActivate(route: any): Promise<boolean> {
            const dependencyManager = Core.DependencyManager.getCurrent();
            const securityService = dependencyManager.resolveObject<Core.Contracts.ISecurityService>("securityService");
            if (!securityService.isLoggedIn()) {
                securityService.login(await this.getPreLoginState());
                return false;
            }
            return await super.$routerCanActivate(route);
        }

        protected async getPreLoginState(): Promise<any> {
            return null;
        }

        protected async getPostLoginState(): Promise<any> {
            return JSON.parse(localStorage["state"]);
        }

    }
}