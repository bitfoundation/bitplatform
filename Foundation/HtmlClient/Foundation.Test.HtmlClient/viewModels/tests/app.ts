module Foundation.Test.ViewModels {

    @Core.FormViewModelDependency({
        name: "App",
        templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/app.html",
        $routeConfig: [
            { path: "/rad-combo-page", name: 'RadComboFormViewModel', useAsDefault: true },
            { path: "/angular-service-usage-page", name: 'AngularServiceUsageFormViewModel' },
            { path: "/angular-translate-page", name: 'AngularTranslateFormViewModel' },
            { path: "/async-page", name: 'AsyncFormViewModel' },
            { path: "/date-time-service-page", name: 'DateTimeServiceFormViewModel' },
            { path: "/entity-context-usage-page", name: 'EntityContextUsageFormViewModel' },
            { path: "/form-validation-page", name: 'FormValidationFormViewModel' },
            { path: "/nested-route-page/...", name: 'NestedRouteMainFormViewModel' },
            { path: "/rad-grid-page", name: 'RadGridFormViewModel' },
            { path: "/repeat-page", name: 'RepeatFormViewModel' },
            { path: "/route-parameter-page/:to", name: 'RouteParameterFormViewModel' },
            { path: "/simple-page", name: 'SimpleFormViewModel' },
            { path: '/**', redirectTo: ['NestedRouteMainFormViewModel'] }
        ]
    })
    export class App extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("MessageReciever") public messageReciever: Core.Contracts.IMessageReciever) {
            super();
        }

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            await this.messageReciever.start();
        }

    }

}