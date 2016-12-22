module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "radColorPicker" })
    export class DefaultRadColorPickerDirective implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                required: "ngModel",
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const template = `<input kendo-color-picker k-buttons="false" k-preview="false" k-input="false"></input>`;

                    return template;
                }
            });
        }
    }
}