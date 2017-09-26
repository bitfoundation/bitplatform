module Bit.Tests.ViewModels {
    @ComponentDependency({
        name: "RouteParameterViewModel",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/routeParameterview.html",
        bindings: {
            $transition$: '<'
        }
    })
    export class RouteParameterViewModel {

        public constructor( @Inject("$document") public $document: ng.IDocumentService) {

        }

        public $transition$: any;

        public async $onInit(): Promise<void> {
            const to: string = this.$transition$.params().to;
            this.$document.attr("title", to);
        }
    }
}