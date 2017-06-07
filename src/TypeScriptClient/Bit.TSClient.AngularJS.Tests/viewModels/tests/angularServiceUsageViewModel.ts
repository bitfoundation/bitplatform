module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "AngularServiceUsageFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/angularServiceUsageview.html" })
    export class AngularServiceUsageFormViewModel extends Bit.ViewModels.FormViewModel {

        public constructor( @Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        @Command()
        public $onInit(): void {
            this.$document.attr("title", "done");
        }
    }
}