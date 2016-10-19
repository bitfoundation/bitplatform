module Foundation.Test.ViewModels {
    @Core.FormViewModelDependency({ name: "SimpleFormViewModel", routeTemplate: "/simple-page", templateUrl: "|Foundation|/Foundation.Test.HtmlClient/views/tests/simpleview.html" })
    export class SimpleFormViewModel extends Foundation.ViewModel.ViewModels.SecureFormViewModel {
        public num = 10;
    }
}