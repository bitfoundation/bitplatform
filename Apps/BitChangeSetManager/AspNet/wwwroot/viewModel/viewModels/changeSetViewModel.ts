module BitChangeSetManager.ViewModel.ViewModels {

    @DtoRulesDependency({ name: "ChangeSetRules" })
    export class ChangeSetRules extends DtoRules<BitChangeSetManagerModel.ChangeSetDto> {

        public validateMember(memberName: keyof BitChangeSetManagerModel.ChangeSetDto, newValue: any, oldValue: any): void {

            if (memberName == "Title")
                this.setMemberValidaty("Title", "max-length", newValue == null || (newValue as string).length < 50);
            else if (memberName == "Description")
                this.setMemberValidaty("Description", "max-length", newValue == null || (newValue as string).length < 200);

            super.validateMember(memberName, newValue, oldValue);
        }

    }

    @DtoViewModelDependency({
        name: "ChangeSetViewModel",
        templateUrl: `view/views/changeSetView.html`,
        bindings: {
            changeSetSeveritiesDataSource: '<',
            changeSetDeliveryRequirementsDataSource: '<'
        }
    })
    export class ChangeSetViewModel extends DtoViewModel<BitChangeSetManagerModel.ChangeSetDto, ChangeSetRules> {

        public constructor(
            @Inject("$element") public $element: JQuery,
            @Inject("EntityContextProvider") public entityContextProvider: IEntityContextProvider,
            @Inject("ChangeSetRules") public rules: ChangeSetRules,
            @Inject("$mdDialog") public $mdDialog: ng.material.IDialogService,
            @Inject("$translate") public $translate: ng.translate.ITranslateService) {
            super();
        }

        public changeSetSeveritiesDataSource: kendo.data.DataSource;
        public changeSetDeliveryRequirementsDataSource: kendo.data.DataSource;
        public templates: BitChangeSetManagerModel.ChangeSetDescriptionTemplateDto[];
        public provincesDataSource: kendo.data.DataSource;
        public citiesDataSource: kendo.data.DataSource;
        public answersDataSource: kendo.data.DataSource;
        public context: BitChangeSetManagerContext;

        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;

        @Command()
        public async $onInit(): Promise<void> {
            this.context = await this.entityContextProvider.getContext<BitChangeSetManagerContext>("BitChangeSetManager");
            this.templates = await this.context.changeSetDescriptionTemplate.getAllTemplates().toArray();
            this.provincesDataSource = this.context.provinces.asKendoDataSource();
            this.citiesDataSource = this.context.cities.asKendoDataSource({ serverPaging: true, pageSize: 28, serverSorting: true, sort: { field: "Name", dir: "asc" } });
            this.citiesDataSource.asChildOf(this.provincesDataSource, ["ProvinceId"], ["Id"]);
            this.answersDataSource = this.context.constants.asKendoDataSource();
        }

        @Command()
        public async applyTemplate(template: BitChangeSetManagerModel.ChangeSetDescriptionTemplateDto) {
            this.model.Description = template.Content;
        }

        public onSave() {

            if (this.form.isValid() == false || this.model.isValid() == false) {

                this.$mdDialog.show(
                    this.$mdDialog.alert()
                        .ok(this.$translate.instant("Ok"))
                        .title(this.$translate.instant("ChangeSetDataIsInvalid"))
                        .parent(this.$element));

                throw new Error("Change set data is invalid");
            }

        }

        @Command()
        public async loadCityById(args: { id: any }) {
            return await this.context.cities.first((c,id) => c.Id == id, args);
        }

    }

}