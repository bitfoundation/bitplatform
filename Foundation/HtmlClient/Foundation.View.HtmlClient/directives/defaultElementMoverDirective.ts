/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    export interface IDefaultElementMoverDirectiveScope extends ng.IScope {
        predicate: boolean;
        elementSelector: string;
    }

    @Core.DirectiveDependency({ name: "elementMover", usesOldStyle: true })
    export class DefaultElementMoverDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: {
                    predicate: "=",
                    elementSelector: "@"
                },
                link($scope: IDefaultElementMoverDirectiveScope, $element: JQuery, $attrs: ng.IAttributes) {

                    $scope.$watch("predicate", ((isOkToBeMoved: boolean) => {

                        if (isOkToBeMoved == true) {

                            angular.element($scope.elementSelector)
                                .appendTo($element);

                        }

                    }));

                }
            });
        }
    }
}