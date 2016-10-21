module IdentityServer.ViewModel.ViewModels {

    export interface LoginScope extends angular.IScope {
        loginModel: Core.Models.ISsoModel;
    }

    @Foundation.Core.Injectable()
    export class LoginViewModel extends Foundation.ViewModel.ViewModels.FormViewModel {

        public loginModel: IdentityServer.Core.Models.ISsoModel;

        public constructor( @Foundation.Core.Inject("ModelProvider") public modelProvider: Core.Contracts.IModelProvider) {
            super();
            this.loginModel = this.modelProvider.getModel();
        }

    }

}