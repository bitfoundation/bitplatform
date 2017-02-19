module IdentityServerTest.Implementations {
    @Foundation.Core.ObjectDependency({
        name: "PathProvider"
    })
    export class IdentityServerTestPathProvider extends Foundation.ViewModel.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [{ name: "IdentityServer", path: "/bit-framework/IdentityServer/HtmlClient/IdentityServer.HtmlClient" }];

        }
    }
}