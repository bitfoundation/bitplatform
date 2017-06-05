module Foundation.View.Directives {
    @Core.DirectiveDependency({
        name: "RadColorPicker",
        scope: true,
        bindToController: {
        },
        controllerAs: "radColorPicker",
        template: ($element: JQuery, $attrs: ng.IAttributes & { kButtons: string, kPreview: string, kInput: string }) => {
            return `<input kendo-color-picker k-buttons="${$attrs.kButtons || 'false'}" k-preview="${$attrs.kPreview || 'false'}" k-input="${$attrs.kInput || 'false'}"></input>`;
        },
        replace: true,
        terminal: true,
        require: {
            ngModel: "ngModel"
        },
        restrict: "E"
    })
    export class DefaultRadColorPickerDirective {

        public constructor( @Core.Inject("$element") public $element: JQuery) {

        }

        public get colorPicker(): kendo.ui.ColorPicker {
            return this.$element.data("kendoColorPicker");
        }

        public $onDestroy() {
            kendo.destroyWidget(this.colorPicker);
        }

    }
}