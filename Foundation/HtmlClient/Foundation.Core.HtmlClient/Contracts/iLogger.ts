module Foundation.Core.Contracts {
    export interface ILogger {
        logInfo(message: string, additionalInfo?: string, err?: Error): void;
        logWarn(message: string, additionalInfo?: string, err?: Error): void;
        logError(message: string, additionalInfo?: string, err?: Error): void;
        logDetailedError(instance: any, methodName: string, args: any, err: Error);
        logFatal(message: string, additionalInfo?: string, err?: Error): void;
    }
}