const gtag = (window as any).googletag = (window as any).googletag || { cmd: [] };

class Ads {
    private static rewardedSlot: any;
    private static rewardPayload: any;
    private static slotReadyEvent: any;
    private static dotnetObj: DotNetObject | undefined;

    public static async init(adUnitPath: string, dotnetObj?: DotNetObject) {
        Ads.dotnetObj = dotnetObj;

        try {
            await Ads.addScripts(['https://securepubads.g.doubleclick.net/tag/js/gpt.js'], true);
        } catch (err) {
            await Ads.dotnetObj?.invokeMethodAsync('ScriptFailed');
            return;
        }
        
        gtag.cmd.push(async () => {
            Ads.rewardedSlot = gtag.defineOutOfPageSlot(adUnitPath, gtag.enums.OutOfPageFormat.REWARDED);
            
            if (!Ads.rewardedSlot) {
                await Ads.dotnetObj?.invokeMethodAsync('AdNotSupported');
                return;
            }
            Ads.rewardedSlot.addService(gtag.pubads());

            gtag.pubads().addEventListener('rewardedSlotReady', async (event: any) => {
                Ads.slotReadyEvent = event;
                await Ads.dotnetObj?.invokeMethodAsync('AdReady');
            });

            gtag.pubads().addEventListener('rewardedSlotClosed', async () => {
                await Ads.dotnetObj?.invokeMethodAsync('AdClosed', Ads.rewardPayload?.amount, Ads.rewardPayload?.type);
                Ads.rewardPayload = null;

                if (Ads.rewardedSlot) {
                    gtag.destroySlots([Ads.rewardedSlot]);
                }
            });

            gtag.pubads().addEventListener('rewardedSlotGranted', async (event: any) => {
                Ads.rewardPayload = event.payload;
                await Ads.dotnetObj?.invokeMethodAsync('AdRewardGranted', Ads.rewardPayload?.amount, Ads.rewardPayload?.type);
            });

            gtag.pubads().addEventListener('slotRenderEnded', async (event: any) => {
                await Ads.dotnetObj?.invokeMethodAsync('AdSlotRendered', event.isEmpty);

                if (event.slot === Ads.rewardedSlot && event.isEmpty) {
                    await Ads.dotnetObj?.invokeMethodAsync('AdNotAvailable');
                }
            });

            gtag.enableServices();
            gtag.display(Ads.rewardedSlot);
        });
    }

    public static async watch() {
        if (!Ads.slotReadyEvent) return;

        Ads.slotReadyEvent.makeRewardedVisible();
        await Ads.dotnetObj?.invokeMethodAsync('AdVisible');
    }

    private static initScriptPromises: { [key: string]: Promise<unknown> } = {};
    public static async addScripts(scripts: string[], isAsync: boolean) {
        const key = scripts.join('|');
        if (Ads.initScriptPromises[key] !== undefined) {
            return Ads.initScriptPromises[key];
        }

        const allScripts = Array.from(document.scripts).map(s => s.src);
        const notAddedScripts = scripts.filter(s => !allScripts.find(as => as.includes(s)));

        if (notAddedScripts.length == 0) return Promise.resolve();

        const promise = new Promise(async (res: any, rej: any) => {
            try {
                await Promise.all(notAddedScripts.map(addScript));
                res();
            } catch (e: any) {
                rej(e);
            }
        });

        Ads.initScriptPromises[key] = promise;
        return promise;

        async function addScript(url: string) {
            return new Promise((res, rej) => {
                const script = document.createElement('script');
                script.src = url;
                if (isAsync) {
                    script.async = true;
                }
                script.onload = res;
                script.onerror = rej;
                document.body.appendChild(script);
            })
        }
    }
}

(window as any).Ads = Ads;