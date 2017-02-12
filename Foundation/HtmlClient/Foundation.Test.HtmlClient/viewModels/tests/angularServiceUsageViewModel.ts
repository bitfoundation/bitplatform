module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "AngularServiceUsageFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/angularServiceUsageview.html" })
    export class AngularServiceUsageFormViewModel extends ViewModel.ViewModels.SecureFormViewModel {

        public constructor( @Core.Inject("$document") public $document: ng.IDocumentService) {
            super();
        }

        @ViewModel.Command()
        public $onInit(): void {
            this.$document.attr("title", "done");
        }
    }
}