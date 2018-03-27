

module Bit.Directives {

    @DirectiveDependency({
        name: "RadGrid",
        scope: true,
        bindToController: {

        },
        controllerAs: "radGrid",
        replace: true,
        terminal: true,
        restrict: "E",
        template: ($element: JQuery, $attrs: ng.IAttributes) => {

            const guidUtils = DependencyManager.getCurrent().resolveObject<Implementations.DefaultGuidUtils>("GuidUtils");

            const gridTemplate = `<rad-grid-element kendo-grid k-options="::radGrid.options" k-ng-delay="::radGrid.options" />`;

            const viewRowTemplateId = guidUtils.newGuid();

            const viewTemplate = angular.element($element)
                .children("view-template")
                .attr("id", viewRowTemplateId)
                .attr("ng-cloak", "");

            angular.element(document.body).append(viewTemplate);

            $attrs["viewTemplateId"] = viewRowTemplateId;

            const editTemplate = angular.element($element)
                .children("edit-template");

            if (editTemplate.length != 0) {

                const editRowTemplateId = guidUtils.newGuid();

                editTemplate
                    .attr("id", editRowTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(editTemplate);

                $attrs["editTemplateId"] = editRowTemplateId;
            }

            const toolbarTemplate = angular.element($element)
                .children("toolbar-template");

            if (toolbarTemplate.length != 0) {

                const toolbarTemplateId = guidUtils.newGuid();

                toolbarTemplate
                    .attr("id", toolbarTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(toolbarTemplate);

                $attrs["toolbarTemplateId"] = toolbarTemplateId;
            }

            const detailTemplate = angular.element($element)
                .children("detail-template");

            if (detailTemplate.length != 0) {

                const detailTemplateId = guidUtils.newGuid();

                detailTemplate
                    .attr("id", detailTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(detailTemplate);

                $attrs["detailTemplateId"] = detailTemplateId;
            }

            return gridTemplate;
        }
    })
    export class DefaultRadGridDirective {

        public static defaultRadGridDirectiveCustomizers: Array<($scope: ng.IScope, $attrs: ng.IAttributes, $element: JQuery, gridOptions: kendo.ui.GridOptions) => void> = [];
        public static defaultRadGridDirectiveColumnCustomizers: Array<($scope: ng.IScope, $attrs: ng.IAttributes, $element: JQuery, gridOptions: kendo.ui.GridOptions, columnElement: JQuery, gridColumn: kendo.ui.GridColumn) => void> = [];

        public constructor( @Inject("$element") public $element: JQuery,
            @Inject("$scope") public $scope: ng.IScope,
            @Inject("$attrs") public $attrs: ng.IAttributes & { radDataSource: string, radOnInit: string },
            @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider,
            @Inject("$translate") public $translate: ng.translate.ITranslateService,
            @Inject("$parse") public $parse: ng.IParseService,
            @Inject("$interpolate") public $interpolate: ng.IInterpolateService,
            @Inject("DateTimeService") public dateTimeService: Contracts.IDateTimeService) {

        }

        public dataSource: kendo.data.DataSource;
        public options: kendo.ui.GridOptions;
        public radOnInit: (args: { gridOptions: kendo.ui.GridOptions }) => void;

        public get grid(): kendo.ui.Grid {
            return this.$element.data("kendoGrid");
        }

        @Command()
        public async $onInit(): Promise<void> {

            this.$scope.$on("kendoWidgetCreated", this.onWidgetCreated.bind(this));

            let ngModelAndDataSourceWatchDisposal = this.$scope.$watch(this.$attrs.radDataSource, (ds: kendo.data.DataSource) => {

                if (ds == null)
                    return;

                this.dataSource = ds;

                ngModelAndDataSourceWatchDisposal();
                this.onWidgetInitialDataProvided();

            });
        }

        @Command()
        public async onWidgetInitialDataProvided(): Promise<void> {

            this.dataSource.bind("error", this.onDataSourceError.bind(this));

            let editTemplateElement: JQuery = null;
            let editTemplateHtmlString: string = null;
            let editPopupTitle: string = null;

            if (this.$attrs["editTemplateId"] != null) {

                editTemplateElement = angular.element(`#${this.$attrs["editTemplateId"]}`);

                let titleAttrValue = editTemplateElement.attr("title");

                if (titleAttrValue != null)
                    editPopupTitle = this.$interpolate(titleAttrValue)(this.$scope);

                const editTemplateHtml = angular.element(`<rad-grid-editor rad-model-item-template ng-model='::dataItem'>${editTemplateElement.html()}</rad-grid-editor>`);

                editTemplateHtml.first().attr("isolatedOptionsKey", this.$attrs["isolatedOptionsKey"]);

                editTemplateHtmlString = editTemplateHtml.first()[0].outerHTML;

                editTemplateHtml.remove();
            }

            const gridOptions: kendo.ui.GridOptions = {
                dataSource: this.dataSource,
                editable: this.$attrs["editTemplateId"] == null ? { confirmation: true, update: false } : {
                    mode: "popup",
                    confirmation: true,
                    template: kendo.template(editTemplateHtmlString, { useWithBlock: false }),
                    window: {
                        title: editPopupTitle || this.$translate.instant("GridEditPopupTitle"),
                        width: editTemplateElement.width() || "auto",
                        height: editTemplateElement.height() || "auto"
                    }
                },
                edit: (e) => {
                    e.container.data("radGridController", this);
                    angular.element(".k-edit-buttons").remove();
                },
                change: (e) => {
                    setTimeout(() => {
                        this.dataSource.current = this.getCurrent();
                        ScopeManager.update$scope(this.$scope);
                    }, 0);
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
                pageable: this.dataSource.options.pageSize != null ? {
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
                groupable: false,
                dataBound: (e) => {
                    this.syncCurrent();
                }
            };

            if (this.$attrs["toolbarTemplateId"] != null) {

                const toolbarTemplateElement = angular.element(`#${this.$attrs["toolbarTemplateId"]}`);

                const toolbarTemplateHtml = toolbarTemplateElement.html();

                const toolbar: any = kendo.template(toolbarTemplateHtml, { useWithBlock: false });

                gridOptions.toolbar = toolbar;
            }

            if (this.$attrs["detailTemplateId"] != null) {

                const detailTemplateElement = angular.element(`#${this.$attrs["detailTemplateId"]}`);

                const detailTemplateHtml = angular.element(`<rad-grid-detail-template rad-model-item-template ng-model='::dataItem'>${detailTemplateElement.html()}</rad-grid-detail-template>`);

                const detail: any = kendo.template(detailTemplateHtml.first()[0].outerHTML, { useWithBlock: false });

                gridOptions.detailTemplate = detail;

                detailTemplateHtml.remove();
            }

            let gridDtoType = this.dataSource.options.schema['jayType'];

            let metadata = this.metadataProvider.getMetadataSync();

            const columns: Array<kendo.ui.GridColumn> = [];

            const viewTemplateElement = angular.element(`#${this.$attrs["viewTemplateId"]}`);

            let extras = viewTemplateElement.find("extras");

            if (extras.length == 0)
                extras = angular.element("<extras></extras");

            extras.attr("rad-model-item-template", "");
            extras.attr("ng-model", "::dataItem");

            let isFirstDataColumn = true;

            const currentCulture = ClientAppProfileManager.getCurrent().getClientAppProfile().culture;

            viewTemplateElement.find("column")
                .each((index, item) => {

                    const wrappedItem = angular.element(item);
                    let template = item.innerHTML;

                    if (wrappedItem.attr("name") != null && isFirstDataColumn == true) {
                        isFirstDataColumn = false;
                        if (template == null || template == "")
                            wrappedItem[0].innerHTML = `{{::dataItem.${wrappedItem.attr("name")}}}`;
                        wrappedItem.append(extras);
                        template = item.innerHTML;
                    }

                    const gridColumn: kendo.ui.GridColumn = {
                        field: wrappedItem.attr("name"),
                        title: wrappedItem.attr("title"),
                        width: wrappedItem.width() || "auto"
                    };

                    if (template != null && template != "")
                        gridColumn.template = template;

                    gridColumn["element"] = wrappedItem;

                    if (item.hasAttribute("command")) {
                        columns.push(gridColumn);
                        return;
                    }

                    if (wrappedItem.attr("name") == null)
                        throw new Error("column must have a name attribute");

                    const fieldInfo = this.dataSource.options.schema.model.fields[gridColumn.field];

                    if (fieldInfo == null)
                        throw new Error(`Model has no field named ${gridColumn.field} to be used`);

                    if (fieldInfo.type == "date") {

                        if (currentCulture == "FaIr") {

                            if (jQuery["persian-date-picker-val-override-is-configured"] != true) {

                                let jQueryOriginalVal = jQuery.fn.val;

                                jQuery.fn.val = (function value(value) {
                                    let result = jQueryOriginalVal.apply(this, arguments);
                                    if (arguments.length == 1 && this.hasClass("persian-date-picker-value")) {
                                        this.trigger("change");
                                    }
                                    return result;
                                }) as any;

                                jQuery["persian-date-picker-val-override-is-configured"] = true;

                            }

                            gridColumn.filterable = {

                                ui: (element: JQuery) => {

                                    const val = element.val();

                                    element.after('<input type="button" class="k-button" style="width:100%" />');

                                    element.addClass("persian-date-picker-value");

                                    const datePickerButton = element.next();

                                    const persianDatePickerOptions = {
                                        autoClose: fieldInfo.viewType == "Date",
                                        altField: element,
                                        altFieldFormatter: (e) => {
                                            const result = new Date(e);
                                            return result;
                                        },
                                        formatter: (e) => {
                                            const result = new Date(e);
                                            if (fieldInfo.dateType == "DateTime")
                                                return this.dateTimeService.getFormattedDateTime(result);
                                            else
                                                return this.dateTimeService.getFormattedDate(result);
                                        },
                                        timePicker: {
                                            enabled: fieldInfo.dateType == "DateTime"
                                        }
                                    };

                                    let datePickerInstance = datePickerButton["persianDatepicker"](persianDatePickerOptions);

                                    if (val == null || val == "")
                                        datePickerButton.val(null);
                                    else
                                        datePickerButton.val(persianDatePickerOptions.formatter(val));

                                    element.val(val);

                                    element.hide();

                                    element.parents("div.k-filterable.k-content").data("kendoFilterMenu")["popup"].bind("close", function onClose(e) {
                                        if (datePickerInstance.model.view.$container.css("display") == "block") {
                                            e.preventDefault();
                                        }
                                    });
                                }
                            }
                        }
                        else {
                            if (fieldInfo.viewType == "DateTime") {
                                gridColumn.filterable = {
                                    ui: (element: JQuery) => {
                                        element.kendoDateTimePicker();
                                    }
                                }
                            }
                        }
                    }

                    const filterDataSourceAttributeValue = wrappedItem.attr("rad-data-source");

                    if (filterDataSourceAttributeValue != null) {

                        const filterDataSource: kendo.data.DataSource = this.$parse(filterDataSourceAttributeValue)(this.$scope);

                        if (filterDataSource == null)
                            throw new Error(`data source for ${filterDataSourceAttributeValue} is null`);

                        let filterTextFieldName = wrappedItem.attr("rad-text-field-name");
                        let filterValueFieldName = wrappedItem.attr("rad-value-field-name");
                        let bindedMemberName = fieldInfo.field;

                        let dtoMetadata = metadata.Dtos.find(d => d.DtoType == gridDtoType['fullName']);
                        if (dtoMetadata != null) {
                            let thisDSMemberType = filterDataSource.options.schema['jayType'];
                            if (thisDSMemberType != null) {
                                let lookup = dtoMetadata.MembersLookups.find(l => l.DtoMemberName == bindedMemberName && l.LookupDtoType == thisDSMemberType['fullName']);
                                if (lookup != null) {
                                    if (lookup.BaseFilter_JS != null) {
                                        let originalRead = filterDataSource['transport'].read;
                                        filterDataSource['transport'].read = function read(options) {
                                            options.data = options.data || {};
                                            options.data.lookupBaseFilter = lookup.BaseFilter_JS;
                                            return originalRead.apply(this, arguments);
                                        }
                                    }
                                    if (filterTextFieldName == null)
                                        filterTextFieldName = lookup.DataTextField;
                                    if (filterValueFieldName == null)
                                        filterValueFieldName = lookup.DataValueField;
                                }
                            }
                        }

                        if (filterValueFieldName == null) {
                            if (filterDataSource.options.schema != null && filterDataSource.options.schema.model != null && filterDataSource.options.schema.model.idField != null)
                                filterValueFieldName = filterDataSource.options.schema.model.idField;
                        }

                        if (filterDataSource.options.schema.model.fields[filterTextFieldName] == null)
                            throw new Error(`Model has no property named ${filterTextFieldName} to be used as text field`);

                        if (filterDataSource.options.schema.model.fields[filterValueFieldName] == null)
                            throw new Error(`Model has no property named ${filterValueFieldName} to be used as value field`);

                        let comboBoxOptions: kendo.ui.ComboBoxOptions = {
                            dataSource: filterDataSource,
                            autoBind: filterDataSource.flatView().length != 0,
                            dataTextField: filterTextFieldName,
                            dataValueField: filterValueFieldName,
                            filter: "contains",
                            minLength: 3,
                            valuePrimitive: true,
                            ignoreCase: true,
                            suggest: true,
                            highlightFirst: true,
                            open: (e) => {
                                if (e.sender.options.autoBind == false) {
                                    e.sender.options.autoBind = true;
                                    if (e.sender.options.dataSource.flatView().length == 0)
                                        (e.sender.options.dataSource as kendo.data.DataSource).fetch();
                                }
                            },
                            delay: 300,
                            placeholder: "..."
                        };

                        gridColumn.filterable = {
                            ui: (element: JQuery) => {
                                element.kendoComboBox(comboBoxOptions);
                            },
                            ignoreCase: true
                        }
                    };

                    columns.push(gridColumn);

                });

            gridOptions.columns = columns;

            columns.forEach(col => {
                DefaultRadGridDirective.defaultRadGridDirectiveColumnCustomizers.forEach(colCustomizer => {
                    colCustomizer(this.$scope, this.$attrs, this.$element, gridOptions, col["element"], col);
                });
            });

            DefaultRadGridDirective.defaultRadGridDirectiveCustomizers.forEach(gridCustomizer => {
                gridCustomizer(this.$scope, this.$attrs, this.$element, gridOptions);
            });

            if (this.$attrs.radOnInit != null) {
                this.radOnInit({ gridOptions: gridOptions });
            }

            this.options = gridOptions;

        }

        @Command()
        public onWidgetCreated() {

            let grid = this.grid;

            grid.wrapper.find(".k-header").data("radGridController", this);

            this.dataSource["setHandlers"] = this.dataSource["setHandlers"] || [];

            this.syncCurrent();

            this.dataSource["setHandlers"].push(this.setCurrent.bind(this));
        }

        public syncCurrent() {
            if (this.dataSource.current == null && this.getCurrent() != null)
                this.dataSource["_current"] = this.getCurrent();
            if (this.dataSource.current != null && this.getCurrent() == null)
                this.setCurrent(this.dataSource.current as $data.Entity);
        }

        public setCurrent(entity: $data.Entity) {

            let grid = this.grid;

            if (entity == null) {
                grid.clearSelection();
            }
            else {
                let _current = this.getCurrent();
                if (_current == null || _current.uid != entity.uid)
                    grid.select(grid.tbody.find(`tr[data-uid='${entity.uid}']`));
            }

        }

        public getCurrent() {

            let grid = this.grid;

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
        }

        public onDataSourceError() {
            if (this.dataSource["destroyed"]().length != 0) {
                this.dataSource.cancelChanges();
            }
        }

        public deleteDataItem($event) {

            const row = angular.element($event.currentTarget).parents("tr");

            this.grid.removeRow(row);

            if (this.grid.options.editable != null && (this.grid.options.editable == false || (this.grid.options.editable as kendo.ui.GridEditable).update == false)) {
                this.grid.dataSource.sync();
            }
        }

        public addDataItem() {
            this.grid.addRow();
        }

        public updateDataItem($event) {

            const row = angular.element($event.currentTarget).parents("tr");

            this.grid.editRow(row);

        }

        public cancelDataItemChange($event) {
            const uid = angular.element($event.target).parents(".k-popup-edit-form").attr("data-uid");
            this.grid.trigger("cancel", { container: angular.element($event.target).parents(".k-window"), sender: this.grid, model: this.grid.dataSource.flatView().find(i => i["uid"] == uid) });
            this.grid.cancelRow();
        }

        public saveDataItemChange($event) {
            this.grid.saveRow();
        }

        public $onDestroy() {
            if (this.dataSource != null) {
                this.dataSource.unbind("error", this.onDataSourceError);
                this.dataSource["setHandlers"].splice(this.dataSource["setHandlers"].indexOf(this.setCurrent), 1);
            }
            if (this.grid != null) {
                kendo.destroyWidget(this.grid);
            }
        }
    }
}