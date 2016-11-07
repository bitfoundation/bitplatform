/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Foundation.Core.DirectiveDependency({ name: 'radCombo' })
    export class DefaultRadComboDirective implements Foundation.ViewModel.Contracts.IDirective {
        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                required: 'ngModel',
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    let itemTemplate = element
                        .children("item-template");

                    let guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    if (itemTemplate.length != 0) {

                        let itemTemplateId = guidUtils.newGuid();

                        angular.element(document.body).append(itemTemplate.attr('id', itemTemplateId));

                        attrs['itemTemplateId'] = itemTemplateId;
                    }

                    let headerTemplate = angular.element(element)
                        .children("header-template");

                    if (headerTemplate.length != 0) {

                        let headerTemplateId = guidUtils.newGuid();

                        headerTemplate.attr('id', headerTemplateId);

                        angular.element(document.body).append(headerTemplate);

                        attrs['headerTemplateId'] = headerTemplateId;
                    }

                    let replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, 'g'), replacement);
                    };

                    let isolatedOptionsKey = 'options' + replaceAll(guidUtils.newGuid(), '-', '');

                    attrs['isolatedOptionsKey'] = isolatedOptionsKey;

                    let ngModelOptions = '';
                    if (attrs['ngModel'] != null && attrs['ngModelOptions'] == null) {
                        ngModelOptions = `ng-model-options="{ updateOn : 'change' , allowInvalid : true }"`;
                    }

                    const template = `<input ${ngModelOptions} kendo-combo-box k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}"></input>`;

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

                            let ngModelAssign = null;

                            if (attributes.ngModel != null)
                                ngModelAssign = $parse(attributes.ngModel).assign;

                            $scope.$on("kendoWidgetCreated", (event, combo: kendo.ui.ComboBox) => {

                                if (combo.element[0] != element[0]) {
                                    return;
                                }

                                if (window['ngMaterial'] != null) {

                                    let mdInputContainerParent = combo.wrapper.parents('md-input-container');

                                    if (mdInputContainerParent.length != 0) {
                                        combo.wrapper
                                            .focusin(() => {
                                                if (angular.element(element).is(':disabled'))
                                                    return;
                                                mdInputContainerParent.addClass('md-input-focused');
                                            });
                                    }

                                    $scope.$on('$destroy', () => {
                                        combo.wrapper.unbind('focusin');
                                    });
                                }

                                Object.defineProperty(dataSource, "current", {
                                    configurable: true,
                                    enumerable: false,
                                    get: () => {

                                        let newCurrent = null;

                                        let dataItem = combo.dataItem();

                                        if (dataItem == null)
                                            newCurrent = null;
                                        else
                                            newCurrent = dataItem.innerInstance != null ? dataItem.innerInstance() : dataItem;

                                        return newCurrent;
                                    },
                                    set: (entity: $data.Entity) => {

                                        let value = null;

                                        if (entity != null) {
                                            value = entity[radValueFieldName];
                                            if (combo.value() != value)
                                                combo.value(value);
                                        }
                                        else {
                                            if (combo.value() != null)
                                                combo.value(null);
                                            if (combo.text() != null)
                                                combo.text(null);
                                        }

                                        if (ngModelAssign != null) {
                                            ngModelAssign($scope, value);
                                        }

                                        dataSource.onCurrentChanged();
                                    }
                                });
                            });

                            let text: string = null;

                            if (attributes.radText != null) {

                                let parsedText = $parse(attributes.radText);

                                text = parsedText($scope);

                                if (text == '')
                                    text = null;

                                if (attributes.ngModel != null) {
                                    $scope.$watch(attributes.ngModel, () => {
                                        let current = dataSource.current;
                                        if (current != null)
                                            parsedText.assign($scope, current[attributes.radTextFieldName]);
                                    });
                                }
                            }

                            const comboOptions: kendo.ui.ComboBoxOptions = {
                                dataSource: dataSource,
                                autoBind: dataSource.view().length != 0 || (attributes.radText == null ? true : false),
                                dataTextField: attributes.radTextFieldName,
                                dataValueField: radValueFieldName,
                                filter: "contains",
                                minLength: 3,
                                valuePrimitive: true,
                                ignoreCase: true,
                                suggest: true,
                                highlightFirst: true,
                                change: (e) => {
                                    dataSource.onCurrentChanged();
                                },
                                delay: 300,
                                popup: {
                                    appendTo: "md-dialog"
                                }
                            };

                            if (text != null)
                                comboOptions.text = text;

                            if (attributes['itemTemplateId'] != null) {

                                let itemTemplateElement = angular.element("#" + attributes['itemTemplateId']);

                                let itemTemplateElementHtml = itemTemplateElement.html();

                                itemTemplateElement.remove();

                                let itemTemplate: any = kendo.template(itemTemplateElementHtml);

                                comboOptions.template = itemTemplate;
                            }

                            if (attributes['headerTemplateId'] != null) {

                                let headerTemplateElement = angular.element("#" + attributes['headerTemplateId']);

                                let headerTemplateElementHtml = headerTemplateElement.html();

                                headerTemplateElement.remove();

                                let headerTemplate: any = kendo.template(headerTemplateElementHtml);

                                comboOptions.headerTemplate = headerTemplate;
                            }

                            $scope[attributes['isolatedOptionsKey']] = comboOptions;

                        });
                    });
                }
            });
        }
    }
}