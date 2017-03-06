module BitChangeSetManager.ViewModels {

    @DtoRulesDependency({ name: "ChangeSetRules" })
    export class ChangeSetRules extends DtoRules<BitChangeSetManagerModel.ChangeSetDto> {

        public validateMember(memberName: keyof BitChangeSetManagerModel.ChangeSetDto, newValue: any, oldValue: any): void {
            super.validateMember(memberName, newValue, oldValue);
        }

    }

    @DtoViewModelDependency({
        name: "ChangeSetViewModel",
        templateUrl: `view/changeSetView.html`
    })
    export class ChangeSetViewModel extends DtoViewModel<BitChangeSetManagerModel.ChangeSetDto, ChangeSetRules> {

        public constructor( @Inject("ChangeSetRules") public rules: ChangeSetRules) {
            super();
        }

        @Command()
        public async $onInit(): Promise<void> {

        }

    }

}