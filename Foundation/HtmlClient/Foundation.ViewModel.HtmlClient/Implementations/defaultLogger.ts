/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
/// <reference path="../../foundation.viewModel.htmlclient/foundation.model.context.d.ts" />
/// <reference path="../../foundation.core.htmlclient/typings.d.ts" />
/// <reference path="../../foundation.core.htmlclient/declarations.d.ts" />

module Foundation.ViewModel.Implementations {
    export class DefaultLogger implements Core.Contracts.ILogger {

        private clientAppProfile: Core.Contracts.IClientAppProfile;

        constructor() {
            this.clientAppProfile = Core.ClientAppProfileManager.getCurrent().getClientAppProfile();
        }

        public logInfo(message: string, additionalInfo?: string, err?: Error): void {
            if (this.clientAppProfile.isDebugMode == true)
                console.trace({ message: message, additionalInfo: additionalInfo, err: err });
        }

        private createLogInfo(message: string, additionalInfo?: string, err?: Error): Model.Dtos.ClientLogDto {

            const logInfo: any = {
                Message: message,
                AdditionalInfo: additionalInfo,
                ClientDate: new Date(),
                ClientWasOnlie: navigator.onLine,
                Route: document.location.pathname
            };

            if (err != null) {
                logInfo.Error = err.message;
                logInfo.StackTrace = err["stack"];
                logInfo.ErrorName = err.name;
            }
            else {
                const fakeErrorToGetStack = new Error();
                logInfo.StackTrace = fakeErrorToGetStack["stack"];
            }

            if (logInfo.StackTrace != null) {
                logInfo.StackTrace = logInfo.StackTrace.replace(/^[^\(]+?[\n$]/gm, "")
                    .replace(/^\s+at\s+/gm, "")
                    .replace(/^Object.<anonymous>\s*\(/gm, "{anonymous}()@");
            }

            return logInfo;
        }

        private lastLoggedError: string = null;

        protected ingoreLog(logInfo: Model.Dtos.ClientLogDto): boolean {
            if (logInfo.ErrorName == "HTTP request failed" /*This type of error is handled at server side*/)
                return true;
            if (logInfo.Error == this.lastLoggedError)
                return true;
            this.lastLoggedError = logInfo.Error;
            return false;
        }

        private sendLogToServer(logInfo: Model.Dtos.ClientLogDto) {

            if (logInfo == null)
                throw new Error("logInfo is null");

            if (this.ingoreLog(logInfo))
                return;

            const logsJson = sessionStorage["logs"];

            let logs = new Array<Model.Dtos.ClientLogDto>();

            if (logsJson != null) {
                logs = JSON.parse(logsJson);
            }

            logs.push(logInfo);

            this.saveLogsToServer(logs);
        }

        private async saveLogsToServer(logs: Array<Model.Dtos.ClientLogDto>): Promise<void> {
            if (navigator.onLine) {
                try {
                    const contextProvider = Core.DependencyManager.getCurrent().resolveObject<ViewModel.Contracts.IEntityContextProvider>("EntityContextProvider");
                    const context = await contextProvider.getContext<FoundationContainer>("Foundation");
                    for (let i = 0; i < logs.length; i++) {
                        context.clientsLogs.add(logs[i]);
                    }
                    await context.saveChanges();
                    sessionStorage.removeItem("logs");
                }
                catch (e) {
                    sessionStorage["logs"] = JSON.stringify(logs);
                }
            }

        }

        public logWarn(message: string, additionalInfo?: string, err?: Error): void {

            const logInfo = this.createLogInfo(message, additionalInfo, err);

            logInfo.LogLevel = "Warning";

            if (this.clientAppProfile.isDebugMode == true)
                console.warn(logInfo);

            this.sendLogToServer(logInfo);
        }

        public logError(message: string, additionalInfo?: string, err?: any): void {

            const logInfo = this.createLogInfo(message, additionalInfo, err);

            logInfo.LogLevel = "Error";

            if (this.clientAppProfile.isDebugMode == true)
                console.error(logInfo);

            this.sendLogToServer(logInfo);
        }

        public logDetailedError(instance: any, methodName: string, args: any, err: Error) {

            if (err != null) {
                if (err["isLogged"] == true)
                    return;
                else
                    err["isLogged"] = true;
            }

            let className = "Unknown";

            if (instance && instance.constructor && instance.constructor.name)
                className = instance.constructor.name;

            let simplifiedArgs = args
                .map(a => {
                    try {
                        return JSON.parse(JSON.stringify(a));
                    }
                    catch (e) {
                        return null;
                    }
                });

            this.logError(`Error at: ${className}:${methodName}`, ` args: ${JSON.stringify(simplifiedArgs)}`, err);
        }

        public logFatal(message: string, additionalInfo?: string, err?: Error): void {

            const logInfo = this.createLogInfo(message, additionalInfo, err);

            logInfo.LogLevel = "Fatal";

            if (this.clientAppProfile.isDebugMode == true)
                console.error(logInfo);

            this.sendLogToServer(logInfo);
        }
    }
}