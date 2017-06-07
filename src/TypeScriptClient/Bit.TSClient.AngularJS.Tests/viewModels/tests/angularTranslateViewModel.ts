module Bit.Tests.ViewModels {

    @SecureFormViewModelDependency({ name: "AngularTranslateFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/angularTranslateview.html" })
    export class AngularTranslateFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("$translate") public $translate: ng.translate.ITranslateService) {
            super();
        }

        public text: string = "KnownError";

        @Command()
        public changeText(): void {
            this.text = this.$translate.instant("UnKnownError");
        }

        @Command()
        public changeLanguage(): void {
            this.$translate.use("FaIr");
        }
    }
}