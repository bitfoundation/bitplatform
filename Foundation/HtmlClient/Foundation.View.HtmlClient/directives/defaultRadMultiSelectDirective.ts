/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radMultiSelect" })
    export class DefaultRadMultiSelectDirective implements ViewModel.Contracts.IDirective {

        public static defaultRadMultiSelectDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, multiSelectOptions: kendo.ui.MultiSelectOptions) => void> = [];

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

                    const guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    const itemTemplate = element
                        .children("item-template");

                    if (itemTemplate.length != 0) {

                        const itemTemplateId = guidUtils.newGuid();

                        itemTemplate
                            .attr("id", itemTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(itemTemplate);

                        attrs["itemTemplateId"] = itemTemplateId;
                    }

                    const tagTemplate = element
                        .children("tag-template");

                    if (tagTemplate.length != 0) {

                        const tagTemplateId = guidUtils.newGuid();

                        tagTemplate
                            .attr("id", tagTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(tagTemplate);

                        attrs["tagTemplateId"] = tagTemplateId;
                    }

                    const headerTemplate = element
                        .children("header-template");

                    if (headerTemplate.length != 0) {

                        const headerTemplateId = guidUtils.newGuid();

                        headerTemplate
                            .attr("id", headerTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(headerTemplate);

                        attrs["headerTemplateId"] = headerTemplateId;
                    }

                    const replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, "g"), replacement);
                    };

                    const isolatedOptionsKey = `options${replaceAll(guidUtils.newGuid(), "-", "")}`;

                    attrs["isolatedOptionsKey"] = isolatedOptionsKey;

                    let ngModelOptions = "";
                    if (attrs["ngModel"] != null && attrs["ngModelOptions"] == null) {
                        ngModelOptions = `ng-model-options="{ updateOn : 'change' , allowInvalid : true }"`;
                    }

                    const template = `<select ${ngModelOptions} kendo-multi-select k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}"></select>`;

                    return template;
                },
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes, requireArgs: { mdInputContainer: { element: JQuery } }) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");
                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");
                    let ngModelAssign = null;
                    if (attributes.ngModel != null)
                        ngModelAssign = $parse(attributes.ngModel).assign;

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

                            let kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, multiSelect: kendo.ui.MultiSelect) => {

                                if (multiSelect.element[0] != element[0]) {
                                    return;
                                }

                                kendoWidgetCreatedDisposal();

                                $scope.$on("$destroy", () => {

                                    if (multiSelect.wrapper != null) {

                                        multiSelect.wrapper.each(function (id, kElement) {
                                            const dataObj = angular.element(kElement).data();
                                            for (let mData in dataObj) {
                                                if (angular.isObject(dataObj[mData])) {
                                                    if (typeof dataObj[mData]["destroy"] == "function") {
                                                        dataObj[mData].destroy();
                                                    }
                                                }
                                            }
                                        });

                                        multiSelect.wrapper.remove();
                                    }

                                    multiSelect.destroy();

                                });

                                if (requireArgs.mdInputContainer != null) {

                                    const mdInputContainerParent = requireArgs.mdInputContainer.element;

                                    multiSelect.wrapper
                                        .focusin(() => {

                                            if (angular.element(element).is(":disabled"))
                                                return;

                                            mdInputContainerParent.addClass("md-input-focused");

                                            multiSelect.open(); // Should be removed
                                        })
                                        .focusout(() => {
                                            mdInputContainerParent.removeClass("md-input-focused");
                                        });

                                    $scope.$watchCollection<Array<any>>(attributes.ngModel.replace("::", ""), (newVal, oldVal) => {
                                        if (newVal != null && newVal.length != 0)
                                            mdInputContainerParent.addClass("md-input-has-value");
                                        else
                                            mdInputContainerParent.removeClass("md-input-has-value");
                                    });


                                    const $destroyDisposal = $scope.$on("$destroy", () => {
                                        multiSelect.wrapper.unbind("focusin");
                                        multiSelect.wrapper.unbind("focusout");
                                        $destroyDisposal();
                                    });
                                }

                                $scope.$watchCollection(attributes.ngModel.replace("::", ""), (newValue, oldVal) => {
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

                            if (attributes["itemTemplateId"] != null) {

                                let itemTemplateElement = angular.element(`#${attributes["itemTemplateId"]}`);

                                let itemTemplateElementHtml = itemTemplateElement.html();

                                let itemTemplate: any = kendo.template(itemTemplateElementHtml, { useWithBlock: false });

                                multiSelectOptions.itemTemplate = itemTemplate;
                            }

                            if (attributes["tagTemplateId"] != null) {

                                let tagTemplateElement = angular.element(`#${attributes["tagTemplateId"]}`);

                                let tagTemplateElementHtml = tagTemplateElement.html();

                                let tagTemplate: any = kendo.template(tagTemplateElementHtml, { useWithBlock: false });

                                multiSelectOptions.tagTemplate = tagTemplate;
                            }

                            if (attributes["headerTemplateId"] != null) {

                                let headerTemplateElement = angular.element(`#${attributes["headerTemplateId"]}`);

                                let headerTemplateElementHtml = headerTemplateElement.html();

                                let headerTemplate: any = kendo.template(headerTemplateElementHtml, { useWithBlock: false });

                                multiSelectOptions.headerTemplate = headerTemplate;
                            }

                            if (dataSource.options.schema.model.fields[multiSelectOptions.dataTextField] == null)
                                throw new Error(`Model has no property named ${multiSelectOptions.dataTextField} to be used as text field`);

                            if (dataSource.options.schema.model.fields[multiSelectOptions.dataValueField] == null)
                                throw new Error(`Model has no property named ${multiSelectOptions.dataValueField} to be used as value field`);

                            DefaultRadMultiSelectDirective.defaultRadMultiSelectDirectiveCustomizers.forEach(multiSelectCustomizer => {
                                multiSelectCustomizer($scope, attributes, element, multiSelectOptions);
                            });

                            if (attributes.onInit != null) {
                                let onInitFN = $parse(attributes.onInit);
                                if (typeof onInitFN == "function") {
                                    onInitFN($scope, { multiSelectOptions: multiSelectOptions });
                                }
                            }

                            $scope[attributes["isolatedOptionsKey"]] = multiSelectOptions;

                        });
                    });
                }
            });
        }
    }
}