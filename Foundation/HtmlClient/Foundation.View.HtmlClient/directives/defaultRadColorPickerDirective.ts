module Foundation.View.Directives {
    @Core.DirectiveDependency({
        name: "RadColorPicker",
        scope: false,
        replace: true,
        terminal: true,
        require: "ngModel",
        template: ($element: JQuery, $attrs: ng.IAttributes) => {

            const template = `<input kendo-color-picker k-buttons="false" k-preview="false" k-input="false"></input>`;

            return template;
        }
    })
    export class DefaultRadColorPickerDirective {
    }
}