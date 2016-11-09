module Foundation.Core.Contracts {
    export interface IMessageReciever {
        onMessageRecieved(messageKey: string, callback: (args?: any) => Promise<void>): () => void;
        stop(): Promise<void>;
        start(config?: { preferWebSockets: boolean }): Promise<void>;
    }
}