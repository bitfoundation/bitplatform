module Foundation.Test.ViewModels {

    @Core.SecureFormViewModelDependency({
        name: "App",
        templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/app.html",
        $routeConfig: [
            { path: "/rad-combo-page", name: "RadComboFormViewModel", useAsDefault: true },
            { path: "/angular-service-usage-page", name: "AngularServiceUsageFormViewModel" },
            { path: "/angular-translate-page", name: "AngularTranslateFormViewModel" },
            { path: "/async-page", name: "AsyncFormViewModel" },
            { path: "/date-time-service-page", name: "DateTimeServiceFormViewModel" },
            { path: "/entity-context-usage-page", name: "EntityContextUsageFormViewModel" },
            { path: "/form-validation-page", name: "FormValidationFormViewModel" },
            { path: "/nested-route-page/...", name: "NestedRouteMainFormViewModel" },
            { path: "/rad-grid-page", name: "RadGridFormViewModel" },
            { path: "/repeat-page", name: "RepeatFormViewModel" },
            { path: "/route-parameter-page/:to", name: "RouteParameterFormViewModel" },
            { path: "/simple-page", name: "SimpleFormViewModel" },
            { path: "/lookups-page", name: "LookupsFormViewModel" },
            { path: "/**", redirectTo: ["NestedRouteMainFormViewModel"] }
        ]
    })
    export class App extends ViewModel.ViewModels.FormViewModel {

        public constructor( @Core.Inject("MessageReceiver") public messageReceiver: Core.Contracts.IMessageReceiver) {
            super();
        }

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            await this.messageReceiver.start();
        }

    }

}