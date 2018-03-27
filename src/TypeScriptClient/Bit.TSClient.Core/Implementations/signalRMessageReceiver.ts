module Bit.Implementations {

    export class SignalRMessageReceiver implements Contracts.IMessageReceiver {

        private _isInited = false;
        private _isConnected = false;
        private _stayConnected = false;

        @Log()
        public async stop(): Promise<void> {
            this._stayConnected = false;
            if ($.signalR != null && $.connection != null && this._isInited == true)
                $.connection.hub.stop(true, true);
        }

        public async start(config: { preferWebSockets?: boolean } = { preferWebSockets: false }): Promise<void> {
            this._stayConnected = true;
            await this.getInitPromise();
            await new Promise<void>((res, rej) => {
                return $.connection.hub.start({
                    transport: config.preferWebSockets == true ? ["webSockets", "serverSentEvents"] : ["serverSentEvents", "webSockets"]
                }).then(() => res()).fail((e) => rej(e));
            });
            this._isConnected = true;
        }

        protected async callListeners(messageKey: string, messageArgs: any) {

            if (messageKey == null)
                throw new Error("messageKey is null");

            PubSub.publish(messageKey, messageArgs);
        }

        private initPromise = null;

        private getInitPromise(): Promise<void> {

            if (this.initPromise == null) {

                this.initPromise = new Promise<void>(async (resolve, reject) => {

                    try {
                        if (this._isInited == true)
                            return;

                        if (typeof ($) == "undefined")
                            reject("jQuery is not present");

                        if ($.signalR == null)
                            reject("SignalR is not present");

                        if (typeof (PubSub) == "undefined")
                            reject("PubSub is not present");

                        this._isInited = true;

                        const signalRAppPushReceiver = this;

                        if ($.hubConnection.prototype.createHubProxies == null) {

                            $.hubConnection.prototype.createHubProxies = function createHubProxies() {
                                var proxies = {};
                                this.starting(function onStarting() {
                                    signalRAppPushReceiver.registerHubProxies(proxies, true);
                                    this._registerSubscribedHubs();
                                }).disconnected(function onDisconnected() {
                                    signalRAppPushReceiver.registerHubProxies(proxies, false);
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
                            reject("messagesHub is null");

                        messagesHub["client"].OnMessageReceived = async (messageKey: string, messageArgs?: string) => {
                            await this.callListeners(messageKey, (messageArgs == null || messageArgs == "") ? null : JSON.parse(messageArgs));
                        };

                        $.connection.hub.disconnected(async () => {
                            if (this._isConnected == true) {
                                this._isConnected = false;
                                await this.callListeners("On-Disconnected", null);
                            }
                            if (this._stayConnected == true) {
                                setTimeout(async () => {
                                    await this.start();
                                    this._isConnected = true;
                                    await this.callListeners("On-ReNew", null);
                                }, 5000);
                            }
                        });
                    }
                    catch (e) {
                        reject(e);
                        throw e;
                    }

                    resolve();
                });
            }

            return this.initPromise;

        }

        private makeProxyCallback(hub, callback) {
            return function () {
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