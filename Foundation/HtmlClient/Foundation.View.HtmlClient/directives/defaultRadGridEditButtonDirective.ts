/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridEditButton" })
    export class DefaultRadGridEditButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile(element: JQuery, attributes: ng.IAttributes) {
                    return {
                        pre($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, controller: ng.INgModelController, transcludeFn: ng.ITranscludeFunction) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const gridIsolatedKey = angular.element(element).parents("rad-grid-element").attr("isolated-options-key");

                            const newElementHtml = replaceAll(element[0].outerHTML, "rad-grid-edit-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            newElement.attr("ng-click", `${attributes["ngClick"] || ""};${gridIsolatedKey}Update($event)`);

                            if (DefaultRadGridEditButtonDirective.defaultClasses != null && DefaultRadGridEditButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridEditButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
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