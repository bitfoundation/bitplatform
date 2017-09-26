module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "AngularServiceUsageViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/angularServiceUsageview.html" })
    export class AngularServiceUsageViewModel {

        public constructor( @Inject("$document") public $document: ng.IDocumentService) {
            
        }

        @Command()
        public $onInit(): void {
            this.$document.attr("title", "done");
        }
    }
}