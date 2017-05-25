module Foundation.Test.ViewModels {

    @Core.DtoRulesDependency({ name: "ValidationSampleRules" })
    export class ValidationSampleRules extends ViewModel.Implementations.DtoRules<Bit.Tests.Model.Dto.ValidationSampleDto>{

        public validateMember(memberName: keyof Bit.Tests.Model.Dto.ValidationSampleDto, newValue: any, oldValue: any): void {
            if (memberName == "RequiredByDtoRulesMember") {
                this.setMemberValidaty("RequiredByDtoRulesMember", "required", newValue != null);
            }
            super.validateMember(memberName, newValue, oldValue);
        }

    }

    @Core.DtoViewModelDependency({ name: "FormValidationFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/formValidationview.html" })
    export class FormValidationFormViewModel extends ViewModel.ViewModels.DtoViewModel<Bit.Tests.Model.Dto.ValidationSampleDto, ValidationSampleRules> {

        public constructor( @Core.Inject("ValidationSampleRules") public validationSampleRules: ValidationSampleRules) {
            super();
            this.rules = validationSampleRules;
        }

        @ViewModel.Command()
        public async $onInit(): Promise<void> {
            this.model = new Bit.Tests.Model.Dto.ValidationSampleDto();
        }

        @ViewModel.Command()
        public submitFirstPart(form: ViewModel.ViewModels.DtoFormController<Bit.Tests.Model.Dto.ValidationSampleDto>): void {
            const isValid = form.isValid();
        }
    }
}