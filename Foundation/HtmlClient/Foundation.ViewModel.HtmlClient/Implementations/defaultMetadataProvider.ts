/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultMetadataProvider implements Contracts.IMetadataProvider {

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {

        }

        private getMetadataPromise: Promise<Contracts.AppMetadata> = null;

        @Core.Log()
        public async getMetadata(): Promise<Contracts.AppMetadata> {

            if (this.getMetadataPromise == null) {

                this.getMetadataPromise = new Promise<Contracts.AppMetadata>(async (resolve, reject) => {

                    let appMetadata: Contracts.AppMetadata = null

                    try {

                        let response = await fetch(`Metadata/V${this.clientAppProfileManager.getClientAppProfile().version}`, { credentials: 'include' });
                        if (response.ok) {
                            appMetadata = await response.json() as any;
                        }
                        else {
                            reject("Error retriving metadata");
                        }
                    }
                    catch (e) {
                        reject(e);
                        throw e;
                    }

                    resolve(appMetadata);

                });

            }

            return this.getMetadataPromise;

        }
    }
}