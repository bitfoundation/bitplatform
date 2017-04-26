module Foundation.Test.ViewModels {
    @Core.SecureFormViewModelDependency({ name: "SimpleFormViewModel", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/simpleview.html" })
    export class SimpleFormViewModel extends ViewModel.ViewModels.FormViewModel {
        public num = 10;
    }
}