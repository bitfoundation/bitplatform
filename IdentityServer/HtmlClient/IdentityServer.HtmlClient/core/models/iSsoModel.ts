module IdentityServer.Core.Models {

    export interface ISsoModel {
        additionalLinks: any;
        allowRememberMe: boolean;
        antiForgery: {
            name: string;
            value: string;
        };
        clientLogoUrl: string;
        clientName: string;
        clientUrl: string;
        currentUser: string;
        custom: any;
        errorMessage: string;
        externalProviders: Array<any>;
        loginUrl: string;
        logoutUrl: string;
        rememberMe: boolean;
        requestId: string;
        siteName: string;
        siteUrl: string;
        username: string;
        password: string;
        redirectUrl: any;
        autoRedirect: boolean;
        autoRedirectDelay: number;
        returnUrl: string;
    }
}