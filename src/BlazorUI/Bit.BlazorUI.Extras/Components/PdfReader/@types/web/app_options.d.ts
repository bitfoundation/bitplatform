declare class AppOptions {
    static eventBus: any;
    static "__#65@#opts": Map<any, any>;
    static get(name: any): any;
    static getAll(kind?: null, defaultOnly?: boolean): any;
    static set(name: any, value: any): void;
    static setAll(options: any, prefs?: boolean): void;
}
declare namespace OptionKind {
    let BROWSER: number;
    let VIEWER: number;
    let API: number;
    let WORKER: number;
    let EVENT_DISPATCH: number;
    let PREFERENCE: number;
}
