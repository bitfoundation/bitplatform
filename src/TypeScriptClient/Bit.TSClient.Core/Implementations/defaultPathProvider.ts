module Bit.Implementations {

    export class PathUtils {

        private static urlRegx = new RegExp('^(?!mailto:)(?:(?:http|https|ftp)://)(?:\\S+(?::\\S*)?@)?(?:(?:(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}(?:\\.(?:[0-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))|(?:(?:[a-z\\u00a1-\\uffff0-9]+-?)*[a-z\\u00a1-\\uffff0-9]+)(?:\\.(?:[a-z\\u00a1-\\uffff0-9]+-?)*[a-z\\u00a1-\\uffff0-9]+)*(?:\\.(?:[a-z\\u00a1-\\uffff]{2,})))|localhost)(?::\\d{2,5})?(?:(/|\\?|#)[^\\s]*)?$', 'i');

        public static isUrlPath(path: string): boolean {

            if (path == null || path == "")
                return false;

            return PathUtils.urlRegx.test(path);
        }

    }

    export abstract class DefaultPathProvider implements Contracts.IPathProvider {

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

            if (!PathUtils.isUrlPath(absolutePath))
                return `Files/V${ClientAppProfileManager.getCurrent().getClientAppProfile().version}/${absolutePath}`;
            else
                return absolutePath;
        }
    }
}