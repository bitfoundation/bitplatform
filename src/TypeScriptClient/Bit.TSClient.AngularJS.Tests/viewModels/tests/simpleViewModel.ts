module Bit.Tests.ViewModels {
    @SecureFormViewModelDependency({ name: "SimpleFormViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/simpleview.html" })
    export class SimpleFormViewModel extends Bit.ViewModels.FormViewModel {
        public num = 10;
    }
}