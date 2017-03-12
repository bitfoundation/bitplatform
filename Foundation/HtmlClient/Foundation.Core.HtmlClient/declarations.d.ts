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
    }
}

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

declare var clientAppProfile: Foundation.Core.Contracts.IClientAppProfile;
declare var ngMaterial: { version: string };
declare var odatajs: any;
declare var jasmineRequire: any;
