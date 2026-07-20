/**
 * NOTE: The L10n-implementations should use lowercase language-codes
 *       internally.
 * @implements {IL10n}
 */
declare class L10n implements IL10n {
    static "__#61@#fixupLangCode"(langCode: any): any;
    static "__#61@#isRTL"(lang: any): boolean;
    constructor({ lang, isRTL }: {
        lang: any;
        isRTL: any;
    }, l10n?: null);
    _setL10n(l10n: any): void;
    /** @inheritdoc */
    getLanguage(): any;
    /** @inheritdoc */
    getDirection(): string;
    /** @inheritdoc */
    get(ids: any, args: null | undefined, fallback: any): Promise<any>;
    /** @inheritdoc */
    translate(element: any): Promise<void>;
    /** @inheritdoc */
    translateOnce(element: any): Promise<void>;
    /** @inheritdoc */
    destroy(): Promise<void>;
    /** @inheritdoc */
    pause(): void;
    /** @inheritdoc */
    resume(): void;
}
