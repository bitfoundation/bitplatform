/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {
    @Core.DirectiveDependency({ name: "radCombo" })
    export class DefaultRadComboDirective implements ViewModel.Contracts.IDirective {

        public static defaultRadComboDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, comboBoxOptions: kendo.ui.ComboBoxOptions) => void> = [];

        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                require: {
                    mdInputContainer: "^?mdInputContainer",
                    ngModel: "ngModel"
                },
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const itemTemplate = element
                        .children("item-template");

                    const guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    if (itemTemplate.length != 0) {

                        const itemTemplateId = guidUtils.newGuid();

                        angular.element(document.body).append(itemTemplate.attr("id", itemTemplateId).attr('ng-cloak', ''));

                        attrs["itemTemplateId"] = itemTemplateId;
                    }

                    const headerTemplate = angular.element(element)
                        .children("header-template");

                    if (headerTemplate.length != 0) {

                        const headerTemplateId = guidUtils.newGuid();

                        headerTemplate
                            .attr("id", headerTemplateId)
                            .attr('ng-cloak', '');

                        angular.element(document.body).append(headerTemplate);

                        attrs["headerTemplateId"] = headerTemplateId;
                    }

                    const replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, "g"), replacement);
                    };

                    const isolatedOptionsKey = "options" + replaceAll(guidUtils.newGuid(), "-", "");

                    attrs["isolatedOptionsKey"] = isolatedOptionsKey;

                    let ngModelOptions = "";
                    if (attrs["ngModel"] != null && attrs["ngModelOptions"] == null) {
                        ngModelOptions = `ng-model-options="{ updateOn : 'change' , allowInvalid : true }"`;
                    }

                    const template = `<input ${ngModelOptions} kendo-combo-box k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}"></input>`;

                    return template;
                },
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, requireArgs: { mdInputContainer: { element: JQuery } }) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                    $timeout(() => {

                        const watches = attributes.radText != null ? [attributes.radDatasource, (() => {
                            const modelParts = attributes.radText.split(".");
                            modelParts.pop();
                            const modelParentProp = modelParts.join(".");
                            return modelParentProp;
                        })()] : [attributes.radDatasource];

                        let model = null;

                        const watchForDatasourceAndNgModelIfAnyToCreateComboWidgetUnRegisterHandler = $scope.$watchGroup(watches, (values: Array<any>) => {

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

                            let kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, combo: kendo.ui.ComboBox) => {

                                if (combo.element[0] != element[0]) {
                                    return;
                                }

                                kendoWidgetCreatedDisposal();

                                $scope.$on("$destroy", () => {

                                    delete dataSource.current;

                                    if (combo.wrapper != null) {

                                        combo.wrapper.each(function (id, kElement) {
                                            let dataObj = angular.element(kElement).data();
                                            for (let mData in dataObj) {
                                                if (angular.isObject(dataObj[mData])) {
                                                    if (typeof dataObj[mData]["destroy"] == "function") {
                                                        dataObj[mData].destroy();
                                                    }
                                                }
                                            }
                                        });

                                        combo.wrapper.remove();
                                    }

                                    combo.destroy();

                                });

                                if (requireArgs.mdInputContainer != null) {

                                    const mdInputContainerParent = requireArgs.mdInputContainer.element;

                                        combo.wrapper
                                            .focusin(() => {
                                                if (angular.element(element).is(":disabled"))
                                                    return;
                                                mdInputContainerParent.addClass("md-input-focused");
                                            });

                                    const $destroyDisposal = $scope.$on("$destroy", () => {
                                        combo.wrapper.unbind("focusin");
                                        $destroyDisposal();
                                    });
                                }

                                Object.defineProperty(dataSource, "current", {
                                    configurable: true,
                                    enumerable: false,
                                    get: () => {

                                        let newCurrent = null;

                                        const dataItem = combo.dataItem();

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

                                if (text == "")
                                    text = null;

                                if (attributes.ngModel != null) {
                                    $scope.$watch(attributes.ngModel.replace('::', ''), () => {
                                        const current = dataSource.current;
                                        if (current != null)
                                            parsedText.assign($scope, current[attributes.radTextFieldName]);
                                    });
                                }
                            }

                            const comboBoxOptions: kendo.ui.ComboBoxOptions = {
                                dataSource: dataSource,
                                autoBind: dataSource.flatView().length != 0 || attributes.radText == null,
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
                                open: (e) => {
                                    if (e.sender.options.autoBind == false && attributes.radText != null) {
                                        e.sender.options.autoBind = true;
                                        if (e.sender.options.dataSource.flatView().length == 0)
                                            (e.sender.options.dataSource as kendo.data.DataSource).fetch();
                                    }
                                },
                                delay: 300,
                                popup: {
                                    appendTo: "md-dialog"
                                }
                            };

                            if (text != null)
                                comboBoxOptions.text = text;

                            if (attributes["itemTemplateId"] != null) {

                                let itemTemplateElement = angular.element("#" + attributes["itemTemplateId"]);

                                let itemTemplateElementHtml = itemTemplateElement.html();

                                let itemTemplate: any = kendo.template(itemTemplateElementHtml);

                                comboBoxOptions.template = itemTemplate;
                            }

                            if (attributes["headerTemplateId"] != null) {

                                let headerTemplateElement = angular.element("#" + attributes["headerTemplateId"]);

                                let headerTemplateElementHtml = headerTemplateElement.html();

                                let headerTemplate: any = kendo.template(headerTemplateElementHtml);

                                comboBoxOptions.headerTemplate = headerTemplate;
                            }

                            if (dataSource.options.schema.model.fields[comboBoxOptions.dataTextField] == null)
                                throw new Error(`Model has no property named ${comboBoxOptions.dataTextField} to be used as text field`);

                            if (dataSource.options.schema.model.fields[comboBoxOptions.dataValueField] == null)
                                throw new Error(`Model has no property named ${comboBoxOptions.dataValueField} to be used as value field`);

                            DefaultRadComboDirective.defaultRadComboDirectiveCustomizers.forEach(radComboCustomizer => {
                                radComboCustomizer($scope, attributes, element, comboBoxOptions);
                            });

                            if (attributes.onInit != null) {
                                let onInitFN = $parse(attributes.onInit);
                                if (typeof onInitFN == 'function') {
                                    onInitFN($scope, { comboBoxOptions: comboBoxOptions });
                                }
                            }

                            $scope[attributes["isolatedOptionsKey"]] = comboBoxOptions;

                        });
                    });
                }
            });
        }
    }
}