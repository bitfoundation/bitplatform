module Foundation.Test.Implementations.Tests {
    export class ValidationTests {
        public static async testValidationFormViewModelWithValidBehavior(): Promise<void> {

            let uiAutomation = new UIAutomation<ViewModels.FormValidationFormViewModel>(angular.element("#formValidationView"));

            uiAutomation.formViewModel.validationSampleDto.RequiredByAttributeMember = "value1";

            uiAutomation.formViewModel.validationSampleDto.RequiredByMetadataMember = "value2";

            uiAutomation.formViewModel.validationSampleDto.NotRequiredMember = "value3";

            let form = uiAutomation.getForm(angular.element("[name='validationSampleDtoForm']"));

            await uiAutomation.formViewModel.submitFirstPart(form);

            if (uiAutomation.view.find("#requiredByAttributeMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#firstPartIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredErrors").text() != "{}")
                throw new Error("Validation has a problem");
        }

        static async testValidationFormViewModelWithInValidBehavior(): Promise<void> {

            let uiAutomation = new UIAutomation<ViewModels.FormValidationFormViewModel>(angular.element("#formValidationView"));

            let form = uiAutomation.getForm(angular.element("[name='validationSampleDtoForm']"));

            await uiAutomation.formViewModel.submitFirstPart(form);

            if (uiAutomation.view.find("#requiredByAttributeMemberIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#firstPartIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberErrors").text() != '{"required":true}')
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberErrors").text() != '{"required":true}')
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredErrors").text() != "{}")
                throw new Error("Validation has a problem");

            uiAutomation.formViewModel.validationSampleDto.RequiredByAttributeMember = "value1";

            uiAutomation.formViewModel.validationSampleDto.RequiredByMetadataMember = "value2";

            uiAutomation.formViewModel.validationSampleDto.NotRequiredMember = "value3";

            await uiAutomation.formViewModel.submitFirstPart(form);

            if (uiAutomation.view.find("#requiredByAttributeMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#firstPartIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredErrors").text() != "{}")
                throw new Error("Validation has a problem");
        }
    }
}