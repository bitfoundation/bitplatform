
/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({
        name: "LayoutChild",
        scope: false,
        replace: false,
        terminal: false,
        restrict: "A"
    })
    export class DefaultLayoutChildDirective {

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public $onInit() {
            this.$element.attr("flex-gt-md", "25");
            this.$element.attr("flex-gt-sm", "33");
            this.$element.attr("flex-gt-xs", "50");
            this.$element.attr("flex-xs", "100");
        }
    }
}