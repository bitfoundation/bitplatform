/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.Test.Implementations {
    @Core.ObjectDependency({
        name: 'PathProvider'
    })
    export class FoundationTestPathProvider extends Foundation.ViewModel.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [{ name: "Foundation", path: "/bit-framework/Foundation/HtmlClient" }];
        }

    }
}