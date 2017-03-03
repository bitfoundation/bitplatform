
module BitChangeSetManager {
    @ObjectDependency({
        name: "PathProvider"
    })
    export class BitChangeSetManagerPathProvider extends FoundationVM.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [];
        }

    }
}