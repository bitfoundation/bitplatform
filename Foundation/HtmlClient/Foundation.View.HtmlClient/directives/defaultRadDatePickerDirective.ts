/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radDatePicker' })
    export class DefaultRadDatePickerDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                required: 'ngModel',
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const template = `<input kendo-date-picker />`;

                    return template;
                },
            });
        }
    }
}