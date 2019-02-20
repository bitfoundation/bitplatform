module Bit.Tests.ViewModels {

    @ComponentDependency({ name: "AngularTranslateViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/angularTranslateview.html" })
    export class AngularTranslateViewModel {

        public constructor(@Inject("$translate") public $translate) {
        }

        public text: string = "KnownError";

        @Command()
        public changeText(): void {
            this.text = this.$translate.instant("UnknownError");
        }

        @Command()
        public changeLanguage(): void {
            this.$translate.use("FaIr");
        }
    }
}
