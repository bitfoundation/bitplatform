module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "FormValidationFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/formValidationview.html" })
    export class FormValidationFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public validationSampleDto: Test.Model.Dto.ValidationSampleDto = null;

        @Foundation.ViewModel.Command()
        public async $onInit(): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContext>("Test");
            this.validationSampleDto = new Test.Model.Dto.ValidationSampleDto();
        }

        @ViewModel.Command()
        public submitFirstPart(form: { validationSampleDtoForm: angular.INgModelController }): void {
            let isValid = form.validationSampleDtoForm['$$parentForm'].isValid();
        }
    }
}