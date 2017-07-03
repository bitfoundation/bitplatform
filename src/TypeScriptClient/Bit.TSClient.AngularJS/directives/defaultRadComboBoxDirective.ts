

module Bit.Directives {
    @DirectiveDependency({
        name: "RadComboBox",
        scope: true,
        bindToController: {
            ngModelValue: "=ngModel",
            radTextValue: "=radText",
            radVirtualEntityLoader: "&",
            radOnInit: "&"
        },
        controllerAs: "radComboBox",
        template: ($element: JQuery, $attrs: ng.IAttributes & { ngModelOptions: string }) => {

            const itemTemplate = $element
                .children("item-template");

            const guidUtils = DependencyManager.getCurrent().resolveObject<Implementations.DefaultGuidUtils>("GuidUtils");

            if (itemTemplate.length != 0) {

                const itemTemplateId = guidUtils.newGuid();

                angular.element(document.body).append(itemTemplate.attr("id", itemTemplateId).attr("ng-cloak", ""));

                $attrs["itemTemplateId"] = itemTemplateId;
            }

            const headerTemplate = angular.element($element)
                .children("header-template");

            if (headerTemplate.length != 0) {

                const headerTemplateId = guidUtils.newGuid();

                headerTemplate
                    .attr("id", headerTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(headerTemplate);

                $attrs["headerTemplateId"] = headerTemplateId;
            }

            let ngModelOptions = "";
            if ($attrs.ngModelOptions == null) {
                ngModelOptions = `ng-model-options="{ updateOn : 'change' , allowInvalid : true }"`;
            }

            const template = `<input ${ngModelOptions} kendo-combo-box k-options="radComboBox.options" k-ng-delay="::radComboBox.options"></input>`;

            return template;
        },
        replace: true,
        terminal: true,
        require: {
            mdInputContainer: "^?mdInputContainer",
            ngModel: "ngModel"
        },
        restrict: "E"
    })
    export class DefaultRadComboBoxDirective {

        public static defaultRadComboBoxDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, $element: JQuery, comboBoxOptions: kendo.ui.ComboBoxOptions) => void> = [];

        public constructor( @Inject("$element") public $element: JQuery,
            @Inject("$scope") public $scope: ng.IScope,
            @Inject("$attrs") public $attrs: ng.IAttributes & { ngModel: string, radText: string, radDataSource: string, radValueFieldName: string, radTextFieldName: string, radVirtualEntityLoader: string, radOnInit: string },
            @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider) {

        }

        private radValueFieldName: string;
        private radTextFieldName: string;
        public ngModelValue: any;
        public radTextValue: any;
        public bindedMemberName: string; // in vm.customer.FirstName >> This will return "FirstName"
        public parentOfNgModel: Model.Contracts.IDto; // in vm.customer.FirstName >> This will return customer instance
        public dataSource: kendo.data.DataSource;
        private originalDataSourceTransportRead: any;
        public ngModel: ng.INgModelController;
        public mdInputContainer: { element: JQuery };
        public mdInputContainerParent: JQuery;
        public options: kendo.ui.ComboBoxOptions;
        public radVirtualEntityLoader: (args: { id: any }) => Promise<Model.Contracts.IDto>;
        public radOnInit: (args: { comboBoxOptions: kendo.ui.ComboBoxOptions }) => void;

        public get comboBox(): kendo.ui.ComboBox {
            return this.$element.data("kendoComboBox");
        }

        @Command()
        public async $onInit(): Promise<void> {

            this.$scope.$on("kendoWidgetCreated", this.onWidgetCreated.bind(this));

            const modelParts = this.$attrs.ngModel.split(".");
            this.bindedMemberName = modelParts.pop();
            const ngModelDataItemFullPropName = modelParts.join(".");

            let ngModelAndDataSourceWatchDisposal = this.$scope.$watchGroup([this.$attrs.radDataSource, ngModelDataItemFullPropName], (values: Array<any>) => {

                if (values == null || values.length == 0 || values.some(v => v == null))
                    return;

                this.dataSource = values[0];
                this.originalDataSourceTransportRead = this.dataSource['transport'].read;
                this.parentOfNgModel = values[1];

                ngModelAndDataSourceWatchDisposal();
                this.onWidgetInitialDataProvided();

            });
        }

        @Command()
        public async onWidgetInitialDataProvided(): Promise<void> {

            this.radValueFieldName = this.$attrs.radValueFieldName;
            this.radTextFieldName = this.$attrs.radTextFieldName;

            if (this.parentOfNgModel instanceof $data.Entity) {
                let parentOfNgModelType = this.parentOfNgModel.getType();
                if (parentOfNgModelType.memberDefinitions[`$${this.bindedMemberName}`] == null)
                    throw new Error(`${parentOfNgModelType['fullName']} has no member named ${this.bindedMemberName}`);
                let metadata = this.metadataProvider.getMetadataSync();
                let dtoMetadata = metadata.Dtos.find(d => d.DtoType == parentOfNgModelType['fullName']);
                if (dtoMetadata != null) {
                    let thisDSMemberType = this.dataSource.options.schema['jayType'];
                    if (thisDSMemberType != null) {
                        let lookup = dtoMetadata.MembersLookups.find(l => l.DtoMemberName == this.bindedMemberName && l.LookupDtoType == thisDSMemberType['fullName']);
                        if (lookup != null) {
                            if (lookup.BaseFilter_JS != null) {
                                let originalRead = this.originalDataSourceTransportRead;
                                this.dataSource['transport'].read = function (options) {
                                    options.data = options.data || {};
                                    options.data.lookupBaseFilter = lookup.BaseFilter_JS;
                                    return originalRead.apply(this, arguments);
                                }
                            }
                            if (this.radTextFieldName == null)
                                this.radTextFieldName = lookup.DataTextField;
                            if (this.radValueFieldName == null)
                                this.radValueFieldName = lookup.DataValueField;
                        }
                    }
                }
            }

            if (this.radValueFieldName == null) {
                if (this.dataSource.options.schema != null && this.dataSource.options.schema.model != null && this.dataSource.options.schema.model.idField != null)
                    this.radValueFieldName = this.dataSource.options.schema.model.idField;
            }

            if (this.$attrs.radText != null) {
                this.$scope.$watch(this.$attrs.ngModel.replace("::", ""), (newValue) => {
                    if (this.ngModel.$isEmpty(newValue))
                        this.radTextValue = "";
                    else {
                        const current = this.dataSource.current;
                        if (current != null)
                            this.radTextValue = current[this.radTextFieldName];
                    }
                });
            }

            const comboBoxOptions: kendo.ui.ComboBoxOptions = {
                dataSource: this.dataSource,
                autoBind: this.dataSource.flatView().length != 0 || this.$attrs.radText == null,
                dataTextField: this.radTextFieldName,
                dataValueField: this.radValueFieldName,
                filter: "contains",
                minLength: 3,
                valuePrimitive: true,
                ignoreCase: true,
                suggest: true,
                highlightFirst: true,
                change: (e) => {
                    this.dataSource.onCurrentChanged();
                },
                open: (e) => {
                    if (e.sender.options.autoBind == false && this.$attrs.radText != null) {
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

            if (this.radTextValue != null)
                comboBoxOptions.text = this.radTextValue;

            if (this.$attrs["itemTemplateId"] != null) {

                let itemTemplateElement = angular.element(`#${this.$attrs["itemTemplateId"]}`);

                let itemTemplateElementHtml = itemTemplateElement.html();

                let itemTemplate: any = kendo.template(itemTemplateElementHtml, { useWithBlock: false });

                comboBoxOptions.template = itemTemplate;
            }

            if (this.$attrs["headerTemplateId"] != null) {

                let headerTemplateElement = angular.element(`#${this.$attrs["headerTemplateId"]}`);

                let headerTemplateElementHtml = headerTemplateElement.html();

                let headerTemplate: any = kendo.template(headerTemplateElementHtml, { useWithBlock: false });

                comboBoxOptions.headerTemplate = headerTemplate;
            }

            if (this.dataSource.options.schema.model.fields[comboBoxOptions.dataTextField] == null)
                throw new Error(`Model has no property named ${comboBoxOptions.dataTextField} to be used as text field`);

            if (this.dataSource.options.schema.model.fields[comboBoxOptions.dataValueField] == null)
                throw new Error(`Model has no property named ${comboBoxOptions.dataValueField} to be used as value field`);

            if (this.$attrs.radVirtualEntityLoader != null) {

                comboBoxOptions.virtual = {
                    mapValueTo: 'dataItem',
                    valueMapper: async (options: { value: string, success: (e: Array<any>) => void }): Promise<void> => {

                        try {

                            if (this.ngModel.$isEmpty(options.value)) {
                                options.success([]);
                                return;
                            }

                            let items = $.makeArray(this.dataSource.data())
                                .filter(t => t[comboBoxOptions.dataValueField] == options.value);

                            if (items.length == 0) {
                                items = [(await this.radVirtualEntityLoader({ id: options.value }))];
                            }

                            options.success(items);

                            setTimeout(() => {
                                let comboBox = this.comboBox;
                                if (comboBox == null)
                                    return;
                                let input = comboBox.wrapper.find("input");
                                let item = items[0];
                                input.text(item[comboBoxOptions.dataTextField]);
                            }, 0);

                        }
                        finally {
                            ScopeManager.update$scope(this.$scope);
                        }

                    }
                }
            }

            DefaultRadComboBoxDirective.defaultRadComboBoxDirectiveCustomizers.forEach(radComboBoxCustomizer => {
                radComboBoxCustomizer(this.$scope, this.$attrs, this.$element, comboBoxOptions);
            });

            if (this.$attrs.radOnInit != null) {
                if (this.radOnInit != null) {
                    this.radOnInit({ comboBoxOptions: comboBoxOptions });
                }
            }

            this.options = comboBoxOptions;

        }

        @Command()
        public onWidgetCreated() {

            let comboBox = this.comboBox;

            if (this.mdInputContainer != null) {

                this.mdInputContainerParent = this.mdInputContainer.element;

                comboBox.wrapper.bind("focusin", this.onComboFocusIn.bind(this));
            }

            Object.defineProperty(this.dataSource, "current", {
                configurable: true,
                enumerable: false,
                get: () => {

                    let newCurrent = null;

                    const dataItem = comboBox.dataItem();

                    if (dataItem == null)
                        newCurrent = null;
                    else
                        newCurrent = dataItem.innerInstance != null ? dataItem.innerInstance() : dataItem;

                    if (newCurrent == null && this.$attrs.radText != null && !this.ngModel.$isEmpty(comboBox.value()) && comboBox.options.autoBind == false) {
                        newCurrent = {};
                        newCurrent[this.radValueFieldName] = comboBox.value();
                        newCurrent[this.radTextFieldName] = this.radTextValue;
                    };

                    return newCurrent;
                },
                set: (entity: $data.Entity) => {

                    let value = null;

                    if (entity != null) {
                        value = entity[this.radValueFieldName];
                    }

                    if (comboBox.value() != value)
                        comboBox.value(value);

                    if (this.ngModel.$isEmpty(value) && !this.ngModel.$isEmpty(comboBox.text()))
                        comboBox.text(null);

                    this.$scope.$applyAsync(() => {
                        this.ngModelValue = value;
                    });

                    this.dataSource.onCurrentChanged();
                }
            });

        }

        public onComboFocusIn() {
            if (this.$element.is(":disabled"))
                return;
            this.mdInputContainerParent.addClass("md-input-focused");
        }

        public $onDestroy() {
            if (this.dataSource != null) {
                this.dataSource['transport'].read = this.originalDataSourceTransportRead;
                delete this.dataSource.current;
            }
            if (this.comboBox != null) {
                this.comboBox.wrapper.unbind("focusin", this.onComboFocusIn);
                kendo.destroyWidget(this.comboBox);
            }
        }
    }
}