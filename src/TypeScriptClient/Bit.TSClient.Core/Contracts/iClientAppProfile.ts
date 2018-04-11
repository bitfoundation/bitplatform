module Bit.Contracts {

    export type EnvironmentConfig = {
        key: string;
        value: any;
    };

    export type IClientAppProfile = {
        screenSize: "DesktopAndTablet" | "MobileAndPhablet" | "";
        theme: string;
        culture: string;
        version: string;
        isDebugMode: boolean;
        clientType: "Cordova" | "Browser" | "Electron" | "";
        calendar: string;
        direction: "Ltr" | "Rtl";
        appTitle: string;
        appName: string;
        currentTimeZone: string;
        desiredTimeZone: string;
        environmentConfigs: Array<EnvironmentConfig>;
        getConfig<T>(configKey: string, defaultValueOnNotFound?: T): T;
    }
}