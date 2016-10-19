module Foundation.Test.ViewModels {

    @Core.FormViewModelDependency({ name: "AngularTranslateFormViewModel", routeTemplate: "/angular-translate-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/angularTranslateview.html" })
    export class AngularTranslateFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("$translate") public $translate: angular.translate.ITranslateService) {
            super();
        }

        public text: string = "KnownError";

        @ViewModel.Command()
        public changeText(): void {
            this.text = this.$translate.instant("UnKnownError");
        }

        @ViewModel.Command()
        public changeLanguage(): void {
            this.$translate.use('FaIr');
        }
    }
}