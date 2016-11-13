module Foundation.Core {

    export class ClientAppProfileManager {

        private static current = new ClientAppProfileManager();

        protected clientAppProfile: Contracts.IClientAppProfile;

        public static getCurrent() {
            return ClientAppProfileManager.current;
        }

        public determinateClientScreenSize(): void {

            if (this.clientAppProfile.screenSize == null || this.clientAppProfile.screenSize == "")
                this.clientAppProfile.screenSize = "DesktopAndTablet";

        }

        public determinateCulture(): void {

            /* Must be extensible without overriding entire clientAppProfileManager or even this method */

            if (this.clientAppProfile.culture == "FaIr") {
                this.clientAppProfile.calendar = "Jalali";
                this.clientAppProfile.direction = "Rtl";
            } else {
                this.clientAppProfile.calendar = "Gregorian";
                this.clientAppProfile.direction = "Ltr";
            }
        }

        public determinateTimeZones(): void {
            if (this.clientAppProfile.currentTimeZone == null || this.clientAppProfile.currentTimeZone == "") {
                this.clientAppProfile.currentTimeZone = String(String(new Date()).split("(")[1]).split(")")[0];
            }
            if (this.clientAppProfile.desiredTimeZone == null || this.clientAppProfile.desiredTimeZone == "") {
                this.clientAppProfile.desiredTimeZone = this.clientAppProfile.currentTimeZone;
            }
        }

        public determinateClientType(): void {

            if (this.clientAppProfile.clientType == null || this.clientAppProfile.clientType == "") {
                this.clientAppProfile.clientType = "Web";
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