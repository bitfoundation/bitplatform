module Bit.Tests.ViewModels {

    @SecureFormViewModelDependency({
        name: "App",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/app.html",
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
    export class App extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("MessageReceiver") public messageReceiver: Contracts.IMessageReceiver) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {
            await this.messageReceiver.start();
        }

    }

}