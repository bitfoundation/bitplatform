/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadGridEditButton",
        bindToController: {
        },
        controllerAs: "radGridEditButton",
        require: {
            radGrid: "^^radGrid"
        },
        restrict: "E",
        scope: true,
        template: `<button ng-click="radGridEditButton.radGrid.updateDataItem($event)" ng-transclude></button>`,
        replace: true,
        terminal: true,
        transclude: true
    })
    export class DefaultRadGridEditButtonDirective {

        public static defaultClasses: string[] = ["md-button", "md-raised"];

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public $postLink() {
            DefaultRadGridEditButtonDirective.defaultClasses.forEach(cssClass => {
                this.$element.addClass(cssClass);
            });
        }

    }
}