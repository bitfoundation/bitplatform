module Bit.Tests.ViewModels {

    @DtoRulesDependency({ name: "ValidationSampleRules" })
    export class ValidationSampleRules extends Bit.Implementations.DtoRules<Bit.Tests.Model.Dto.ValidationSampleDto>{

        public validateMember(memberName: keyof Bit.Tests.Model.Dto.ValidationSampleDto, newValue: any, oldValue: any): void {
            if (memberName == "RequiredByDtoRulesMember") {
                this.setMemberValidaty("RequiredByDtoRulesMember", "required", newValue != null);
            }
            super.validateMember(memberName, newValue, oldValue);
        }

    }

    @DtoViewModelDependency({ name: "FormValidationFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/formValidationview.html" })
    export class FormValidationFormViewModel extends Bit.ViewModels.DtoViewModel<Bit.Tests.Model.Dto.ValidationSampleDto, ValidationSampleRules> {

        public constructor( @Inject("ValidationSampleRules") public validationSampleRules: ValidationSampleRules) {
            super();
            this.rules = validationSampleRules;
        }

        @Command()
        public async $onInit(): Promise<void> {
            this.model = new Bit.Tests.Model.Dto.ValidationSampleDto();
        }

        @Command()
        public submitFirstPart(form: Bit.ViewModels.DtoFormController<Bit.Tests.Model.Dto.ValidationSampleDto>): void {
            const isValid = form.isValid();
        }
    }
}