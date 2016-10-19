module Foundation.ViewModel.Implementations {

    @Foundation.Core.Injectable()
    export class SignalRMessageReciever implements Core.Contracts.IMessageReciever {

        public constructor( @Foundation.Core.Inject("$rootScope") public $rootScope: ng.IRootScopeService) {

        }

        private isInited = false;

        private listeners: Array<{ name: string, callbacks: Array<(args?: any) => Promise<void>> }>;

        protected async callListeners(messageKey: string, messageArgs: any) {
            if (messageKey == null)
                throw new Error('messageKey is null');
            const listenerToCall = this.listeners.find(l => l.name.toLowerCase() == messageKey.toLowerCase());
            if (listenerToCall != null) {
                try {
                    for (let callbackIndex = 0; callbackIndex < listenerToCall.callbacks.length; callbackIndex++) {
                        const callback = listenerToCall.callbacks[callbackIndex];
                        await callback(messageArgs);
                    }
                }
                finally {
                    Foundation.ViewModel.ScopeManager.update$scope(this.$rootScope);
                }
            }
        }

        private async ensureInited(): Promise<void> {

            if (this.isInited == true)
                return;

            this.isInited = true;

            this.listeners = [];

            await Core.DependencyManager.getCurrent().resolveFile("signalR");

            const signalRAppPushReciever = this;

            if ($.hubConnection.prototype.createHubProxies == null) {

                $.hubConnection.prototype.createHubProxies = function () {
                    var proxies = {};
                    this.starting(function () {
                        signalRAppPushReciever.registerHubProxies(proxies, true);
                        this._registerSubscribedHubs();
                    }).disconnected(function () {
                        signalRAppPushReciever.registerHubProxies(proxies, false);
                    });

                    proxies["messagesHub"] = this.createHubProxy("messagesHub");
                    proxies["messagesHub"].client = {};
                    proxies["messagesHub"].server = {};

                    return proxies;
                };

                $.signalR.hub = $.hubConnection("signalr", { useDefaultPath: false });

                $.extend($.signalR, $.signalR.hub["createHubProxies"]());

            }

            const messagesHub: any = $.connection["messagesHub"];

            if (messagesHub == null)
                throw new Error('messagesHub is null');

            messagesHub["client"].OnMessageRecieved = async (messageKey: string, messageArgs?: string) => {
                await this.callListeners(messageKey, (messageArgs == null || messageArgs == "") ? null : JSON.parse(messageArgs));
            };

            await $.connection.hub.start({
                transport: ['serverSentEvents', 'webSockets', 'longPolling', 'foreverFrame']
            });

            let isConnected = true;

            $.connection.hub.disconnected(async () => {
                if (isConnected == true) {
                    isConnected = false;
                    await this.callListeners("On-Disconnected", null);
                }
                setTimeout(async () => {
                    await $.connection.hub.start();
                    isConnected = true;
                    await this.callListeners("On-ReNew", null);
                }, 5000);
            });
        }

        @Core.Log()
        public async onMessageRecieved(messageKey: string, callback: (args?: any) => Promise<void>): Promise<void> {

            if (messageKey == null)
                throw new Error('messageKey is null');

            await this.ensureInited();

            let listener = this.listeners.find(l => l.name.toLowerCase() == messageKey.toLowerCase());

            if (listener == null) {
                listener = { name: messageKey, callbacks: [] };
                this.listeners.push(listener);
            }

            listener.callbacks.push(callback);
        }

        @Core.Log()
        public removeCallback(messageKey: string, callbackToRemove: (args?: any) => Promise<void>): void {

            if (this.isInited == false)
                return;

            if (messageKey == null)
                throw new Error('messageKey is null');

            let listener = this.listeners.find(l => l.name.toLowerCase() == messageKey.toLowerCase());

            if (listener != null) {
                let index = listener.callbacks.indexOf(callbackToRemove);
                if (index !== -1) {
                    listener.callbacks.splice(index, 1);
                }
            }
        }

        private makeProxyCallback(hub, callback) {
            return () => {
                callback.apply(hub, $.makeArray(arguments));
            };
        }

        private registerHubProxies(instance, shouldSubscribe: boolean) {

            let key, hub, memberKey, memberValue, subscriptionMethod;

            for (key in instance) {
                if (instance.hasOwnProperty(key)) {

                    hub = instance[key];

                    if (!(hub.hubName)) {
                        continue;
                    }

                    if (shouldSubscribe) {
                        subscriptionMethod = hub.on;
                    } else {
                        subscriptionMethod = hub.off;
                    }

                    for (memberKey in hub.client) {
                        if (hub.client.hasOwnProperty(memberKey)) {
                            memberValue = hub.client[memberKey];

                            if (!$.isFunction(memberValue)) {
                                continue;
                            }

                            subscriptionMethod.call(hub, memberKey, this.makeProxyCallback(hub, memberValue));
                        }
                    }
                }
            }
        }
    }
}