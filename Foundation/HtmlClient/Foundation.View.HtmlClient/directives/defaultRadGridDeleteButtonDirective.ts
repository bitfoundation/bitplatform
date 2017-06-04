/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridDeleteButton", usesOldStyle: true })
    export class DefaultRadGridDeleteButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile($element: JQuery, $attrs: ng.IAttributes) {
                    return {
                        pre($scope: ng.IScope, $element: JQuery, $attrs: ng.IAttributes, controller: ng.INgModelController, transcludeFn: ng.ITranscludeFunction) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const gridIsolatedKey = angular.element($element).parents("rad-grid-element").attr("isolated-options-key");

                            const newElementHtml = replaceAll($element[0].outerHTML, "rad-grid-delete-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter($element);

                            angular.element($element).remove();

                            newElement.attr("ng-click", `${$attrs["ngClick"] || ""};${gridIsolatedKey}Delete($event)`);

                            if (DefaultRadGridDeleteButtonDirective.defaultClasses != null && DefaultRadGridDeleteButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridDeleteButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
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