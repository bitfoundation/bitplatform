module BitChangeSetManager.ViewModels {

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
        templateUrl: `view/changeSetView.html`,
        bindings: {
            changeSetSeveritiesDataSource: '<',
            changeSetDeliveryRequirementsDataSource: '<'
        }
    })
    export class ChangeSetViewModel extends DtoViewModel<BitChangeSetManagerModel.ChangeSetDto, ChangeSetRules> {

        public constructor(
            @Inject("$element") public $element: JQuery,
            @Inject("ChangeSetRules") public rules: ChangeSetRules,
            @Inject("$mdDialog") public $mdDialog: ng.material.IDialogService,
            @Inject("$translate") public $translate: ng.translate.ITranslateService) {
            super();
        }

        public changeSetSeveritiesDataSource: kendo.data.DataSource;
        public changeSetDeliveryRequirementsDataSource: kendo.data.DataSource;

        public changeSetMetadata = BitChangeSetManagerModel.ChangeSetDto;

        @Command()
        public async $onInit(): Promise<void> {

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

    }

}