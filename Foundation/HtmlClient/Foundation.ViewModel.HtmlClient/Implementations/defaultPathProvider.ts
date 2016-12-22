module Foundation.ViewModel.Implementations {
    export abstract class DefaultPathProvider implements Contracts.IPathProvider {

        public constructor(public clientAppProfileManager = Core.DependencyManager.getCurrent().resolveObject<Core.ClientAppProfileManager>("ClientAppProfileManager")) {

        }

        public abstract getProjectsPath(): Array<{ name: string, path: string }>;

        public getFullPath(relativePath: string | Function | (string | Function)[]): string {

            if (relativePath == null)
                return "";

            let rPath: string = null;

            if (typeof relativePath == "function")
                rPath = (relativePath as Function)();
            else
                rPath = relativePath as string;

            let absolutePath = rPath;
            this.getProjectsPath()
                .forEach(p => {
                    absolutePath = absolutePath.replace(`|${p.name}|`, p.path);
                });

            return `Files/V${this.clientAppProfileManager.getClientAppProfile().version}/${absolutePath}`;

        }
    }
}