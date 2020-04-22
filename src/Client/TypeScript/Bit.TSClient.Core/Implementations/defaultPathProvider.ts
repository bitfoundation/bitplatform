module Bit.Implementations {

    export class PathUtils {

        private static urlRegex = new RegExp("^([a-z]+://|//)", "i"); // https://stackoverflow.com/a/19692053/2720104

        public static isUrlPath(path: string): boolean {

            if (path == null || path == "") {
                return false;
            }

            return PathUtils.urlRegex.test(path);
        }

    }

    export abstract class DefaultPathProvider implements Contracts.IPathProvider {

        public abstract getProjectsPath(): Array<{ name: string, path: string }>;

        public getFullPath(relativePath: string | Function | (string | Function)[]): string {

            if (relativePath == null) {
                return "";
            }

            let rPath: string = null;

            if (typeof relativePath == "function") {
                rPath = (relativePath as Function)();
            } else {
                rPath = relativePath as string;
            }

            let absolutePath = rPath;
            this.getProjectsPath()
                .forEach(p => {
                    absolutePath = absolutePath.replace(`|${p.name}|`, p.path);
                });

            if (!PathUtils.isUrlPath(absolutePath)) {
                return `Files/V${ClientAppProfileManager.getCurrent().getClientAppProfile().version}/${absolutePath}`;
            } else {
                return absolutePath;
            }
        }
    }
}
