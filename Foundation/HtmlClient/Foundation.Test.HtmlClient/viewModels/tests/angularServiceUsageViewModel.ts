module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "AngularServiceUsageFormViewModel", routeTemplate: "/angular-service-usage-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/angularServiceUsageview.html" })
    export class AngularServiceUsageFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        public async $routerOnActivate(route): Promise<void> {
            this.$document.attr("title", "done");
            return await super.$routerOnActivate(route);
        }
    }
}