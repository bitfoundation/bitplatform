/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadGridCancelButton",
        bindToController: {
        },
        controllerAs: "radGridCancelButton",
        restrict: "E",
        scope: true,
        template: `<button ng-click="radGridCancelButton.radGrid.cancelDataItemChange($event)" ng-transclude></button>`,
        replace: true,
        terminal: true,
        transclude: true
    })
    export class DefaultRadGridCancelButtonDirective {

        public static defaultClasses: string[] = ["md-button", "md-raised"];
        public radGrid: DefaultRadGridDirective;

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public $postLink() {

            setTimeout(() => {

                this.radGrid = this.$element.parents(".k-popup-edit-form").data("radGridController");

                DefaultRadGridAddButtonDirective.defaultClasses.forEach(cssClass => {
                    this.$element.addClass(cssClass);
                });

            }, 0);
        }

    }
}