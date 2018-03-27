

module Bit.Directives {
    @DirectiveDependency({
        name: "RadMultiSelect",
        scope: true,
        bindToController: {
            ngModelValue: "=ngModel",
            radOnInit: "&"
        },
        controllerAs: "radMultiSelect",
        template: ($element: JQuery, $attrs: ng.IAttributes & { ngModelOptions: string }) => {

            const guidUtils = DependencyManager.getCurrent().resolveObject<Implementations.DefaultGuidUtils>("GuidUtils");

            const itemTemplate = $element
                .children("item-template");

            if (itemTemplate.length != 0) {

                const itemTemplateId = guidUtils.newGuid();

                itemTemplate
                    .attr("id", itemTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(itemTemplate);

                $attrs["itemTemplateId"] = itemTemplateId;
            }

            const tagTemplate = $element
                .children("tag-template");

            if (tagTemplate.length != 0) {

                const tagTemplateId = guidUtils.newGuid();

                tagTemplate
                    .attr("id", tagTemplateId)
                    .attr("ng-cloak", "");

                angular.element(document.body).append(tagTemplate);

                $attrs["tagTemplateId"] = tagTemplateId;
            }

            const headerTemplate = $element
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

            const template = `<select ${ngModelOptions} kendo-multi-select k-options="radMultiSelect.options" k-ng-delay="::radMultiSelect.options"></input>`;

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
    export class DefaultRadMultiSelectDirective {

        public static defaultRadMultiSelectDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, $element: JQuery, multiSelectOptions: kendo.ui.MultiSelectOptions) => void> = [];

        public constructor( @Inject("$element") public $element: JQuery,
            @Inject("$scope") public $scope: ng.IScope,
            @Inject("$attrs") public $attrs: ng.IAttributes & { ngModel: string, radDataSource: string, radValueFieldName: string, radTextFieldName: string, radOnInit: string },
            @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider) {

        }

        private radValueFieldName: string;
        private radTextFieldName: string;
        public ngModelValue: any;
        public bindedMemberName: string; // in vm.customer.FirstName >> This will return "FirstName"
        public parentOfNgModel: Model.Contracts.IDto; // in vm.customer.FirstName >> This will return customer instance
        public dataSource: kendo.data.DataSource;
        private originalDataSourceTransportRead: any;
        public ngModel: ng.INgModelController;
        public mdInputContainer: { element: JQuery };
        public mdInputContainerParent: JQuery;
        public options: kendo.ui.MultiSelectOptions;
        public radOnInit: (args: { multiSelectOptions: kendo.ui.MultiSelectOptions }) => void;

        public get multiSelect(): kendo.ui.MultiSelect {
            return this.$element.data("kendoMultiSelect");
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
                                this.dataSource['transport'].read = function read(options) {
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

            const multiSelectOptions: kendo.ui.MultiSelectOptions = {
                dataSource: this.dataSource,
                autoBind: true,
                dataTextField: this.radTextFieldName,
                dataValueField: this.radValueFieldName,
                filter: "contains",
                minLength: 3,
                valuePrimitive: true,
                ignoreCase: true,
                highlightFirst: true,
                change: this.applyMultiSelectValueToNgModel.bind(this),
                delay: 300,
                popup: {
                    appendTo: "md-dialog"
                },
                autoClose: false // Should be removed
            };

            if (this.$attrs["itemTemplateId"] != null) {

                let itemTemplateElement = angular.element(`#${this.$attrs["itemTemplateId"]}`);

                let itemTemplateElementHtml = itemTemplateElement.html();

                let itemTemplate: any = kendo.template(itemTemplateElementHtml, { useWithBlock: false });

                multiSelectOptions.itemTemplate = itemTemplate;
            }

            if (this.$attrs["tagTemplateId"] != null) {

                let tagTemplateElement = angular.element(`#${this.$attrs["tagTemplateId"]}`);

                let tagTemplateElementHtml = tagTemplateElement.html();

                let tagTemplate: any = kendo.template(tagTemplateElementHtml, { useWithBlock: false });

                multiSelectOptions.tagTemplate = tagTemplate;
            }

            if (this.$attrs["headerTemplateId"] != null) {

                let headerTemplateElement = angular.element(`#${this.$attrs["headerTemplateId"]}`);

                let headerTemplateElementHtml = headerTemplateElement.html();

                let headerTemplate: any = kendo.template(headerTemplateElementHtml, { useWithBlock: false });

                multiSelectOptions.headerTemplate = headerTemplate;
            }

            if (this.dataSource.options.schema.model.fields[multiSelectOptions.dataTextField] == null)
                throw new Error(`Model has no property named ${multiSelectOptions.dataTextField} to be used as text field`);

            if (this.dataSource.options.schema.model.fields[multiSelectOptions.dataValueField] == null)
                throw new Error(`Model has no property named ${multiSelectOptions.dataValueField} to be used as value field`);

            DefaultRadMultiSelectDirective.defaultRadMultiSelectDirectiveCustomizers.forEach(radMultiSelectCustomizer => {
                radMultiSelectCustomizer(this.$scope, this.$attrs, this.$element, multiSelectOptions);
            });

            if (this.$attrs.radOnInit != null) {
                if (this.radOnInit != null) {
                    this.radOnInit({ multiSelectOptions: multiSelectOptions });
                }
            }

            this.options = multiSelectOptions;

        }

        @Command()
        public async applyMultiSelectValueToNgModel(): Promise<void> {
            this.ngModelValue = this.multiSelect.value();
        }

        @Command()
        public onWidgetCreated() {

            let multiSelect = this.multiSelect;

            if (this.mdInputContainer != null) {

                this.mdInputContainerParent = this.mdInputContainer.element;

                multiSelect.wrapper.bind("focusin", this.onMultiSelectFocusIn.bind(this));
                multiSelect.wrapper.bind("focusout", this.onMultiSelectFocusOut.bind(this));

                this.$scope.$watchCollection<Array<any>>(this.$attrs.ngModel.replace("::", ""), (newVal, oldVal) => {
                    if (newVal != null && newVal.length != 0)
                        this.mdInputContainerParent.addClass("md-input-has-value");
                    else
                        this.mdInputContainerParent.removeClass("md-input-has-value");
                });
            }

            this.$scope.$watchCollection(this.$attrs.ngModel.replace("::", ""), (newValue, oldVal) => {
                this.multiSelect.value(newValue);
            });
        }

        public onMultiSelectFocusIn() {
            if (this.$element.is(":disabled"))
                return;
            this.mdInputContainerParent.addClass("md-input-focused");
            this.multiSelect.open(); // Should be removed
        }

        public onMultiSelectFocusOut() {
            this.mdInputContainerParent.removeClass("md-input-focused");
        }

        public $onDestroy() {
            if (this.dataSource != null) {
                this.dataSource['transport'].read = this.originalDataSourceTransportRead;
            }
            if (this.multiSelect != null) {
                this.multiSelect.wrapper.unbind("focusin", this.onMultiSelectFocusIn);
                this.multiSelect.wrapper.bind("focusout", this.onMultiSelectFocusOut);
                kendo.destroyWidget(this.multiSelect);
            }
        }
    }
}