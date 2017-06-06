/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadGridDeleteButton",
        bindToController: {
        },
        controllerAs: "radGridDeleteButton",
        require: {
            radGrid: "^^radGrid"
        },
        restrict: "E",
        scope: true,
        template: `<button ng-click=";radGridDeleteButton.radGrid.deleteDataItem($event)" ng-transclude></button>`,
        replace: true,
        terminal: true,
        transclude: true
    })
    export class DefaultRadGridDeleteButtonDirective {

        public static defaultClasses: string[] = ["md-button", "md-raised"];

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            DefaultRadGridDeleteButtonDirective.defaultClasses.forEach(cssClass => {
                this.$element.addClass(cssClass);
            });
        }

    }
}