module Bit.Contracts.Identity {
    export interface ILoginModelProvider {
        getModel(): Models.Identity.LoginModel;
    }
}