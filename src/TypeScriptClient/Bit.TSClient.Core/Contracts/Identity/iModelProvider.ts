module Bit.Contracts.Identity {
    export interface IModelProvider {
        getModel(): Models.Identity.ISsoModel;
    }
}