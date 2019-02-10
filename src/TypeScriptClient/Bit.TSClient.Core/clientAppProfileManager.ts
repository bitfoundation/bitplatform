module Bit {

    export class ClientAppProfileManager {

        private static current = new ClientAppProfileManager();

        protected clientAppProfile: Contracts.IClientAppProfile;

        public static getCurrent() {
            return ClientAppProfileManager.current;
        }

        public determinateClientScreenSize(): void {

            if (this.clientAppProfile.screenSize == null || this.clientAppProfile.screenSize == "") {
                this.clientAppProfile.screenSize = "DesktopAndTablet";
            }

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

        private isAllASCII(str: string): boolean {
            return /^[\x00-\x7F]*$/.test(str);
        }

        private currentTimeZoneHasValue(): boolean {
            return this.clientAppProfile.currentTimeZone != null && this.clientAppProfile.currentTimeZone != "";
        }

        public determinateTimeZones(): void {
            if (!this.currentTimeZoneHasValue()) {
                const date = new Date();
                try {
                    if (typeof (date.toLocaleDateString) == "undefined")
                        throw new Error("Browser has no support for toLocaleDateString. Go to catch to try another apporach!");

                    this.clientAppProfile.currentTimeZone = date.toLocaleDateString("en-US", { timeZoneName: "long" }).split(", ")[1]; // Ignore browser's current culture if it supports locales & options in its toLocaleDateString method.

                    if (!this.currentTimeZoneHasValue())
                        throw new Error("Browser has no support for options in toLocaleDateString method. Go to catch to try another apporach!"); // Old browsers may return "MM/DD/YYYY" instead of "MM/DD/YYYY, TimeZoneLongName" due lack of support for { timeZoneName: "long" } option.
                }
                catch (e) {
                    this.clientAppProfile.currentTimeZone = String(String(date).split("(")[1]).split(")")[0]; // It returns invalid value in non en-US browsers!
                }

                if (this.currentTimeZoneHasValue() && !this.isAllASCII(this.clientAppProfile.currentTimeZone))
                    this.clientAppProfile = null; // We're unable to find time zone name's name in en-US culture. Having no currentTimeZone is better than having currentTimeZone with invalid value!
            }
            if ((this.clientAppProfile.desiredTimeZone == null || this.clientAppProfile.desiredTimeZone == "") && this.currentTimeZoneHasValue()) {
                this.clientAppProfile.desiredTimeZone = this.clientAppProfile.currentTimeZone;
            }
        }

        public determinateClientType(): void {

            if (this.clientAppProfile.clientType == null || this.clientAppProfile.clientType == "") {
                this.clientAppProfile.clientType = "Browser";
            }
        }

        public init(): void {

            this.clientAppProfile = clientAppProfile;

            if (this.clientAppProfile == null) {
                throw new Error("client app profile is null");
            }

            this.clientAppProfile.getConfig = <T>(configKey: string, defaultValueOnNotFound?: T): T => {

                if (configKey == null) {
                    throw new Error("config key is null");
                }

                let value: T = null;

                let valueWasFound = false;

                for (let configIndex = 0; configIndex < this.clientAppProfile.environmentConfigs.length; configIndex++) {
                    if (this.clientAppProfile.environmentConfigs[configIndex].key.toLowerCase() == configKey.toLowerCase()) {
                        value = this.clientAppProfile.environmentConfigs[configIndex].value;
                        valueWasFound = true;
                    }
                }

                if (valueWasFound == false) {
                    if (defaultValueOnNotFound != null) {
                        value = defaultValueOnNotFound;
                    } else {
                        throw new Error(`Config named ${configKey} not found`);
                    }
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