
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "layoutChild" })
    export class DefaultLayoutChildDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: false,
                terminal: false,
                restrict: "A",
                link: ($scope: ng.IScope, $element: ng.IAugmentedJQuery) => {
                    $element.attr("flex-gt-md", "25");
                    $element.attr("flex-gt-sm", "33");
                    $element.attr("flex-gt-xs", "50");
                    $element.attr("flex-xs", "100");
                }
            });
        }
    }
}