module IdentityServerTest.Implementations {
    export class IdentityServerTestPathProvider extends Foundation.ViewModel.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [{ name: "IdentityServer", path: "/bit-identity-server/IdentityServer/HtmlClient" }];

        }
    }
}