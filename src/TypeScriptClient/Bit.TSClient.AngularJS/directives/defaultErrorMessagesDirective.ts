


module Bit.Directives {

    @DirectiveDependency({
        name: "ErrorMessages",
        scope: true,
        bindToController: {
        },
        controllerAs: "errorMessages",
        restrict: "E",
        template: ($element: JQuery, $attrs: ng.IAttributes & { field: string }) => {
            return `<div multiple ng-messages="${$attrs.field}.$error" ng-if="${$attrs.field}.validityEvaludated && ${$attrs.field}.$invalid" error-messages-transclude></div>`;
        },
        transclude: true
    })
    export class DefaultErrorMessagesDirective {
    }

    @DirectiveDependency({
        name: "ErrorMessagesTransclude",
        scope: {},
        bindToController: {
        },
        controllerAs: "errorMessagesTransclude",
        restrict: "A"
    })
    export class ErrorMessagesTransclude {

        public constructor( @Inject("$transclude") public $transclude: ng.ITranscludeFunction, @Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            this.$transclude((clone, transcludeScope) => {
                angular.element(this.$element).append(clone);
            });
        }
    }
}