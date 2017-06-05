module Foundation.View.Directives {
    @Core.DirectiveDependency({
        name: "RadEditor",
        scope: true,
        bindToController: {
        },
        controllerAs: "radEditor",
        template: `<textarea kendo-editor k-ng-delay="::radEditor.options"></textarea>`,
        replace: true,
        terminal: true,
        require: {
            mdInputContainer: "^?mdInputContainer",
            ngModel: "ngModel"
        },
        restrict: "E"
    })
    export class DefaultRadEditor {

        public constructor( @Core.Inject("$element") public $element: JQuery, @Core.Inject("$scope") public $scope: ng.IScope) {

        }

        @ViewModel.Command()
        public async $onInit(): Promise<void> {

            let kendoWidgetCreatedDisposal = this.$scope.$on("kendoWidgetCreated", () => {
                kendoWidgetCreatedDisposal();
                this.onWidgetCreated.bind(this);
            });

            this.options = {

            };
        }

        public onWidgetCreated() {

            if (this.mdInputContainer != null) {

                this.mdInputContainerParent = this.mdInputContainer.element;

                angular.element(this.editor.body).bind("focusin", this.onEditorFocusIn.bind(this));

                this.mdInputContainerParent.addClass("md-input-has-value");

                this.mdInputContainer.setHasValue = function () {

                };
            }
        }

        public onEditorFocusIn() {
            if (this.$element.is(":disabled"))
                return;
            this.mdInputContainerParent.addClass("md-input-focused");
        }

        public mdInputContainer: { element: JQuery, setHasValue: () => void };
        public mdInputContainerParent: JQuery;
        public options: kendo.ui.EditorOptions;

        public get editor(): kendo.ui.Editor {
            return this.$element.data("kendoEditor");
        }

        public $onDestroy() {
            if (this.editor != null) {
                angular.element(this.editor.body).unbind("focusin", this.onEditorFocusIn);
                kendo.destroyWidget(this.editor);
            }
        }

    }
}