module Bit.Contracts {
    export interface ISecurityService {
        isLoggedIn(): boolean;
        login(state?: any): void;
        logout(): void;
    }
}