/// <reference path="../../foundation.viewmodel.htmlclient/foundation.viewmodel.d.ts" />

module Foundation.View.Directives {

    @Foundation.Core.DirectiveDependency({ name: 'radGrid' })
    export class DefaultRadGridDirective implements Foundation.ViewModel.Contracts.IDirective {

        public static scrollable = false;
        public static resizable = true;
        public static reorderable = true;
        public static navigatable = true;
        public static mobile = false;
        public static filterable = true;
        public static columnMenu = true;
        public static pageable = true;
        public static groupable = false;

        public getDirectiveFactory(): angular.IDirectiveFactory {
            return () => ({
                scope: false,
                replace: true,
                terminal: true,
                template: (element: JQuery, attrs: ng.IAttributes) => {

                    let guidUtils = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Implementations.GuidUtils>("GuidUtils");

                    let replaceAll = (text: string, search: string, replacement: string) => {
                        return text.replace(new RegExp(search, 'g'), replacement);
                    };

                    let isolatedOptionsKey = 'options' + replaceAll(guidUtils.newGuid(), '-', '');

                    attrs['isolatedOptionsKey'] = isolatedOptionsKey;

                    const gridTemplate = `<fake-element kendo-grid k-options="::${isolatedOptionsKey}" k-ng-delay="::${isolatedOptionsKey}" />`;

                    let editRowTemplateId = guidUtils.newGuid();

                    angular.element(element)
                        .children("[type='edit/template']")
                        .attr('id', editRowTemplateId)
                        .insertAfter(element);

                    attrs['editTemplateId'] = editRowTemplateId;

                    let viewRowTemplateId = guidUtils.newGuid();

                    angular.element(element)
                        .children("[type='view/template']")
                        .attr('id', viewRowTemplateId)
                        .insertAfter(element);

                    attrs['viewTemplateId'] = viewRowTemplateId;

                    let toolbarTemplate = angular.element(element)
                        .children("[type='toolbar/template']");

                    if (toolbarTemplate.length != 0) {

                        let toolbarTemplateId = guidUtils.newGuid();

                        toolbarTemplate.attr('id', toolbarTemplateId)
                            .insertAfter(element);

                        attrs['toolbarTemplateId'] = toolbarTemplateId;
                    }

                    return gridTemplate;
                },
                link($scope: angular.IScope, element: JQuery, attributes: any) {

                    let dependencyManager = Core.DependencyManager.getCurrent();

                    let $timeout = dependencyManager.resolveObject<angular.ITimeoutService>("$timeout");

                    let $translate = dependencyManager.resolveObject<angular.translate.ITranslateService>("$translate");

                    let $compile = dependencyManager.resolveObject<angular.ICompileService>("$compile");

                    let $parse = dependencyManager.resolveObject<angular.IParseService>("$parse");

                    let dateTimeService = Foundation.Core.DependencyManager.getCurrent().resolveObject<Foundation.ViewModel.Contracts.IDateTimeService>("DateTimeService");

                    let clientAppProfileManager = dependencyManager.resolveObject<Foundation.Core.ClientAppProfileManager>("ClientAppProfileManager");

                    $timeout(() => {

                        let watchForDatasourceToCreateDataGridWidgetUnRegisterHandler = $scope.$watch(attributes.radDatasource, (datasource: kendo.data.DataSource) => {

                            if (datasource == null)
                                return;

                            watchForDatasourceToCreateDataGridWidgetUnRegisterHandler();

                            let kendoWidgetCreatedDisposal = $scope.$on("kendoWidgetCreated", (event, grid: kendo.ui.Grid) => {

                                if (grid.element[0] != element[0]) {
                                    return;
                                }

                                kendoWidgetCreatedDisposal();

                                $scope[attributes['isolatedOptionsKey'] + "Add"] = () => {
                                    grid.addRow();
                                };

                                $scope[attributes['isolatedOptionsKey'] + "Delete"] = ($event) => {

                                    let row = angular.element($event.currentTarget).parents('tr');

                                    grid.removeRow(row);

                                };

                                $scope[attributes['isolatedOptionsKey'] + "Update"] = ($event) => {

                                    let row = angular.element($event.currentTarget).parents('tr');

                                    grid.editRow(row);

                                };

                                $scope[attributes['isolatedOptionsKey'] + "Cancel"] = ($event) => {
                                    let uid = angular.element($event.target).parents('.k-popup-edit-form').attr('data-uid');
                                    grid.trigger('cancel', { container: angular.element($event.target).parents('.k-window'), sender: grid, model: grid.dataSource.flatView().find(i => i['uid'] == uid) });
                                    grid.cancelRow();
                                };

                                $scope[attributes['isolatedOptionsKey'] + "Save"] = ($event) => {

                                    grid.saveRow();

                                };

                                Object.defineProperty(datasource, "current", {
                                    configurable: true,
                                    enumerable: false,
                                    get: () => {

                                        let current = null;

                                        let itemBeingInserted = grid.dataSource
                                            .flatView().find(i => i['isNew']() == true);

                                        if (itemBeingInserted != null)
                                            current = itemBeingInserted.innerInstance != null ? itemBeingInserted.innerInstance() : itemBeingInserted;

                                        if (current == null) {

                                            let selectedDataItem = grid.dataItem(grid.select());

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

                                datasource.bind('error', function (e) {
                                    if (datasource['destroyed']().length != 0) {
                                        datasource.cancelChanges();
                                    }
                                });
                            });

                            let viewTemplateElement = angular.element("#" + attributes['viewTemplateId']);

                            let viewTemplateHtml = viewTemplateElement.html();

                            viewTemplateHtml = viewTemplateHtml.replace("<tr", `<tr data-uid='#: uid #' rad-grid-row ng-model='::dataItem' isolatedOptionsKey='${attributes['isolatedOptionsKey']}'`)

                            viewTemplateElement.remove();

                            let editTemplateElement = angular.element("#" + attributes['editTemplateId']);

                            let editPopupTitle = editTemplateElement.attr('title');

                            let editTemplateHtml = angular.element(`<fake>${editTemplateElement.html()}</fake>`);

                            editTemplateHtml.first().attr('isolatedOptionsKey', attributes['isolatedOptionsKey']);

                            let editTemplateHtmlString = editTemplateHtml.first()[0].outerHTML;

                            editTemplateElement.remove();

                            editTemplateHtml.remove();

                            let gridOptions: kendo.ui.GridOptions = {
                                dataSource: datasource,
                                editable: {
                                    mode: 'popup',
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
                                    Foundation.ViewModel.ScopeManager.update$scope($scope);
                                },
                                autoBind: true,
                                cancel: async (e): Promise<void> => {
                                    if (e.model.isNew() == false && e.model.dirty == true && e.model.innerInstance != null) {
                                        let entity = e.model.innerInstance();
                                        entity.resetChanges();
                                        await entity.refresh();
                                    }
                                },
                                rowTemplate: kendo.template(viewTemplateHtml),
                                selectable: 'row',
                                sortable: {
                                    mode: 'multiple'
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

                            if (attributes['toolbarTemplateId'] != null) {

                                let toolbarTemplateElement = angular.element("#" + attributes['toolbarTemplateId']);

                                let toolbarTemplateHtml = toolbarTemplateElement.html();

                                toolbarTemplateElement.remove();

                                let toolbar: any = kendo.template(toolbarTemplateHtml);

                                gridOptions.toolbar = toolbar;
                            }

                            let compiledViewTemplate = angular.element(viewTemplateHtml);

                            angular.element(element)
                                .after($compile(compiledViewTemplate)($scope));

                            let columns: Array<kendo.ui.GridColumn> = [];

                            compiledViewTemplate.find('td')
                                .each((index, item) => {

                                    let wrappedItem = angular.element(item);

                                    if (wrappedItem.attr('name') != null) {

                                        let gridColumn: kendo.ui.GridColumn = {
                                            field: wrappedItem.attr('name'),
                                            title: wrappedItem.attr('title'),
                                            width: wrappedItem.attr('width') || 'auto'
                                        };

                                        if (DefaultRadGridDirective.filterable == true) {

                                            let field = datasource.options.schema.model.fields[gridColumn.field];

                                            if (field.type == "date") {

                                                let currentCulture = clientAppProfileManager.getClientAppProfile().culture;

                                                if (currentCulture == 'FaIr') {

                                                    gridColumn.filterable = {

                                                        ui: (element: JQuery) => {

                                                            element.after('<input type="button" class="k-button" style="width:100%" />');

                                                            let datePickerButton = element.next();

                                                            datePickerButton.val(element.val());

                                                            datePickerButton.pDatepicker({
                                                                position: ['0px', '0px'],
                                                                autoClose: field.viewType == "Date",
                                                                altField: element,
                                                                altFieldFormatter: (e) => {
                                                                    let result = new Date(e);
                                                                    return result;
                                                                },
                                                                formatter: (e) => {
                                                                    let result = new Date(e);
                                                                    if (field.dateType == "DateTime")
                                                                        return dateTimeService.getFormattedDateTime(result);
                                                                    else
                                                                        return dateTimeService.getFormattedDate(result);
                                                                },
                                                                timePicker: {
                                                                    enabled: field.dateType == "DateTime"
                                                                },
                                                                onShow: () => {

                                                                    let thisPDatePickerElementToBePopupedUsingKendoPopup = angular.element('.datepicker-plot-area')
                                                                        .filter((eId, el) => angular.element(el).is(":visible"));

                                                                    let parentMenu = element.parents('div.k-column-menu').first();

                                                                    let kendoPopupElement = thisPDatePickerElementToBePopupedUsingKendoPopup.kendoPopup({
                                                                        anchor: parentMenu
                                                                    });

                                                                    let kendoPopup = kendoPopupElement.data('kendoPopup');

                                                                    kendoPopup.open();

                                                                    thisPDatePickerElementToBePopupedUsingKendoPopup.css('top', '-25px');

                                                                    this['kendoPopuo'] = kendoPopup;
                                                                },
                                                                onHide: () => {
                                                                    this['kendoPopuo'].destroy();
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

                                            let filterDataSourceAttributeValue = wrappedItem.attr('filter-data-source');

                                            if (filterDataSourceAttributeValue != null) {

                                                let filterDataSource: kendo.data.DataSource = $parse(filterDataSourceAttributeValue)($scope);

                                                let filterTextFieldName = wrappedItem.attr('filter-text-field');
                                                let filterValueFieldName = wrappedItem.attr('filter-value-field');

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
                                                            placeholder: '...',
                                                            filter: 'contains',
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

                            $scope[attributes['isolatedOptionsKey']] = gridOptions;
                        });
                    });
                }
            });
        }
    }
}