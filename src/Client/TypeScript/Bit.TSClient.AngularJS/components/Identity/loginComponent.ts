module Bit.Components.Identity {

    @ComponentDependency({
        name: "login", template: `

<style>
    .login-header {
        color: #585858;
        font-size: 1.5em;
        font-weight: bold;
        margin: 60px;
        text-align: center;
    }

    .login-content {
        color: #585858;
        width: 350px;
        margin: 40px auto 10px;
        border-radius: 5px;
    }

    .login-form-title {
        font-size: 1.5em;
        font-weight: bold;
        padding: 3px;
        text-align: center;
        margin-top: 0;
    }

    .login-error-message {
        width: 100%;
        background-color: pink;
        color: red;
        padding: 3px;
        margin-bottom: 2em;
        border-radius: 2px;
        font-size: -.5em;
    }
</style>

<div class="login-header">
    <a>{{:: "AppTitle" | translate}}</a>
</div>

<div class="login-content padded" md-whiteframe="2">

    <form name="loginForm" method="post" action="{{::vm.loginModel.loginUrl}}" novalidate autocomplete="off" style="padding:2em;">

        <anti-forgery-token token="::vm.loginModel.antiForgery"></anti-forgery-token>

        <h1 class="login-form-title">
            {{:: "LoginTitle" | translate}}
        </h1>

        <br />

        <div name="error" class="login-error-message" ng-if="::vm.loginModel.errorMessage != null && vm.loginModel.errorMessage != ''">
            {{:: vm.loginModel.errorMessage | translate}}
        </div>

        <md-input-container class="md-block" md-no-float="false">
            <label>{{::"UserName" | translate }}</label>
            <input style="direction:ltr" ng-required="true" name="username" type="text" ng-model="::vm.loginModel.userName" autofocus />
            <div ng-messages="loginForm.userName.$error"
                 ng-show="loginForm.userName.$invalid ">
                <div ng-message="required">{{:: "UserNameIsRequired" | translate}}</div>
            </div>
        </md-input-container>

        <md-input-container class="md-block">
            <label>{{:: "Password" | translate }}</label>
            <input style="direction:ltr" ng-required="true" name="password" type="password" ng-model="::vm.loginModel.password" />
            <div ng-messages="loginForm.password.$error"
                 ng-show="loginForm.password.$invalid">
                <div ng-message="required">{{:: "PasswordIsRequired" | translate }}</div>
            </div>
        </md-input-container>

        <br />

        <div flex layout="row" layout-align="start" ng-if="::vm.loginModel.allowRememberMe">
            <md-checkbox ng-model="::vm.loginModel.rememberMe">
                {{:: "RememberMe" | translate }}
            </md-checkbox>
        </div>

        <md-button name="login" class="md-raised md-primary login-button" type="submit" ng-disabled="loginForm.$invalid">{{:: "Login" | translate }}</md-button>

        <section ng-if="::vm.loginModel.externalProviders != null && vm.loginModel.externalProviders.length != 0">
          <div class="label">{{:: "OrLoginUsing" | translate }}</div>
          <a ng-repeat="provider in ::vm.loginModel.externalProviders" class='md-button md-primary' style='padding-top:9px;' ng-href="{{::provider.href}}">{{:: provider.text | translate }}</a>
        </section>

    </form>
</div>


` })
    export class LoginComponent {

        public loginModel: Models.Identity.LoginModel;

        public constructor(@Inject("LoginModelProvider") public loginModelProvider: Contracts.Identity.ILoginModelProvider) {
            this.loginModel = this.loginModelProvider.getModel();
        }

    }

}
