/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Foundation.Core.DirectiveDependency({ name: 'radGridAddButton' })
    export class DefaultRadGridAddButtonDirective implements Foundation.ViewModel.Contracts.IDirective {
        public static defaultClasses: string[] = ["md-raised", "md-primary"];
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                compile: function (element, attributes) {
                    return {
                        pre: function ($scope, element, attributes: ng.IAttributes, controller, transcludeFn) {

                            let replaceAll = (text: string, search: string, replacement: string) => {
                                return text.replace(new RegExp(search, 'g'), replacement);
                            };

                            let gridEquivalentFakeElement = angular.element(element).parents('.k-grid').find('fake-element');
                            let gridIsolatedKey = gridEquivalentFakeElement.attr('k-options').replace('::', '');

                            let newElementHtml = replaceAll(element[0].outerHTML, 'rad-grid-add-button', 'md-button');

                            let newElement = angular.element(newElementHtml).insertAfter(element);

                            angular.element(element).remove();

                            newElement.attr('ng-click', `${attributes['ngClick'] || ''};${gridIsolatedKey}Add($event)`);

                            if (DefaultRadGridAddButtonDirective.defaultClasses != null && DefaultRadGridAddButtonDirective.defaultClasses.length != 0) {
                                DefaultRadGridAddButtonDirective.defaultClasses.filter(cls => cls != null && cls != '').forEach(cls => {
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