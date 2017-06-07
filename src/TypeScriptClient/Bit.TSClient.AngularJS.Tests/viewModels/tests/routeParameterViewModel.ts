module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "RouteParameterFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/routeParameterview.html" })
    export class RouteParameterFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        public async $onInit(): Promise<void> {
            const to: string = this.route.params["to"];
            this.$document.attr("title", to);
        }
    }
}