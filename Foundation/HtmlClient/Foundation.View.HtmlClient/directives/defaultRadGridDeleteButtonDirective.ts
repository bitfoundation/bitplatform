/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridDeleteButton" })
    export class DefaultRadGridDeleteButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile: function (element, attributes) {
                    return {
                        pre: function (scope, element, attributes: ng.IAttributes, controller, transcludeFn) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const gridIsolatedKey = angular.element(element).parents("tr").attr("isolatedoptionskey");

                            const newElementHtml = replaceAll(element[0].outerHTML, "rad-grid-delete-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            newElement.attr("ng-click", `${attributes["ngClick"] || ""};${gridIsolatedKey}Delete($event)`);

                            if (DefaultRadGridDeleteButtonDirective.defaultClasses != null && DefaultRadGridDeleteButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridDeleteButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
                                    newElement.addClass(cls);
                                });
                            }

                            const dependencyManager = Core.DependencyManager.getCurrent();

                            dependencyManager.resolveObject<ng.ICompileService>("$compile")(newElement)(scope);
                        }
                    }
                }
            });
        }
    }
}