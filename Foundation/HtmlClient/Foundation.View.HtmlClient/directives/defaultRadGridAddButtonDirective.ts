/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "radGridAddButton" })
    export class DefaultRadGridAddButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised", "md-primary"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile($element: JQuery, $attrs: ng.IAttributes) {
                    return {
                        pre($scope: ng.IScope, $element: JQuery, $attrs: ng.IAttributes, controller: ng.INgModelController, transcludeFn: ng.ITranscludeFunction) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const radGridElement = angular.element($element).parents(".k-grid").find("rad-grid-element");
                            const gridIsolatedKey = radGridElement.attr("isolated-options-key");

                            const newElementHtml = replaceAll($element[0].outerHTML, "rad-grid-add-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter($element);

                            angular.element($element).remove();

                            newElement.attr("ng-click", `${$attrs["ngClick"] || ""};${gridIsolatedKey}Add($event)`);

                            if (DefaultRadGridAddButtonDirective.defaultClasses != null && DefaultRadGridAddButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridAddButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
                                    newElement.addClass(cls);
                                });
                            }

                            const dependencyManager = Core.DependencyManager.getCurrent();

                            dependencyManager.resolveObject<ng.ICompileService>("$compile")(newElement)($scope);
                        }
                    }
                }
            });
        }
    }
}