/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGrid" })
    export class DefaultRadGridDirective implements ViewModel.Contracts.IDirective {

        public static defaultRadGridDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, gridOptions: kendo.ui.GridOptions) => void> = [];
        public static defaultRadGridDirectiveColumnCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, element: JQuery, gridOptions: kendo.ui.GridOptions, columnElement: JQuery, gridColumn: kendo.ui.GridColumn) => void> = [];

        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    const guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    const replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, "g"), replacement);
                    };

                    const isolatedOptionsKey = `options${replaceAll(guidUtils.newGuid(), "-", "")}`;

                    attrs["isolatedOptionsKey"] = isolatedOptionsKey;

                    const gridTemplate = `<rad-grid-element kendo-grid k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}" />`;

                    const viewRowTemplateId = replaceAll(guidUtils.newGuid(), "-", "");

                    const viewTemplate = angular.element(element)
                        .children("view-template")
                        .attr("id", viewRowTemplateId)
                        .attr("ng-cloak", "");

                    angular.element(document.body).append(viewTemplate);

                    attrs["viewTemplateId"] = viewRowTemplateId;

                    const editTemplate = angular.element(element)
                        .children("edit-template");

                    if (editTemplate.length != 0) {

                        const editRowTemplateId = replaceAll(guidUtils.newGuid(), "-", "");

                        editTemplate
                            .attr("id", editRowTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(editTemplate);

                        attrs["editTemplateId"] = editRowTemplateId;
                    }

                    const toolbarTemplate = angular.element(element)
                        .children("toolbar-template");

                    if (toolbarTemplate.length != 0) {

                        const toolbarTemplateId = replaceAll(guidUtils.newGuid(), "-", "");

                        toolbarTemplate
                            .attr("id", toolbarTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(toolbarTemplate);

                        attrs["toolbarTemplateId"] = toolbarTemplateId;
                    }

                    const detailTemplate = angular.element(element)
                        .children("detail-template");

                    if (detailTemplate.length != 0) {

                        const detailTemplateId = replaceAll(guidUtils.newGuid(), "-", "");

                        detailTemplate
                            .attr("id", detailTemplateId)
                            .attr("ng-cloak", "");

                        angular.element(document.body).append(detailTemplate);

                        attrs["detailTemplateId"] = detailTemplateId;
                    }

                    return gridTemplate;
                },
                link($scope: ng.IScope, element: JQuery, attributes: ng.IAttributes & { radDatasource: string, onInit: string }) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");

                    const $translate = dependencyManager.resolveObject<ng.translate.ITranslateService>("$translate");

                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                    const $interpolate = dependencyManager.resolveObject<ng.IInterpolateService>("$interpolate");

                    const dateTimeService = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Contracts.IDateTimeService>("DateTimeService");

                    const clientAppProfileManager = dependencyManager.resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager");

                    function dsError(e: { sender: kendo.data.DataSource }) {
                        if (e.sender["destroyed"]().length != 0) {
                            e.sender.cancelChanges();
                        }
                    }

                    $timeout(() => {

                        const watchForDatasourceToCreateDataGridWidgetUnRegisterHandler = $scope.$watch(attributes.radDatasource, (dataSource: kendo.data.DataSource) => {

                            if (dataSource == null)
                                return;

                            watchForDatasourceToCreateDataGridWidgetUnRegisterHandler();

                            const kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, grid: kendo.ui.Grid) => {

                                if (grid.element[0] != element[0]) {
                                    return;
                                }

                                kendoWidgetCreatedDisposal();

                                $scope.$on("$destroy", () => {

                                    delete dataSource.current;
                                    dataSource.unbind("error", dsError);

                                    if (grid.wrapper != null) {

                                        grid.wrapper.each(function (id, kElement) {
                                            const dataObj = angular.element(kElement).data();
                                            for (let mData in dataObj) {
                                                if (dataObj.hasOwnProperty(mData)) {
                                                    if (angular.isObject(dataObj[mData])) {
                                                        if (typeof dataObj[mData]["destroy"] == "function") {
                                                            dataObj[mData].destroy();
                                                        }
                                                    }
                                                }
                                            }
                                        });

                                        grid.wrapper.remove();
                                    }

                                    grid.destroy();

                                });

                                $scope[attributes["isolatedOptionsKey"] + "Delete"] = ($event) => {

                                    const row = angular.element($event.currentTarget).parents("tr");

                                    grid.removeRow(row);

                                };

                                if (attributes["editTemplateId"] != null) {

                                    $scope[attributes["isolatedOptionsKey"] + "Add"] = () => {
                                        grid.addRow();
                                    };

                                    $scope[attributes["isolatedOptionsKey"] + "Update"] = ($event) => {

                                        const row = angular.element($event.currentTarget).parents("tr");

                                        grid.editRow(row);

                                    };

                                    $scope[attributes["isolatedOptionsKey"] + "Cancel"] = ($event) => {
                                        const uid = angular.element($event.target).parents(".k-popup-edit-form").attr("data-uid");
                                        grid.trigger("cancel", { container: angular.element($event.target).parents(".k-window"), sender: grid, model: grid.dataSource.flatView().find(i => i["uid"] == uid) });
                                        grid.cancelRow();
                                    };

                                    $scope[attributes["isolatedOptionsKey"] + "Save"] = ($event) => {

                                        grid.saveRow();

                                    };
                                }

                                Object.defineProperty(dataSource, "current", {
                                    configurable: true,
                                    enumerable: false,
                                    get: () => {

                                        let current = null;

                                        const itemBeingInserted = grid.dataSource
                                            .flatView().find(i => i["isNew"]() == true);

                                        if (itemBeingInserted != null)
                                            current = itemBeingInserted.innerInstance != null ? itemBeingInserted.innerInstance() : itemBeingInserted;

                                        if (current == null) {

                                            const selectedDataItem = grid.dataItem(grid.select());

                                            if (selectedDataItem == null)
                                                current = null;
                                            else
                                                current = selectedDataItem.innerInstance != null ? selectedDataItem.innerInstance() : itemBeingInserted;
                                        }

                                        return current;
                                    },
                                    set: (entity: $data.Entity) => {
                                        if (entity == null) {
                                            grid.clearSelection();
                                            dataSource.onCurrentChanged();
                                        }
                                        else {
                                            throw new Error("Not implemented");
                                        }
                                    }
                                });

                                dataSource.bind("error", dsError);

                            });

                            let editTemplateElement: JQuery = null;
                            let editTemplateHtmlString: string = null;
                            let editPopupTitle: string = null;

                            if (attributes["editTemplateId"] != null) {

                                editTemplateElement = angular.element(`#${attributes["editTemplateId"]}`);

                                let titleAttrValue = editTemplateElement.attr("title");

                                if (titleAttrValue != null)
                                    editPopupTitle = $interpolate(titleAttrValue)($scope);

                                const editTemplateHtml = angular.element(`<rad-grid-editor rad-model-item-template ng-model='::dataItem'>${editTemplateElement.html()}</rad-grid-editor>`);

                                editTemplateHtml.first().attr("isolatedOptionsKey", attributes["isolatedOptionsKey"]);

                                editTemplateHtmlString = editTemplateHtml.first()[0].outerHTML;

                                editTemplateHtml.remove();
                            }

                            const gridOptions: kendo.ui.GridOptions = {
                                dataSource: dataSource,
                                editable: attributes["editTemplateId"] == null ? { confirmation: true, update: false } : {
                                    mode: "popup",
                                    confirmation: true,
                                    template: kendo.template(editTemplateHtmlString, { useWithBlock: false }),
                                    window: {
                                        title: editPopupTitle || $translate.instant("GridEditPopupTitle"),
                                        width: editTemplateElement.width() || "auto",
                                        height: editTemplateElement.height() || "auto"
                                    }
                                },
                                edit: (e) => {
                                    angular.element(".k-edit-buttons").remove();
                                },
                                change(e) {
                                    dataSource.onCurrentChanged();
                                    ViewModel.ScopeManager.update$scope($scope);
                                },
                                autoBind: true,
                                cancel: async (e): Promise<void> => {
                                    if (e.model.isNew() == false && e.model.dirty == true && e.model.innerInstance != null) {
                                        const entity = e.model.innerInstance();
                                        entity.resetChanges();
                                        await entity.refresh();
                                    }
                                },
                                selectable: "row",
                                sortable: {
                                    mode: "multiple"
                                },
                                pageable: dataSource.options.pageSize != null ? {
                                    buttonCount: 3,
                                    input: true,
                                    pageSizes: [5, 10, 15, 25, 100],
                                    refresh: true
                                } : false,
                                scrollable: false,
                                resizable: true,
                                navigatable: true,
                                mobile: false,
                                filterable: true,
                                columnMenu: true,
                                groupable: false
                            };

                            if (attributes["toolbarTemplateId"] != null) {

                                const toolbarTemplateElement = angular.element(`#${attributes["toolbarTemplateId"]}`);

                                const toolbarTemplateHtml = toolbarTemplateElement.html();

                                const toolbar: any = kendo.template(toolbarTemplateHtml, { useWithBlock: false });

                                gridOptions.toolbar = toolbar;
                            }

                            if (attributes["detailTemplateId"] != null) {

                                const detailTemplateElement = angular.element(`#${attributes["detailTemplateId"]}`);

                                const detailTemplateHtml = angular.element(`<rad-grid-detail-template rad-model-item-template ng-model='::dataItem'>${detailTemplateElement.html()}</rad-grid-detail-template>`);

                                const detail: any = kendo.template(detailTemplateHtml.first()[0].outerHTML, { useWithBlock: false });

                                gridOptions.detailTemplate = detail;

                                detailTemplateHtml.remove();
                            }

                            const columns: Array<kendo.ui.GridColumn> = [];

                            const viewTemplateElement = angular.element(`#${attributes["viewTemplateId"]}`);

                            let extras = viewTemplateElement.find("extras");

                            if (extras.length == 0)
                                extras = angular.element("<extras></extras");

                            extras.attr("rad-model-item-template", "");
                            extras.attr("ng-model", "::dataItem");

                            let isFirstColumn = true;

                            viewTemplateElement.find("column")
                                .each((index, item) => {

                                    const wrappedItem = angular.element(item);

                                    if (isFirstColumn == true) {
                                        isFirstColumn = false;
                                        wrappedItem.append(extras);
                                    }

                                    const template = item.innerHTML;

                                    const gridColumn: kendo.ui.GridColumn = {
                                        field: wrappedItem.attr("name"),
                                        title: wrappedItem.attr("title"),
                                        width: wrappedItem.width() || "auto",
                                        template: template
                                    };

                                    gridColumn["element"] = wrappedItem;

                                    if (item.hasAttribute("command")) {
                                        columns.push(gridColumn);
                                        return;
                                    }

                                    if (wrappedItem.attr("name") == null)
                                        throw new Error("column must have a name attribute");

                                    const field = dataSource.options.schema.model.fields[gridColumn.field];

                                    if (field == null)
                                        throw new Error(`Model has no field named ${gridColumn.field} to be used`);

                                    if (field.type == "date") {

                                        const currentCulture = clientAppProfileManager.getClientAppProfile().culture;

                                        if (currentCulture == "FaIr") {

                                            gridColumn.filterable = {

                                                ui: (element: JQuery) => {

                                                    const val = element.val();

                                                    element.after('<input type="button" class="k-button" style="width:100%" />');

                                                    const datePickerButton = element.next();

                                                    const persianDatePickerOptions: PDatePickerOptions = {
                                                        position: ["0px", "0px"],
                                                        autoClose: field.viewType == "Date",
                                                        altField: element,
                                                        altFieldFormatter: (e) => {
                                                            const result = new Date(e);
                                                            return result;
                                                        },
                                                        formatter: (e) => {
                                                            const result = new Date(e);
                                                            if (field.dateType == "DateTime")
                                                                return dateTimeService.getFormattedDateTime(result);
                                                            else
                                                                return dateTimeService.getFormattedDate(result);
                                                        },
                                                        timePicker: {
                                                            enabled: field.dateType == "DateTime"
                                                        },
                                                        onShow: () => {

                                                            const thisPDatePickerElementToBePopupedUsingKendoPopup = angular.element(".datepicker-plot-area")
                                                                .filter((eId, el) => angular.element(el).is(":visible"));

                                                            const parentMenu = element.parents("div.k-column-menu").first();

                                                            const kendoPopupElement = thisPDatePickerElementToBePopupedUsingKendoPopup.kendoPopup({
                                                                anchor: parentMenu
                                                            });

                                                            const kendoPopup = kendoPopupElement.data("kendoPopup");

                                                            kendoPopup.open();

                                                            thisPDatePickerElementToBePopupedUsingKendoPopup.css("top", "-25px");

                                                            this["kendoPopuo"] = kendoPopup;
                                                        },
                                                        onHide: () => {
                                                            this["kendoPopuo"].destroy();
                                                        }
                                                    };

                                                    datePickerButton.pDatepicker(persianDatePickerOptions);

                                                    if (val == null || val == "")
                                                        datePickerButton.val(null);
                                                    else
                                                        datePickerButton.val(persianDatePickerOptions.formatter(val));

                                                    element.val(val);

                                                    element.hide();
                                                }
                                            }
                                        }
                                        else {
                                            if (field.viewType == "DateTime") {
                                                gridColumn.filterable = {
                                                    ui: (element: JQuery) => {
                                                        element.kendoDateTimePicker();
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    const filterDataSourceAttributeValue = wrappedItem.attr("filter-data-source");

                                    if (filterDataSourceAttributeValue != null) {

                                        const filterDataSource: kendo.data.DataSource = $parse(filterDataSourceAttributeValue)($scope);

                                        const filterTextFieldName = wrappedItem.attr("filter-text-field");
                                        let filterValueFieldName = wrappedItem.attr("filter-value-field");

                                        if (filterValueFieldName == null) {
                                            if (filterDataSource.options.schema != null && filterDataSource.options.schema.model != null && filterDataSource.options.schema.model.idField != null)
                                                filterValueFieldName = filterDataSource.options.schema.model.idField;
                                        }

                                        if (filterDataSource.options.schema.model.fields[filterTextFieldName] == null)
                                            throw new Error(`Model has no property named ${filterTextFieldName} to be used as text field`);

                                        if (filterDataSource.options.schema.model.fields[filterValueFieldName] == null)
                                            throw new Error(`Model has no property named ${filterValueFieldName} to be used as value field`);

                                        gridColumn.filterable = {
                                            ui: (element: JQuery) => {
                                                element.kendoComboBox({
                                                    autoBind: filterDataSource.flatView().length != 0,
                                                    open: (e) => {
                                                        if (e.sender.options.autoBind == false) {
                                                            e.sender.options.autoBind = true;
                                                            if (e.sender.options.dataSource.flatView().length == 0)
                                                                (e.sender.options.dataSource as kendo.data.DataSource).fetch();
                                                        }
                                                    },
                                                    valuePrimitive: true,
                                                    dataSource: filterDataSource,
                                                    dataTextField: filterTextFieldName,
                                                    dataValueField: filterValueFieldName || filterDataSource.options.schema.model.idField,
                                                    delay: 300,
                                                    ignoreCase: true,
                                                    minLength: 3,
                                                    placeholder: "...",
                                                    filter: "contains",
                                                    suggest: true,
                                                    highlightFirst: true
                                                });
                                            },
                                            ignoreCase: true
                                        }
                                    };

                                    columns.push(gridColumn);

                                });

                            gridOptions.columns = columns;

                            columns.forEach(col => {
                                DefaultRadGridDirective.defaultRadGridDirectiveColumnCustomizers.forEach(colCustomizer => {
                                    colCustomizer($scope, attributes, element, gridOptions, col["element"], col);
                                });
                            });

                            DefaultRadGridDirective.defaultRadGridDirectiveCustomizers.forEach(gridCustomizer => {
                                gridCustomizer($scope, attributes, element, gridOptions);
                            });

                            if (attributes.onInit != null) {
                                let onInitFN = $parse(attributes.onInit);
                                if (typeof onInitFN == "function") {
                                    onInitFN($scope, { gridOptions: gridOptions });
                                }
                            }

                            $scope[attributes["isolatedOptionsKey"]] = gridOptions;
                        });
                    });
                }
            });
        }
    }
}