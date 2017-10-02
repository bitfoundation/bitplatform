module Bit.Tests.ViewModels {
    @ComponentDependency({
        name: "NestedRouteMainViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/nestedRouteMainview.html"
    })
    export class NestedRouteMainViewModel {
        public constructor( @Inject("$document") public $document: ng.IDocumentService) {
            this.$document.attr("title", "Nested View");
        }
    }

    @ComponentDependency({ name: "FirstPartViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/firstPartview.html" })
    export class FirstPartViewModel {

        public constructor( @Inject("$state") public $state: ng.ui.IStateService) {

        }

        public goToNextPart(): void {
            this.$state.go("nestedRouteMainViewModel.secondPartViewModel", { parameter: 1 });
        }

    }

    @ComponentDependency({
        name: "secondPartViewModel",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/secondPartview.html",
        cache: true
    })
    export class SecondPartViewModel {

        public constructor( @Inject("$state") public $state: ng.ui.IStateService) {

        }

        public parameter: number;

        @Command()
        public async onActivated(initialActivation: boolean) {
            this.parameter = this.$state.params.parameter;
        }
    }
}