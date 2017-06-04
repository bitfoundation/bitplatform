
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "errorMessages", usesOldStyle: true })
    export class DefaultErrorMessagesDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                transclude: true,
                template: ($element: JQuery, $attrs: ng.IAttributes) => {
                    return `<div multiple ng-messages="${$attrs["field"]}.$error" ng-if="${$attrs["field"]}.validityEvaludated && ${$attrs["field"]}.$invalid" error-messages-transclude></div>`;
                }
            });
        }
    }

    @Core.DirectiveDependency({
        name: "ErrorMessagesTransclude"
    })
    export class ErrorMessagesTransclude {

        public constructor( @Core.Inject("$transclude") public $transclude: ng.ITranscludeFunction, @Core.Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            this.$transclude((clone, transcludeScope) => {
                angular.element(this.$element).append(clone);
            });
        }
    }
}