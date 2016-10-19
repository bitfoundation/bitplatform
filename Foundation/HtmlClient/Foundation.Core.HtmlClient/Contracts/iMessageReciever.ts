module Foundation.Core.Contracts {
    export interface IMessageReciever {
        onMessageRecieved(messageKey: string, callback: (args?: any) => Promise<void>): Promise<void>;
        removeCallback(messageKey: string, callback: (args?: any) => Promise<void>): void;
    }
}