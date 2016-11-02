module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({
        name: "NestedRouteMainFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/nestedRouteMainview.html",
        $routeConfig: [
            { path: '/first-part-page/', name: 'FirstPartFormViewModel', useAsDefault: true },
            { path: '/second-part-page/:parameter', name: 'SecondPartFormViewModel' },
            { path: '/**', redirectTo: ['NestedRouteMainFormViewModel'] }
        ]
    })
    export class NestedRouteMainFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {
        constructor( @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
            this.$document.attr('title', 'Nested View');
        }
    }

    @Core.FormViewModelDependency({ name: "FirstPartFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/firstPartview.html" })
    export class FirstPartFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public $router;

        public goToNextPart(): void {
            this.$router.navigate(['SecondPartFormViewModel', { parameter: 1 }]);
        }
    }

    @Core.FormViewModelDependency({ name: "SecondPartFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/secondPartview.html" })
    export class SecondPartFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public parameter: number;

        public async $routerOnActivate(route: any): Promise<void> {
            this.parameter = route.params.parameter;
            return await super.$routerOnActivate(route);
        }
    }
}