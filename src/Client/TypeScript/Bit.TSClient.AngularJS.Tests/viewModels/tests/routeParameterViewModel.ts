module Bit.Tests.ViewModels {
    @ComponentDependency({
        name: "RouteParameterViewModel",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/routeParameterview.html"
    })
    export class RouteParameterViewModel {

        public constructor( @Inject("$document") public $document: ng.IDocumentService, @Inject("$state") public $state: ng.ui.IStateService) {

        }

        public async $onInit(): Promise<void> {
            const to: string = this.$state.params.to;
            this.$document.attr("title", to);
        }
    }
}