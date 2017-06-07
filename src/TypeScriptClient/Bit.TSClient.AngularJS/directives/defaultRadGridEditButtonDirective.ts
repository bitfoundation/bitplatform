

module Bit.Directives {

    @DirectiveDependency({
        name: "RadGridEditButton",
        bindToController: {
        },
        controllerAs: "radGridEditButton",
        require: {
            radGrid: "^^radGrid"
        },
        restrict: "E",
        scope: true,
        template: ($element: JQuery, $attrs: ng.IAttributes & { ngClick: string }) => {
            delete $attrs.ngClick; // Prevent $compiler from recompiling developer provided custom ng-click (if any)
            return `<button ng-click=";radGridEditButton.radGrid.updateDataItem($event)" ng-transclude></button>`;
        },
        replace: true,
        terminal: true,
        transclude: true
    })
    export class DefaultRadGridEditButtonDirective {

        public static defaultClasses: string[] = ["md-button", "md-raised"];

        public constructor( @Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            DefaultRadGridEditButtonDirective.defaultClasses.forEach(cssClass => {
                this.$element.addClass(cssClass);
            });
        }

    }
}