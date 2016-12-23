/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Core.DirectiveDependency({ name: "radGrid" })
    export class DefaultRadGridDirective implements ViewModel.Contracts.IDirective {

        public static scrollable = false;
        public static resizable = true;
        public static reorderable = true;
        public static navigatable = true;
        public static mobile = false;
        public static filterable = true;
        public static columnMenu = true;
        public static pageable = true;
        public static groupable = false;

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

                    const isolatedOptionsKey = "options" + replaceAll(guidUtils.newGuid(), "-", "");

                    attrs["isolatedOptionsKey"] = isolatedOptionsKey;

                    const gridTemplate = `<fake-element kendo-grid k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}" />`;

                    const editRowTemplateId = guidUtils.newGuid();

                    angular.element(element)
                        .children("[type='edit/template']")
                        .attr("id", editRowTemplateId)
                        .insertAfter(element);

                    attrs["editTemplateId"] = editRowTemplateId;

                    const viewRowTemplateId = guidUtils.newGuid();

                    angular.element(element)
                        .children("[type='view/template']")
                        .attr("id", viewRowTemplateId)
                        .insertAfter(element);

                    attrs["viewTemplateId"] = viewRowTemplateId;

                    const toolbarTemplate = angular.element(element)
                        .children("[type='toolbar/template']");

                    if (toolbarTemplate.length != 0) {

                        const toolbarTemplateId = guidUtils.newGuid();

                        toolbarTemplate.attr("id", toolbarTemplateId)
                            .insertAfter(element);

                        attrs["toolbarTemplateId"] = toolbarTemplateId;
                    }

                    return gridTemplate;
                },
                link($scope: ng.IScope, element: JQuery, attributes: any) {

                    const dependencyManager = Core.DependencyManager.getCurrent();

                    const $timeout = dependencyManager.resolveObject<ng.ITimeoutService>("$timeout");

                    const $translate = dependencyManager.resolveObject<ng.translate.ITranslateService>("$translate");

                    const $compile = dependencyManager.resolveObject<ng.ICompileService>("$compile");

                    const $parse = dependencyManager.resolveObject<ng.IParseService>("$parse");

                    const dateTimeService = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Contracts.IDateTimeService>("DateTimeService");

                    const clientAppProfileManager = dependencyManager.resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager");

                    $timeout(() => {

                        const watchForDatasourceToCreateDataGridWidgetUnRegisterHandler = $scope.$watch(attributes.radDatasource, (datasource: kendo.data.DataSource) => {

                            if (datasource == null)
                                return;

                            watchForDatasourceToCreateDataGridWidgetUnRegisterHandler();

                            const kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, grid: kendo.ui.Grid) => {

                                if (grid.element[0] != element[0]) {
                                    return;
                                }

                                kendoWidgetCreatedDisposal();

                                $scope[attributes["isolatedOptionsKey"] + "Add"] = () => {
                                    grid.addRow();
                                };

                                $scope[attributes["isolatedOptionsKey"] + "Delete"] = ($event) => {

                                    const row = angular.element($event.currentTarget).parents("tr");

                                    grid.removeRow(row);

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

                                Object.defineProperty(datasource, "current", {
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
                                            datasource.onCurrentChanged();
                                        }
                                        else {
                                            throw new Error("Not implemented");
                                        }
                                    }
                                });

                                datasource.bind("error", function (e) {
                                    if (datasource["destroyed"]().length != 0) {
                                        datasource.cancelChanges();
                                    }
                                });
                            });

                            const viewTemplateElement = angular.element("#" + attributes["viewTemplateId"]);

                            let viewTemplateHtml = viewTemplateElement.html();

                            viewTemplateHtml = viewTemplateHtml
                                .replace("<tr",
                                    `<tr data-uid='#: uid #' rad-grid-row ng-model='::dataItem' isolatedOptionsKey='${
                                    attributes["isolatedOptionsKey"]}'`);

                            viewTemplateElement.remove();

                            const editTemplateElement = angular.element("#" + attributes["editTemplateId"]);

                            const editPopupTitle = editTemplateElement.attr("title");

                            const editTemplateHtml = angular.element(`<fake>${editTemplateElement.html()}</fake>`);

                            editTemplateHtml.first().attr("isolatedOptionsKey", attributes["isolatedOptionsKey"]);

                            const editTemplateHtmlString = editTemplateHtml.first()[0].outerHTML;

                            editTemplateElement.remove();

                            editTemplateHtml.remove();

                            const gridOptions: kendo.ui.GridOptions = {
                                dataSource: datasource,
                                editable: {
                                    mode: "popup",
                                    confirmation: true,
                                    template: kendo.template(editTemplateHtmlString),
                                    window: {
                                        title: editPopupTitle || $translate.instant("GridEditPopupTitle")
                                    }
                                },
                                edit: (e) => {
                                    angular.element(".k-edit-buttons").remove();
                                },
                                change: function onChange(e) {
                                    datasource.onCurrentChanged();
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
                                rowTemplate: kendo.template(viewTemplateHtml),
                                selectable: "row",
                                sortable: {
                                    mode: "multiple"
                                },
                                scrollable: DefaultRadGridDirective.scrollable,
                                resizable: DefaultRadGridDirective.resizable,
                                reorderable: DefaultRadGridDirective.reorderable,
                                navigatable: DefaultRadGridDirective.navigatable,
                                mobile: DefaultRadGridDirective.mobile,
                                filterable: DefaultRadGridDirective.filterable,
                                columnMenu: DefaultRadGridDirective.columnMenu,
                                pageable: DefaultRadGridDirective.pageable,
                                groupable: attributes.groupable == true || DefaultRadGridDirective.groupable
                            };

                            if (attributes["toolbarTemplateId"] != null) {

                                const toolbarTemplateElement = angular.element("#" + attributes["toolbarTemplateId"]);

                                const toolbarTemplateHtml = toolbarTemplateElement.html();

                                toolbarTemplateElement.remove();

                                const toolbar: any = kendo.template(toolbarTemplateHtml);

                                gridOptions.toolbar = toolbar;
                            }

                            const compiledViewTemplate = angular.element(viewTemplateHtml);

                            angular.element(element)
                                .after($compile(compiledViewTemplate)($scope));

                            const columns: Array<kendo.ui.GridColumn> = [];

                            compiledViewTemplate.find("td")
                                .each((index, item) => {

                                    const wrappedItem = angular.element(item);

                                    if (wrappedItem.attr("name") != null) {

                                        const gridColumn: kendo.ui.GridColumn = {
                                            field: wrappedItem.attr("name"),
                                            title: wrappedItem.attr("title"),
                                            width: wrappedItem.attr("width") || "auto"
                                        };

                                        if (DefaultRadGridDirective.filterable == true) {

                                            const field = datasource.options.schema.model.fields[gridColumn.field];

                                            if (field == null)
                                                throw new Error(`Model has no field named ${gridColumn.field} to be used`);

                                            if (field.type == "date") {

                                                const currentCulture = clientAppProfileManager.getClientAppProfile().culture;

                                                if (currentCulture == "FaIr") {

                                                    gridColumn.filterable = {

                                                        ui: (element: JQuery) => {

                                                            element.after('<input type="button" class="k-button" style="width:100%" />');

                                                            const datePickerButton = element.next();

                                                            datePickerButton.val(element.val());

                                                            datePickerButton.pDatepicker({
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
                                                            });

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
                                                const filterValueFieldName = wrappedItem.attr("filter-value-field");

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

                                        }

                                        columns.push(gridColumn);
                                    }

                                });

                            compiledViewTemplate.remove();

                            gridOptions.columns = columns;

                            $scope[attributes["isolatedOptionsKey"]] = gridOptions;
                        });
                    });
                }
            });
        }
    }
}