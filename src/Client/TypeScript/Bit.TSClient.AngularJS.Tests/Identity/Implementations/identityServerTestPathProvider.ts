module Bit.Tests.Identity {
    @ObjectDependency({
        name: "PathProvider"
    })
    export class IdentityServerTestPathProvider extends Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [{ name: "IdentityServer", path: "/src/Client/TypeScript/Bit.TSClient.AngularJS" }];

        }
    }
}