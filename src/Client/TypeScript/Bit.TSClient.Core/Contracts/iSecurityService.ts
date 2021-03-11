module Bit.Contracts {

    export type Token = {
        access_token: string;
        id_token: string;
        expires_in: number;
        token_type: string;
        login_date: Date;
    };

    export type BitJwtToken = {
        UserId: string;
        CustomProps: Map<string, string>;
    }

    export interface ISecurityService {
        isLoggedIn(): boolean;
        login(state?: any, client_id?: string, acr_values?: { key: string, value: string }[]): void;
        logout(state?: any, client_id?: string): void;
        loginWithCredentials(userName: string, password: string, client_id: string, client_secret: string, acr_values?: { key: string, value: string }[], scopes?: string[]): Promise<Token>;
        getCurrentToken(): Token;
        getCurrentBitJwtToken(): BitJwtToken;
        getLoginUrl(state?: any, client_id?: string, acr_values?: { key: string, value: string }[]): string;
        getLogoutUrl(id_token: string, state?: any, client_id?: string): string;
    }
}
