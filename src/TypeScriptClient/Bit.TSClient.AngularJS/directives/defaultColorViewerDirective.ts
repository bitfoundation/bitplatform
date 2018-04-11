module Bit.Directives {
    @DirectiveDependency({
        name: "ColorViewer",
        scope: true,
        bindToController: {
            color: "<"
        },
        controllerAs: "colorViewer",
        template: ($element: JQuery, $attrs: ng.IAttributes & { kButtons: string, kPreview: string, kInput: string }) => {
            return `<div></div>`;
        },
        replace: true,
        terminal: true,
        restrict: "E"
    })
    export class DefaultColorViewerDirective {

        public constructor(@Inject("$element") public $element: JQuery) {

        }

        public $onChanges(changes: { color: { currentValue: string } }) {
            if (changes.color != null) {
                this.$element.css("background-color", changes.color.currentValue);
            }
        }
    }
}