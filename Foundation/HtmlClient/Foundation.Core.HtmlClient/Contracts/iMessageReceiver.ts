module Foundation.Core.Contracts {
    export interface IMessageReceiver {
        stop(): Promise<void>;
        start(config?: { preferWebSockets?: boolean }): Promise<void>;
    }
}