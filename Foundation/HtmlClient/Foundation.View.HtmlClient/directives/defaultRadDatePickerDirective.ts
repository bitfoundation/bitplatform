/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadDatePicker",
        scope: true,
        bindToController: {
        },
        controllerAs: "radDatePicker",
        template: "<input kendo-date-picker />",
        replace: true,
        terminal: true,
        require: {
            ngModel: "ngModel"
        },
        restrict: "E"
    })
    export class DefaultRadDatePickerDirective {

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public get datePicker(): kendo.ui.DatePicker {
            return this.$element.data("kendoDatePicker");
        }

        public $onDestroy() {
            kendo.destroyWidget(this.datePicker);
        }

    }
}