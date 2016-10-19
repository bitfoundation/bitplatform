
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Foundation.Core.DirectiveDependency({ name: 'layoutChild' })
    export class DefaultLayoutChildDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: false,
                terminal: false,
                restrict: 'A',
                link: ($scope: ng.IScope, element: ng.IAugmentedJQuery) => {
                    element.attr('flex-gt-md', '25');
                    element.attr('flex-gt-sm', '33');
                    element.attr('flex-gt-xs', '50');
                    element.attr('flex-xs', '100');
                }
            });
        }
    }
}