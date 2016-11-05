declare module angular {
    export interface INgModelController {
        isValid(): boolean;
        editable(propName: string, isEditable?: boolean): boolean;
        visible(propName: string, isVisible?: boolean): boolean;
    }

    export interface Router {
        root: ng.Router;
    }

    export interface RouterOutlet {
        $$outlet: {
            currentInstruction: ng.ComponentInstruction
        }
    }
}

declare module kendo {
    export module data {
        export interface ObservableObject extends Observable {
            innerInstance(): $data.Entity;
        }
        export interface DataSource extends Observable {
            dataView<TEntity>(): Array<TEntity>;
            onCurrentChanged(action?): void;
            asChildOf(parentDataSource: kendo.data.DataSource, childKeys: string[], parentKeys: string[]);
            current: $data.Entity | kendo.data.Model;
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
    method: string;
    headers: any;
    body: any;
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
    interface IPromptDialog extends IPresetDialog<IPromptDialog> {
        cancel(cancel: string): IPromptDialog;
        placeholder(placeholder: string): IPromptDialog;
    }
    interface IDialogService {
        show(dialog: IDialogOptions | IAlertDialog | IConfirmDialog | IPromptDialog): angular.IPromise<any>;
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
    inject?: string[];
}