module Bit.Tests.Implementations.Tests {
    export class ValidationTests {
        public static async testValidationViewModelWithValidBehavior(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.FormValidationViewModel>(angular.element("#formValidationView"));

            uiAutomation.viewModel.model.RequiredByAttributeMember = "value1";

            uiAutomation.viewModel.model.RequiredByMetadataMember = "value2";

            uiAutomation.viewModel.model.NotRequiredMember = "value3";

            uiAutomation.viewModel.model.RequiredByDtoRulesMember = "value4";

            const form = uiAutomation.getForm(angular.element("[name='validationSampleDtoForm']"));

            await uiAutomation.viewModel.submitFirstPart(form);

            if (uiAutomation.view.find("#firstPartIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByDtoRulesIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByDtoRulesErrors").text() != "{}")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredErrors").text() != "{}")
                throw new Error("Validation has a problem");
        }

        static async testValidationViewModelWithInValidBehavior(): Promise<void> {

            const uiAutomation = new Bit.Implementations.UIAutomation<ViewModels.FormValidationViewModel>(angular.element("#formValidationView"));

            const form = uiAutomation.getForm(angular.element("[name='validationSampleDtoForm']"));

            await uiAutomation.viewModel.submitFirstPart(form);

            if (uiAutomation.view.find("#firstPartIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByAttributeMemberErrors").text() != '{"required":true}')
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByMetadataMemberErrors").text() != '{"required":true}')
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByDtoRulesIsValid").text() != "false")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#requiredByDtoRulesErrors").text() != '{"required":true}')
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredIsValid").text() != "true")
                throw new Error("Validation has a problem");

            if (uiAutomation.view.find("#notRequiredErrors").text() != "{}")
                throw new Error("Validation has a problem");

            await ValidationTests.testValidationViewModelWithValidBehavior();
        }
    }
}