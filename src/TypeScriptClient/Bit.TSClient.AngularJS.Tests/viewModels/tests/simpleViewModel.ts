module Bit.Tests.ViewModels {
    @ComponentDependency({ name: "SimpleViewModel", templateUrl: "|Bit|/Bit.TSClient.AngularJS.Tests/views/tests/simpleview.html" })
    export class SimpleViewModel {
        public num = 10;
    }
}