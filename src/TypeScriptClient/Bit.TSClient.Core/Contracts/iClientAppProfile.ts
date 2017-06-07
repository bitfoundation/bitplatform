module Bit.Contracts {

    export interface EnvironmentConfig {
        key: string;
        value: any;
    }

    export interface IClientAppProfile {
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