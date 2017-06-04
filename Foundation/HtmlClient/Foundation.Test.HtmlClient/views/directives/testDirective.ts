module Test.View.Directives {

    // <test test-property="3"></test>

    @Foundation.Core.Injectable()
    export class TestDirectiveController {

        public constructor(@Foundation.Core.Inject("$scope") public $scope: ng.IScope, @Foundation.Core.Inject("$http") public $http: ng.IHttpService, @Foundation.Core.Inject("$document") public $document: ng.IDocumentService) {

        }

        public testProperty = 1;
    }


    @Foundation.Core.DirectiveDependency({ name: "test" })
    export class DefaultElementMoverDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                controller: TestDirectiveController,
                controllerAs: "ctrl",
                bindToController: {
                    testProperty: "@?"
                },
                template: "<div>{{::ctrl.testProperty}}</div>",
                link($scope: ng.IScope, $element: JQuery, $attrs: ng.IAttributes, controller: TestDirectiveController) {
                    controller.testProperty = 2;
                }
            });
        }
    }
}