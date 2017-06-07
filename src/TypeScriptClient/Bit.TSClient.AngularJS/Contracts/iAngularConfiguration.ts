module Bit.Contracts {
    export interface IAngularConfiguration {
        configure(app: ng.IModule): Promise<void>;
    }
}