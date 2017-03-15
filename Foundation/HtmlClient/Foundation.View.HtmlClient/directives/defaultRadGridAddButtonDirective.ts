/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "radGridAddButton" })
    export class DefaultRadGridAddButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised", "md-primary"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile: function (element: JQuery, attributes: ng.IAttributes) {
                    return {
                        pre: function ($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, controller: ng.INgModelController, transcludeFn: ng.ITranscludeFunction) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const radGridElement = angular.element(element).parents(".k-grid").find("rad-grid-element");
                            const gridIsolatedKey = radGridElement.attr("isolated-options-key");

                            const newElementHtml = replaceAll(element[0].outerHTML, "rad-grid-add-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            newElement.attr("ng-click", `${attributes["ngClick"] || ""};${gridIsolatedKey}Add($event)`);

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