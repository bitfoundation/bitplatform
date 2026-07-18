/**
 * @implements {IL10n}
 */
declare class GenericL10n extends L10n implements IL10n {
    /**
     * Generate the bundles for Fluent.
     * @param {String} defaultLang - The fallback language to use for
     *   translations.
     * @param {String} baseLang - The base language to use for translations.
     */
    static "__#62@#generateBundles"(defaultLang: string, baseLang: string): AsyncGenerator<any, void, unknown>;
    static "__#62@#createBundle"(lang: any, baseURL: any, paths: any): Promise<any>;
    static "__#62@#getPaths"(): Promise<{
        baseURL: any;
        paths: any;
    }>;
    static "__#62@#generateBundlesFallback"(lang: any): AsyncGenerator<any, void, unknown>;
    static "__#62@#createBundleFallback"(lang: any): Promise<any>;
    constructor(lang: any);
}
