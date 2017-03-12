
module BitChangeSetManagerSecurity {
    @ObjectDependency({
        name: "PathProvider"
    })
    export class BitChangeSetManagerSecurityPathProvider extends FoundationVM.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [{
                name: "IdentityServer", path: "bower_components/bit-releases/foundation.identity/"
            }];
        }

    }
}