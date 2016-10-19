/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    export interface IDefaultElementMoverDirectiveScope extends ng.IScope {
        predicate: boolean;
        elementSelector: string;
    }

    @Foundation.Core.DirectiveDependency({ name: 'elementMover' })
    export class DefaultElementMoverDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: {
                    predicate: '=',
                    elementSelector: '@'
                },
                link($scope: IDefaultElementMoverDirectiveScope, element: JQuery, attributes: any) {

                    $scope.$watch('predicate', ((isOkToBeMoved: boolean) => {

                        if (isOkToBeMoved == true) {

                            angular.element($scope.elementSelector)
                                .appendTo(element);

                        }

                    }));

                }
            });
        }
    }
}