/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultMetadataProvider implements Contracts.IMetadataProvider {

        private appMetadata: Contracts.AppMetadata = null;

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {

        }

        @Core.Log()
        public async getMetadata(): Promise<Contracts.AppMetadata> {

            if (this.appMetadata == null)
                this.appMetadata = (await (await fetch(`Metadata/V${this.clientAppProfileManager.getClientAppProfile().version}`, { credentials: 'include' })).json()) as any;

            return this.appMetadata;
        }
    }
}