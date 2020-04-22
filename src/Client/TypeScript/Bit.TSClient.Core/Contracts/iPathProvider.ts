module Bit.Contracts {
    export interface IPathProvider {
        getFullPath(relativePath: string | Function | (string | Function)[]): string;
        getProjectsPath(): Array<{ name: string, path: string }>;
    }
}