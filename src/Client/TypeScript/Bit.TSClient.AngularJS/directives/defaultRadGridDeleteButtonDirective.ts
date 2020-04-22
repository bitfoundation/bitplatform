

module Bit.Directives {

    @DirectiveDependency({
        name: "RadGridDeleteButton",
        bindToController: {
        },
        controllerAs: "radGridDeleteButton",
        require: {
            radGrid: "^^radGrid"
        },
        restrict: "E",
        scope: true,
        template: ($element: JQuery, $attrs: ng.IAttributes & { ngClick: string }) => {
            delete $attrs.ngClick; // prevent $compiler from recompiling developer provided custom ng-click (if any)
            return `<button ng-click=";radGridDeleteButton.radGrid.deleteDataItem($event)" ng-transclude></button>`;
        },
        replace: true,
        terminal: true,
        transclude: true
    })
    export class DefaultRadGridDeleteButtonDirective {

        public static defaultClasses: string[] = ["md-button", "md-raised"];

        public constructor(@Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            DefaultRadGridDeleteButtonDirective.defaultClasses.forEach(cssClass => {
                this.$element.addClass(cssClass);
            });
        }

    }
}