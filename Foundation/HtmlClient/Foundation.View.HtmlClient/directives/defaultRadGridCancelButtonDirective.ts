/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radGridCancelButton' })
    export class DefaultRadGridCancelButtonDirective implements Foundation.ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised"];
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                compile: function (element, attributes) {
                    return {
                        pre: function ($scope, element, attributes: ng.IAttributes, controller, transcludeFn) {

                            let replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, 'g'), replacement);
                            };

                            let gridIsolatedKey = angular.element(element).parents('fake').attr('isolatedoptionskey');

                            let newElementHtml = replaceAll(element[0].outerHTML, 'rad-grid-cancel-button', 'md-button');

                            let newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            newElement.attr('ng-click', `${attributes['ngClick'] || ''};${gridIsolatedKey}Cancel($event)`);

                            if (DefaultRadGridCancelButtonDirective.defaultClasses != null && DefaultRadGridCancelButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridCancelButtonDirective.defaultClasses.filter(cls => cls != null && cls != '').forEach(cls => {
                                    newElement.addClass(cls);
                                });
                            }

                            let dependencyManager = Core.DependencyManager.getCurrent();

                            dependencyManager.resolveObject<ng.ICompileService>("$compile")(newElement)($scope);
                        }
                    }
                }
            });
        }
    }
}