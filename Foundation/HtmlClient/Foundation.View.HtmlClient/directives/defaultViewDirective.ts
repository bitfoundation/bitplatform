
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Foundation.Core.DirectiveDependency({ name: 'view' })
    export class DefaultViewDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                link: ($scope: ng.IScope) => {
                    if ($scope['vm'] != null)
                        ($scope['vm'] as Foundation.ViewModel.ViewModels.FormViewModel).bindingContext = $scope;
                }
            });
        }
    }
}