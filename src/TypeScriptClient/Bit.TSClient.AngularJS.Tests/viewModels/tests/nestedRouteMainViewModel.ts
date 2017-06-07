module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({
        name: "NestedRouteMainFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/nestedRouteMainview.html",
        $routeConfig: [
            { path: "/first-part-page/", name: "FirstPartFormViewModel", useAsDefault: true },
            { path: "/second-part-page/:parameter", name: "SecondPartFormViewModel" },
            { path: "/**", redirectTo: ["NestedRouteMainFormViewModel"] }
        ]
    })
    export class NestedRouteMainFormViewModel extends Bit.ViewModels.FormViewModel {
        public constructor( @Inject("$document") public $document: ng.IDocumentService) {
            super();
            this.$document.attr("title", "Nested View");
        }
    }

    @SecureFormViewModelDependency({ name: "FirstPartFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/firstPartview.html" })
    export class FirstPartFormViewModel extends Bit.ViewModels.FormViewModel {

        public goToNextPart(): void {
            this.$router.navigate(["SecondPartFormViewModel", { parameter: 1 }]);
        }
    }

    @SecureFormViewModelDependency({
        name: "SecondPartFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/secondPartview.html"
    })
    export class SecondPartFormViewModel extends Bit.ViewModels.FormViewModel {

        public parameter: number;

        public async $onInit(): Promise<void> {
            this.parameter = this.route.params["parameter"];
        }
    }
}