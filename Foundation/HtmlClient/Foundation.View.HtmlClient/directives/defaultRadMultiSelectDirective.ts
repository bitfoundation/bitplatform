/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radMultiSelect' })
    export class DefaultRadMultiSelectDirective implements Foundation.ViewModel.Contracts.IDirective {
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

                    let tagTemplate = element
                        .children("tag-template");

                    if (tagTemplate.length != 0) {

                        let tagTemplateId = $data.Guid['NewGuid']();

                        tagTemplate.attr('id', tagTemplateId);

                        angular.element(document.body).append(tagTemplate);

                        attrs['tagTemplateId'] = tagTemplateId;
                    }

                    let headerTemplate = element
                        .children("header-template");

                    if (headerTemplate.length != 0) {

                        let headerTemplateId = $data.Guid['NewGuid']();

                        headerTemplate.attr('id', headerTemplateId);

                        angular.element(document.body).append(headerTemplate);

                        attrs['headerTemplateId'] = headerTemplateId;
                    }

                    let replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, 'g'), replacement);
                    };

                    let isolatedOptionsKey = 'options' + replaceAll($data.Guid['NewGuid']().value, '-', '');

                    attrs['isolatedOptionsKey'] = isolatedOptionsKey;

                    let ngModelOptions = '';
                    if (attrs['ngModel'] != null && attrs['ngModelOptions'] == null) {
                        ngModelOptions = `ng-model-options="{ updateOn : 'change' , allowInvalid : true }"`;
                    }

                    const template = `<select ${ngModelOptions} kendo-multi-select k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}"></select>`;

                    return template;
                },
                link($scope: angular.IScope, element: JQuery, attributes: any) {

                    let dependencyManager = Core.DependencyManager.getCurrent();

                    let $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");
                    let $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");
                    let ngModelAssign = null;
                    if (attributes.ngModel != null)
                        ngModelAssign = $parse(attributes.ngModel).assign;

                    $timeout(() => {

                        let watches = attributes.radText != null ? [attributes.radDatasource, (() => {
                            let modelParts = attributes.radText.split('.');
                            modelParts.pop();
                            let modelParentProp = modelParts.join('.');
                            return modelParentProp;
                        })()] : [attributes.radDatasource];

                        let model = null;

                        let watchForDatasourceAndNgModelIfAnyToCreateComboWidgetUnRegisterHandler = $scope.$watchGroup(watches, (values: Array<any>) => {

                            if (values == null || values.length == 0 || values.some(v => v == null))
                                return;

                            let dataSource: kendo.data.DataSource = values[0];

                            if (values.length == 2) {
                                model = values[1];
                            }

                            watchForDatasourceAndNgModelIfAnyToCreateComboWidgetUnRegisterHandler();

                            let radValueFieldName = attributes.radValueFieldName;

                            if (radValueFieldName == null) {
                                if (dataSource.options.schema != null && dataSource.options.schema.model != null && dataSource.options.schema.model.idField != null)
                                    radValueFieldName = dataSource.options.schema.model.idField;
                                else
                                    radValueFieldName = "Id";
                            }

                            $scope.$on("kendoWidgetCreated", (event, multiSelect: kendo.ui.MultiSelect) => {

                                if (multiSelect.element[0] != element[0]) {
                                    return;
                                }

                                if (window['ngMaterial'] != null) {

                                    let mdInputContainerParent = multiSelect.wrapper.parents('md-input-container');

                                    if (mdInputContainerParent.length != 0) {

                                        multiSelect.wrapper
                                            .focusin(() => {

                                                if (angular.element(element).is(':disabled'))
                                                    return;

                                                mdInputContainerParent.addClass('md-input-focused');

                                                multiSelect.open(); // Should be removed
                                            })
                                            .focusout(() => {
                                                mdInputContainerParent.removeClass('md-input-focused')
                                            });

                                        $scope.$watchCollection<Array<any>>(attributes.ngModel, (newVal, oldVal) => {
                                            if (newVal != null && newVal.length != 0)
                                                mdInputContainerParent.addClass('md-input-has-value');
                                            else
                                                mdInputContainerParent.removeClass('md-input-has-value');
                                        });


                                        $scope.$on('$destroy', () => {
                                            multiSelect.wrapper.unbind('focusin');
                                            multiSelect.wrapper.unbind('focusout');
                                        });
                                    }
                                }

                                $scope.$watchCollection(attributes.ngModel, (newValue, oldVal) => {
                                    multiSelect.value(newValue);
                                });
                            });

                            const multiSelectOptions: kendo.ui.MultiSelectOptions = {
                                dataSource: dataSource,
                                autoBind: true,
                                dataTextField: attributes.radTextFieldName,
                                dataValueField: radValueFieldName,
                                filter: "contains",
                                minLength: 3,
                                valuePrimitive: true,
                                ignoreCase: true,
                                highlightFirst: true,
                                delay: attributes.delay || 300,
                                popup: {
                                    appendTo: "md-dialog"
                                },
                                change: function () {
                                    if (ngModelAssign != null)
                                        ngModelAssign($scope, (this as kendo.ui.MultiSelect).value());
                                },
                                autoClose: false // Should be removed
                            };

                            if (attributes['itemTemplateId'] != null) {

                                let itemTemplateElement = angular.element("#" + attributes['itemTemplateId']);

                                let itemTemplateElementHtml = itemTemplateElement.html();

                                itemTemplateElement.remove();

                                let itemTemplate: any = kendo.template(itemTemplateElementHtml);

                                multiSelectOptions.itemTemplate = itemTemplate;
                            }

                            if (attributes['tagTemplateId'] != null) {

                                let tagTemplateElement = angular.element("#" + attributes['tagTemplateId']);

                                let tagTemplateElementHtml = tagTemplateElement.html();

                                tagTemplateElement.remove();

                                let tagTemplate: any = kendo.template(tagTemplateElementHtml);

                                multiSelectOptions.tagTemplate = tagTemplate;
                            }

                            if (attributes['headerTemplateId'] != null) {

                                let headerTemplateElement = angular.element("#" + attributes['headerTemplateId']);

                                let headerTemplateElementHtml = headerTemplateElement.html();

                                headerTemplateElement.remove();

                                let headerTemplate: any = kendo.template(headerTemplateElementHtml);

                                multiSelectOptions.headerTemplate = headerTemplate;
                            }

                            $scope[attributes['isolatedOptionsKey']] = multiSelectOptions;

                        });
                    });
                }
            });
        }
    }
}