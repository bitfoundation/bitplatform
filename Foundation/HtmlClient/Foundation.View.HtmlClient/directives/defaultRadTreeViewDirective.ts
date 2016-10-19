/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radTreeView' })
    export class DefaultRadTreeViewDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                required: 'ngModel',
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    let itemTemplate = element
                        .children("item-template");

                    if (itemTemplate.length != 0) {

                        let itemTemplateId = $data.Guid['NewGuid']();

                        itemTemplate.attr('id', itemTemplateId);

                        angular.element(document.body).append(itemTemplate);

                        attrs['itemTemplateId'] = itemTemplateId;
                    }

                    let replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, 'g'), replacement);
                    };

                    let isolatedOptionsKey = 'options' + replaceAll($data.Guid['NewGuid']().value, '-', '');

                    attrs['isolatedOptionsKey'] = isolatedOptionsKey;

                    const template = `<div kendo-tree-view=${attrs['name']} k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}" />`;

                    return template;
                },
                link($scope: angular.IScope, element: JQuery, attributes: any) {

                    let dependencyManager = Core.DependencyManager.getCurrent();

                    let $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");
                    let $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");

                    $timeout(() => {

                        let watches = attributes.radText != null ? [attributes.radDatasource, (() => {
                            let modelParts = attributes.radText.split('.');
                            modelParts.pop();
                            let modelParentProp = modelParts.join('.');
                            return modelParentProp;
                        })()] : [attributes.radDatasource];

                        let watchForDataSourceUnRegisterHandler = $scope.$watchGroup(watches, (values: Array<any>) => {

                            if (values == null || values.length == 0 || values.some(v => v == null))
                                return;

                            let dataSource: kendo.data.DataSource = values[0];

                            watchForDataSourceUnRegisterHandler();

                            $scope.$on("kendoWidgetCreated", (event, tree: kendo.ui.TreeView) => {

                                if (tree.element[0] != element[0]) {
                                    return;
                                }

                                if (window['ngMaterial'] != null) {

                                    let mdInputContainerParent = tree.wrapper.parents('md-input-container');

                                    if (mdInputContainerParent.length != 0) {

                                        tree.wrapper
                                            .focusin(() => {

                                                if (angular.element(element).is(':disabled'))
                                                    return;

                                                mdInputContainerParent.addClass('md-input-focused');
                                            })
                                            .focusout(() => {
                                                mdInputContainerParent.removeClass('md-input-focused')
                                            });

                                        mdInputContainerParent.addClass('md-input-has-value');

                                        $scope.$on('$destroy', () => {
                                            tree.wrapper.unbind('focusin');
                                            tree.wrapper.unbind('focusout');
                                        });
                                    }
                                }

                                if (attributes.onInit != null)
                                    $parse(attributes.onInit)($scope);

                            });

                            const treeViewOptions: kendo.ui.TreeViewOptions = {
                                dataSource: dataSource,
                                autoBind: true,
                                dataTextField: attributes.radTextFieldName,
                                autoScroll: true,
                                animation: true,
                                checkboxes: false,
                                dragAndDrop: false,
                                loadOnDemand: true
                            };

                            if (attributes['itemTemplateId'] != null) {

                                let itemTemplateElement = angular.element("#" + attributes['itemTemplateId']);

                                let itemTemplateElementHtml = itemTemplateElement.html();

                                itemTemplateElement.remove();

                                let itemTemplate: any = kendo.template(itemTemplateElementHtml);

                                treeViewOptions.template = itemTemplate;
                            }

                            $scope[attributes['isolatedOptionsKey']] = treeViewOptions;

                        });
                    });
                }
            });
        }
    }
}