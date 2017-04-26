module Foundation.Test.ViewModels {
    @Core.SecureFormViewModelDependency({ name: "RouteParameterFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/routeParameterview.html" })
    export class RouteParameterFormViewModel extends ViewModel.ViewModels.FormViewModel {

        public constructor( @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        public async $onInit(): Promise<void> {
            const to: string = this.route.params["to"];
            this.$document.attr("title", to);
        }
    }
}