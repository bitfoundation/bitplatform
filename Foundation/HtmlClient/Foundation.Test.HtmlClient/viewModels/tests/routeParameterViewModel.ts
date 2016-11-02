module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "RouteParameterFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/routeParameterview.html" })
    export class RouteParameterFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("$document") public $document: angular.IDocumentService) {
            super();
        }

        public async $onInit(): Promise<void> {
            const to: string = this.route.params['to'];
            this.$document.attr("title", to);
        }
    }
}