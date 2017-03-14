/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radDatePicker" })
    export class DefaultRadDatePickerDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                require: "ngModel",
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const template = `<input kendo-date-picker />`;

                    return template;
                }
            });
        }
    }
}