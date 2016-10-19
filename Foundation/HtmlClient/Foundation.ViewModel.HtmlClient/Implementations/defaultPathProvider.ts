module Foundation.ViewModel.Implementations {
    export abstract class DefaultPathProvider implements Contracts.IPathProvider {

        public abstract getProjectsPath(): Array<{ name: string, path: string }>;

        public getFullPath(relativePath: string | Function | (string | Function)[]): string {

            if (relativePath == null)
                return '';

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

            return `Files/V${Foundation.Core.ClientAppProfileManager.getCurrent().clientAppProfile.version}/${absolutePath}`;

        }
    }
}