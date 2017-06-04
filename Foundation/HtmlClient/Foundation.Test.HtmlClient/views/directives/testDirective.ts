module Test.View.Directives {

    // <test test-property="vm.someNumber"></test>

    @Foundation.Core.DirectiveDependency({
        name: "test",
        controllerAs: "ctrl",
        bindToController: {
            testProperty: "="
        },
        template: "<div>{{::ctrl.testProperty}}</div>"
    })
    export class TestDirectiveController {

        public constructor( @Foundation.Core.Inject("$scope") public $scope: ng.IScope, @Foundation.Core.Inject("$http") public $http: ng.IHttpService, @Foundation.Core.Inject("$document") public $document: ng.IDocumentService) {

        }

        public $onInit() {
            this.testProperty = 2;
        }

        public testProperty = 1;
    }
}