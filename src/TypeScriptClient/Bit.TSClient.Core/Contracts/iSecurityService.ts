module Bit.Contracts {

    export type Token = {
        access_token: string;
        id_token: string;
        expires_in: number;
        token_type: string;
        login_date: Date;
    };

    export interface ISecurityService {
        isLoggedIn(): boolean;
        login(state?: any, client_id?: string): void;
        logout(state?: any, client_id?: string): void;
        loginWithCredentials(userName: string, password: string, client_id: string, client_secret: string, parameters?: { key: string, value: string }[], scopes?: string[]): Promise<Token>;
        getCurrentToken(): Token;
        getLoginUrl(state?: any, client_id?: string): string;
        getLogoutUrl(id_token: string, state?: any, client_id?: string): string;
    }
}
