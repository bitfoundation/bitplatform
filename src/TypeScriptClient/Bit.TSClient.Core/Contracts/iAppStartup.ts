module Bit.Contracts {
    export interface IAppStartup {
        configuration(): Promise<void>;
    }
}