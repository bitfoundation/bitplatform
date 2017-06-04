/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGridSaveButton" })
    export class DefaultRadGridSaveButtonDirective implements ViewModel.Contracts.IDirective {
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

                            const gridIsolatedKey = angular.element($element).parents("rad-grid-editor").attr("isolatedoptionskey");

                            const newElementHtml = replaceAll($element[0].outerHTML, "rad-grid-save-button", "md-button");

                            const newElement = angular.element(newElementHtml).insertAfter($element);

                            angular.element($element).remove();

                            let $scopeOfGrid = $scope;
                            let $gridScopeName = "this.";
                            while ($scopeOfGrid[gridIsolatedKey] == null || $scopeOfGrid.$parent == null) {
                                $scopeOfGrid = $scopeOfGrid.$parent;
                                $gridScopeName += "$parent.";
                            }

                            newElement.attr("ng-click", `${$attrs["ngClick"] || ""};${$gridScopeName}${gridIsolatedKey}Save($event)`);

                            if (DefaultRadGridSaveButtonDirective.defaultClasses != null && DefaultRadGridSaveButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridSaveButtonDirective.defaultClasses.filter(cls => cls != null && cls != "").forEach(cls => {
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