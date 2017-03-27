module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "radEditor" })
    export class DefaultRadEditor implements ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                require: {
                    mdInputContainer: "^?mdInputContainer",
                    ngModel: "ngModel"
                },
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    const replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, "g"), replacement);
                    };

                    const delayKey = `delay${replaceAll(guidUtils.newGuid(), "-", "")}`;

                    attrs["delayKey"] = delayKey;

                    const template = `<textarea kendo-editor k-ng-delay="::${delayKey}"></textarea>`;

                    return template;

                },
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, requireArgs: { mdInputContainer: { element: JQuery, setHasValue: () => void } }) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");

                    $timeout(() => {

                        const kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, editor: kendo.ui.Editor) => {

                            if (editor.element[0] != element[0]) {
                                return;
                            }

                            kendoWidgetCreatedDisposal();

                            if (requireArgs.mdInputContainer != null) {

                                const mdInputContainerParent = requireArgs.mdInputContainer.element;

                                angular.element(editor.body).focusin(() => {
                                    if (angular.element(element).is(":disabled"))
                                        return;
                                    mdInputContainerParent.addClass("md-input-focused");
                                });

                                mdInputContainerParent.addClass("md-input-has-value");

                                requireArgs.mdInputContainer.setHasValue = function () {

                                };

                                const $destroyDisposal = $scope.$on("$destroy", () => {
                                    angular.element(editor.body).unbind("focusin");
                                    $destroyDisposal();
                                });

                            }
                        });
                    });

                    $scope[attributes["delayKey"]] = {};
                }
            })
        }
    }
}