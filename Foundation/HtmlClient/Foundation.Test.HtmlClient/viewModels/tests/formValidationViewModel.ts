module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "FormValidationFormViewModel", routeTemplate: "/form-validation-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/formValidationview.html" })
    export class FormValidationFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("EntityContextProvider") public entityContextProvider: ViewModel.Contracts.IEntityContextProvider) {
            super();
        }

        public validationSampleDto: Test.Model.Dto.ValidationSampleDto = null;

        public async $routerOnActivate(route): Promise<void> {
            const context = await this.entityContextProvider.getReadContext<TestContainer>("Test");
            this.validationSampleDto = new Test.Model.Dto.ValidationSampleDto();
            return await super.$routerOnActivate(route);
        }

        @ViewModel.Command()
        public submitFirstPart(form: { validationSampleDtoForm: angular.INgModelController }): void {
            let isValid = form.validationSampleDtoForm['$$parentForm'].isValid();
        }
    }
}