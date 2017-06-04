/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    export interface IDefaultElementMoverDirectiveScope extends ng.IScope {
        predicate: boolean;
        elementSelector: string;
    }

    @Core.DirectiveDependency({
        name: "ElementMover",
        scope: {
            predicate: "=",
            elementSelector: "@"
        }
    })
    export class DefaultElementMoverDirective {

        public constructor( @Core.Inject("$element") public $element: JQuery, @Core.Inject("$scope") public $scope: IDefaultElementMoverDirectiveScope) {

        }

        public $onInit() {

            this.$scope.$watch("predicate", ((isOkToBeMoved: boolean) => {

                if (isOkToBeMoved == true) {

                    angular.element(this.$scope.elementSelector)
                        .appendTo(this.$element);

                }

            }));

        }
    }
}