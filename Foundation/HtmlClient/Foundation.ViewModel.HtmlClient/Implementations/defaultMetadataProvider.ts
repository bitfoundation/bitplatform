/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    export class DefaultMetadataProvider implements Contracts.IMetadataProvider {

        private appMetadata: Contracts.AppMetadata = null;

        @Core.Log()
        public async getMetadata(): Promise<Contracts.AppMetadata> {

            if (this.appMetadata == null)
                this.appMetadata = JSON.parse(await (await fetch(`Metadata/V${Core.ClientAppProfileManager.getCurrent().clientAppProfile.version}`)).text());

            return this.appMetadata;
        }
    }
}