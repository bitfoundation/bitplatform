module Test.View.Directives {

    // <test test-property="3"></test>

    @Foundation.Core.Injectable()
    export class TestDirectiveController {

        public constructor(public $scope: ng.IScope, public $http: ng.IHttpService, @Foundation.Core.Inject("$document") public $document: ng.IDocumentService) {

        }

        public testProperty = 1;
    }

    TestDirectiveController.$inject = ["$scope", "$http"];

    @Foundation.Core.DirectiveDependency({ name: "test" })
    export class DefaultElementMoverDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                controller: TestDirectiveController,
                controllerAs: "ctrl",
                bindToController: {
                    testProperty: "@?"
                },
                template: "<div>{{::ctrl.testProperty}}</div>",
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, controller: TestDirectiveController) {
                    controller.testProperty = 2;
                }
            });
        }
    }
}