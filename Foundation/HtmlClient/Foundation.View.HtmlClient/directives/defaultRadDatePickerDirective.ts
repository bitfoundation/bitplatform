/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({
        name: "RadDatePicker",
        scope: false,
        replace: true,
        terminal: true,
        require: "ngModel",
        template: ($element: JQuery, $attrs: ng.IAttributes) => {

            const template = `<input kendo-date-picker />`;

            return template;
        }
    })
    export class DefaultRadDatePickerDirective {

    }
}