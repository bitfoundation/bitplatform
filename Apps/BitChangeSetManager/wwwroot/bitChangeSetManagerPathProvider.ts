
module BitChangeSetManager {
    @ObjectDependency({
        name: "PathProvider"
    })
    export class BitChangeSetManagerPathProvider extends Bit.Implementations.DefaultPathProvider {

        public getProjectsPath(): Array<{ name: string, path: string }> {
            return [];
        }

    }
}