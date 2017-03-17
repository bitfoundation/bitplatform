/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridCancelButton" })
    export class DefaultRadGridCancelButtonDirective implements ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised"];
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                compile: function (element: JQuery, attributes: ng.IAttributes) {
                    return {
                        pre: function ($scope: ng.IScope, element, attributes: ng.IAttributes, controller: ng.INgModelController, transcludeFn: ng.ITranscludeFunction) {

                            const replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, "g"), replacement);
                            };

                            const gridIsolatedKey = angular.element(element).parents("rad-grid-editor").attr("isolatedoptionskey");

                            const newElementHtml = replaceAll(element[0].outerHTML, "rad-grid-cancel-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            let $scopeOfGrid = $scope;
                            let $gridScopeName = 'this.';
                            while ($scopeOfGrid[gridIsolatedKey] == null || $scopeOfGrid.$parent == null) {
                                $scopeOfGrid = $scopeOfGrid.$parent;
                                $gridScopeName += '$parent.';
                            }

                            newElement.attr("ng-click", `${attributes["ngClick"] || ""};${$gridScopeName}${gridIsolatedKey}Cancel($event)`);

                            if (DefaultRadGridCancelButtonDirective.defaultClasses != null && DefaultRadGridCancelButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridCancelButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
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