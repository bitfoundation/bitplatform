module Bit.Components.Identity {

    export interface LoginScope extends ng.IScope {
        loginModel: Models.Identity.ISsoModel;
    }

    @ComponentDependency({ name: "login", templateUrl: "|IdentityServer|/views/loginView.html" })
    export class LoginComponent {

        public loginModel: Models.Identity.ISsoModel;

        public constructor( @Inject("ModelProvider") public modelProvider: Contracts.Identity.IModelProvider) {
            this.loginModel = this.modelProvider.getModel();
        }

    }

}