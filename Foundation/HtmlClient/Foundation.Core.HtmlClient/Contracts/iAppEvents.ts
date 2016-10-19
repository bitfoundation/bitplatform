module Foundation.Core.Contracts {
    export interface IAppEvents {
        onAppStartup(): Promise<void>;
    }
}