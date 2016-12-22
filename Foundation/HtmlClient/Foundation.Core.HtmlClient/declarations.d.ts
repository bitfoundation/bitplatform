declare module kendo {
    export module data {
        export interface ObservableObject extends Observable {
            innerInstance(): $data.Entity;
        }
        export interface DataSource extends Observable {
            flatView(): ObservableArray;
            dataView<TEntity>(): Array<TEntity>;
            onCurrentChanged(action?): void;
            asChildOf(parentDataSource: DataSource, childKeys: string[], parentKeys: string[]);
            current: $data.Entity | Model;
        }
        export interface Model extends ObservableObject {
            innerInstance(): $data.Entity;
        }
    }
}

// Type definitions for fetch API
// Spec: https://fetch.spec.whatwg.org/
// Polyfill: https://github.com/github/fetch
// Definitions by: Ryan Graham <https://github.com/ryan-codingintrigue>

interface FetchOptions {
    method?: "GET" | "POST" | "DELETE" | "PATCH" | "PUT" | "HEAD" | "OPTIONS" | "CONNECT";
    headers?: any;
    body?: any;
    mode?: "cors" | "no-cors" | "same-origin";
    credentials?: "omit" | "same-origin" | "include";
    cache?: "default" | "no-store" | "reload" | "no-cache" | "force-cache" | "only-if-cached";
    redirect?: "follow" | "error" | "manual";
    referrer?: string;
    referrerPolicy?: "referrer" | "no-referrer-when-downgrade" | "origin" | "origin-when-cross-origin" | "unsafe-url";
    integrity?: any;
}

declare enum ResponseType {
    Basic,
    Cors,
    Default,
    Error,
    Opaque
}

interface Headers {
    append(name: string, value: string): void;
    delete(name: string): void;
    get(name: string): string;
    getAll(name: string): Array<string>;
    has(name: string): boolean;
    set(name: string, value: string): void;
}

interface Body {
    bodyUsed: boolean;
    arrayBuffer(): Promise<ArrayBuffer>;
    blob(): Promise<Blob>;
    formData(): Promise<FormData>;
    json(): Promise<JSON>;
    text(): Promise<string>;
}

interface Response extends Body {
    error(): Response;
    redirect(url: string, status?: number): Response;
    type: ResponseType;
    url: string;
    status: number;
    ok: boolean;
    statusText: string;
    headers: Headers;
    clone(): Response;
}

declare var fetch: (url: string, options?: FetchOptions) => Promise<Response>;

interface Array<T> {
    toQueryable(entityType): $data.Queryable<$data.Entity>;
    totalCount: number;
}

declare namespace angular.material {
    interface IPromptDialog extends ng.material.IPresetDialog<IPromptDialog> {
        cancel(cancel: string): IPromptDialog;
        placeholder(placeholder: string): IPromptDialog;
    }
    interface IDialogService {
        show(dialog: ng.material.IDialogOptions | ng.material.IAlertDialog | ng.material.IConfirmDialog | IPromptDialog): ng.IPromise<any>;
        prompt(): IPromptDialog;
    }
}

declare namespace decimal {
    interface IDecimalStatic extends IDecimalConfig {
        add(firstValue: string, secondValue: string): string;
        mul(firstValue: string, secondValue: string): string;
        div(firstValue: string, secondValue: string): string;
        sub(firstValue: string, secondValue: string): string;
    }
}

interface Function {
    injects?: { name: string, kind: "Single" | "All" }[];
}
