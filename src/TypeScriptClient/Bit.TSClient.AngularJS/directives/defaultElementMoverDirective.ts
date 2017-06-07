

module Bit.Directives {

    @DirectiveDependency({
        name: "ElementMover",
        scope: true,
        bindToController: {
            predicate: "<",
            elementSelector: "@"
        },
        controllerAs: "elementMover",
        restrict: "E"
    })
    export class DefaultElementMoverDirective {

        public constructor( @Inject("$element") public $element: JQuery) {

        }

        public $onChanges(changesObj: { predicate: { currentValue: boolean } }) {
            if (changesObj.predicate != null && changesObj.predicate.currentValue == true) {
                angular.element(this.elementSelector)
                    .appendTo(this.$element);
            }
        }

        public predicate: boolean;

        public elementSelector: string;
    }
}