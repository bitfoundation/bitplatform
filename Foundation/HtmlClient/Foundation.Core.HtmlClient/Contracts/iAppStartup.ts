module Foundation.Core.Contracts {
    export interface IAppStartup {
        configuration(): Promise<void>;
    }
}