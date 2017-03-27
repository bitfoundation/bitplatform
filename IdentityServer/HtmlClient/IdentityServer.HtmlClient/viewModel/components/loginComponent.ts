module IdentityServer.ViewModel.ViewModels {

    export interface LoginScope extends ng.IScope {
        loginModel: Core.Models.ISsoModel;
    }

    @Foundation.Core.ComponentDependency({ name: "login", templateUrl: "|IdentityServer|/view/views/loginView.html" })
    export class LoginComponent {

        public loginModel: Core.Models.ISsoModel;

        public constructor( @Foundation.Core.Inject("ModelProvider") public modelProvider: Core.Contracts.IModelProvider) {
            this.loginModel = this.modelProvider.getModel();
        }

    }

}