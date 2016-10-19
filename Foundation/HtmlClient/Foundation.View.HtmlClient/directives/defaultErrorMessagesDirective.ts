
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Foundation.Core.DirectiveDependency({ name: 'errorMessages' })
    export class DefaultErrorMessagesDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                transclude: true,
                template: (element: JQuery, attrs: angular.IAttributes) => {
                    return `<div multiple ng-messages="${attrs['field']}.$error" ng-if="${attrs['field']}.validityEvaludated && ${attrs['field']}.$invalid" error-messages-transclude></div>`;
                }
            });
        }
    }

    @Foundation.Core.DirectiveDependency({ name: 'errorMessagesTransclude' })
    export class ErrorMessagesTransclude implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
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