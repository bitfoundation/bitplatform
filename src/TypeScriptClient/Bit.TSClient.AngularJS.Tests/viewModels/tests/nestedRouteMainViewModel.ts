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

        public constructor( @Inject("$state") public $state) {

        }

        public goToNextPart(): void {
            this.$state.go("nestedRouteMainViewModel.secondPartViewModel", { parameter: 1 });
        }

    }

    @ComponentDependency({
        name: "SecondPartViewModel",
        templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/secondPartview.html",
        bindings: {
            $transition$: '<'
        }
    })
    export class SecondPartViewModel {

        public parameter: number;

        public $transition$: any;

        public async $onInit(): Promise<void> {
            this.parameter = this.$transition$.params().parameter;
        }
    }
}