
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "errorMessages" })
    export class DefaultErrorMessagesDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                transclude: true,
                template: (element: JQuery, attrs: ng.IAttributes) => {
                    return `<div multiple ng-messages="${attrs["field"]}.$error" ng-if="${attrs["field"]}.validityEvaludated && ${attrs["field"]}.$invalid" error-messages-transclude></div>`;
                }
            });
        }
    }

    @Core.DirectiveDependency({ name: "errorMessagesTransclude" })
    export class ErrorMessagesTransclude implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                link: ($scope: ng.IScope, element: JQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController, transclude: ng.ITranscludeFunction) => {
                    transclude((clone, transcludeScope) => {
                        angular.element(element).append(clone);
                    });
                }
            });
        }
    }
}