module Foundation.Core {

    export class ClientAppProfileManager {

        private static current = new ClientAppProfileManager();

        public clientAppProfile: Contracts.IClientAppProfile;

        public static getCurrent() {
            return ClientAppProfileManager.current;
        }

        public determinateClientScreenSize(): void {
            /* TODO: Following code is not working properly, should be fixed */
            /*if (clientAppProfile.screenSize == null) {
                const elementToDetectInchScale = document.createElement("div");
                elementToDetectInchScale.style.height = "10in";
                document.body.appendChild(elementToDetectInchScale);
                const pixelPer1In = elementToDetectInchScale.offsetHeight;
                document.body.removeChild(elementToDetectInchScale);
                const inPerPixel = 10 / pixelPer1In;
                const screenHeightInIn = screen.height * inPerPixel;

                if (screenHeightInIn > 7.7) {
                    clientAppProfile.screenSize = "DesktopAndTablet";
                } else {
                    clientAppProfile.screenSize = "Mobile";
                }
            }*/

            if (ClientAppProfileManager.getCurrent().clientAppProfile.screenSize == null || ClientAppProfileManager.getCurrent().clientAppProfile.screenSize == "")
                ClientAppProfileManager.getCurrent().clientAppProfile.screenSize = "DesktopAndTablet";

        }

        public determinateCulture(): void {

            /* Must be extensible without overriding entire clientAppProfileManager or even this method */

            if (ClientAppProfileManager.getCurrent().clientAppProfile.culture == "FaIr") {
                ClientAppProfileManager.getCurrent().clientAppProfile.calendar = "Jalali";
                ClientAppProfileManager.getCurrent().clientAppProfile.direction = "Rtl";
            } else {
                ClientAppProfileManager.getCurrent().clientAppProfile.calendar = "Gregorian";
                ClientAppProfileManager.getCurrent().clientAppProfile.direction = "Ltr";
            }
        }

        public determinateTimeZones(): void {
            if (ClientAppProfileManager.getCurrent().clientAppProfile.currentTimeZone == null || ClientAppProfileManager.getCurrent().clientAppProfile.currentTimeZone == "") {
                ClientAppProfileManager.getCurrent().clientAppProfile.currentTimeZone = String(String(new Date()).split("(")[1]).split(")")[0];
            }
            if (ClientAppProfileManager.getCurrent().clientAppProfile.desiredTimeZone == null || ClientAppProfileManager.getCurrent().clientAppProfile.desiredTimeZone == "") {
                ClientAppProfileManager.getCurrent().clientAppProfile.desiredTimeZone = ClientAppProfileManager.getCurrent().clientAppProfile.currentTimeZone;
            }
        }

        public determinateClientType(): void {

            if (ClientAppProfileManager.getCurrent().clientAppProfile.clientType == null || ClientAppProfileManager.getCurrent().clientAppProfile.clientType == "") {
                ClientAppProfileManager.getCurrent().clientAppProfile.clientType = "Web";
            }
        }

        public init(): void {

            this.clientAppProfile = window['clientAppProfile'];

            if (this.clientAppProfile == null)
                throw new Error('client app profile is null');

            this.clientAppProfile.getConfig = <T>(configKey: string, defaultValueOnNotFound?: T): T => {

                if (configKey == null)
                    throw new Error('config key is null');

                let value: T = null;

                let valueWasFound = false;

                for (let configIndex = 0; configIndex < this.clientAppProfile.environmentConfigs.length; configIndex++) {
                    if (this.clientAppProfile.environmentConfigs[configIndex].key.toLowerCase() == configKey.toLowerCase()) {
                        value = this.clientAppProfile.environmentConfigs[configIndex].value;
                        valueWasFound = true;
                    }
                }

                if (valueWasFound == false) {
                    if (typeof defaultValueOnNotFound != "undefined")
                        value = defaultValueOnNotFound;
                    else
                        throw new Error(`Config named ${configKey} not found`);
                }

                return value;
            };

            this.determinateClientScreenSize();
            this.determinateCulture();
            this.determinateTimeZones();
            this.determinateClientType();
        }

        public getClientAppProfile(): Contracts.IClientAppProfile {
            return this.clientAppProfile;
        }

    }

    let currentAppProfileExtender = ClientAppProfileManager.getCurrent();

    currentAppProfileExtender.init();
}