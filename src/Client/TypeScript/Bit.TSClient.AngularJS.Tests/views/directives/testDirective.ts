module Bit.Tests.Directives {

    // <test test-property="vm.someNumber"></test>

    @DirectiveDependency({
        name: "Test",
        controllerAs: "ctrl",
        bindToController: {
            testProperty: "="
        },
        template: "<div>{{::ctrl.testProperty}}</div>"
    })
    export class TestDirectiveController {

        public constructor( @Inject("$scope") public $scope: ng.IScope, @Inject("$http") public $http: ng.IHttpService, @Inject("$document") public $document: ng.IDocumentService) {

        }

        public $onInit() {
            this.testProperty = 2;
        }

        public testProperty = 1;
    }
}