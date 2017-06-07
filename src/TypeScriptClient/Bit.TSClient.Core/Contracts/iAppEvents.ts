module Bit.Contracts {
    export interface IAppEvents {
        onAppStartup(): Promise<void>;
    }
}