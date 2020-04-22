

module Bit.Directives {
    @DirectiveDependency({
        name: "RadTreeView",
        scope: true,
        bindToController: {
            radOnInit: "&"
        },
        controllerAs: "radTreeView",
        template: ($element: JQuery, $attrs: ng.IAttributes) => {

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

            const template = `<div kendo-tree-view k-options="radTreeView.options" k-ng-delay="::radTreeView.options" />`;

            return template;
        },
        replace: true,
        terminal: true,
        require: {
            mdInputContainer: "^?mdInputContainer"
        },
        restrict: "E"
    })
    export class DefaultRadTreeViewDirective {

        public static defaultRadTreeViewDirectiveCustomizers: Array<($scope: ng.IScope, attribues: ng.IAttributes, $element: JQuery, treeViewOptions: kendo.ui.TreeViewOptions) => void> = [];

        public constructor(@Inject("$element") public $element: JQuery,
            @Inject("$scope") public $scope: ng.IScope,
            @Inject("$attrs") public $attrs: ng.IAttributes & { ngModel: string, radDataSource: string, radValueFieldName: string, radTextFieldName: string, radOnInit: string },
            @Inject("MetadataProvider") public metadataProvider: Contracts.IMetadataProvider) {

        }

        private radTextFieldName: string;
        public dataSource: kendo.data.DataSource;
        public mdInputContainer: { element: JQuery };
        public mdInputContainerParent: JQuery;
        public options: kendo.ui.TreeViewOptions;
        public radOnInit: (args: { treeViewOptions: kendo.ui.TreeViewOptions }) => void;

        public get treeView(): kendo.ui.TreeView {
            return this.$element.data("kendoTreeView");
        }

        @Command()
        public async $onInit(): Promise<void> {

            this.$scope.$on("kendoWidgetCreated", this.onWidgetCreated.bind(this));

            let dataSourceWatchDisposal = this.$scope.$watch(this.$attrs.radDataSource, (ds: kendo.data.DataSource) => {

                if (ds == null) {
                    return;
                }

                this.dataSource = ds;

                dataSourceWatchDisposal();
                this.onWidgetInitialDataProvided();

            });
        }

        @Command()
        public async onWidgetInitialDataProvided(): Promise<void> {

            this.radTextFieldName = this.$attrs.radTextFieldName;

            const treeViewOptions: kendo.ui.TreeViewOptions = {
                dataSource: this.dataSource,
                autoBind: true,
                dataTextField: this.radTextFieldName,
                autoScroll: true,
                animation: true,
                checkboxes: false,
                dragAndDrop: false,
                loadOnDemand: true
            };

            if (this.$attrs["itemTemplateId"] != null) {

                const itemTemplateElement = angular.element(`#${this.$attrs["itemTemplateId"]}`);

                const itemTemplateElementHtml = itemTemplateElement.html();

                const itemTemplate: any = kendo.template(itemTemplateElementHtml, { useWithBlock: false });

                treeViewOptions.template = itemTemplate;
            }

            if (this.dataSource.options.schema.model.fields[treeViewOptions.dataTextField] == null) {
                throw new Error(`Model has no property named ${treeViewOptions.dataTextField} to be used as text field`);
            }

            DefaultRadTreeViewDirective.defaultRadTreeViewDirectiveCustomizers.forEach(radTreeViewCustomizer => {
                radTreeViewCustomizer(this.$scope, this.$attrs, this.$element, treeViewOptions);
            });

            if (this.$attrs.radOnInit != null) {
                if (this.radOnInit != null) {
                    this.radOnInit({ treeViewOptions: treeViewOptions });
                }
            }

            this.options = treeViewOptions;

        }

        @Command()
        public onWidgetCreated() {

            if (this.mdInputContainer != null) {

                this.mdInputContainerParent = this.mdInputContainer.element;

                this.treeView.wrapper.bind("focusin", this.onTreeViewFocusIn.bind(this));
                this.treeView.wrapper.bind("focusout", this.onTreeViewFocusOut.bind(this));
                this.mdInputContainerParent.addClass("md-input-has-value");
            }
        }

        public onTreeViewFocusIn() {
            if (this.$element.is(":disabled")) {
                return;
            }

            this.mdInputContainerParent.addClass("md-input-focused");
        }

        public onTreeViewFocusOut() {
            this.mdInputContainerParent.removeClass("md-input-focused");
        }

        public $onDestroy() {
            if (this.treeView != null) {
                this.treeView.wrapper.unbind("focusin", this.onTreeViewFocusIn);
                this.treeView.wrapper.bind("focusout", this.onTreeViewFocusOut);
                kendo.destroyWidget(this.treeView);
            }
        }
    }
}