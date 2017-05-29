/// <reference path="../../foundation.core.htmlclient/foundation.core.d.ts" />
module Foundation.ViewModel.Implementations {
    @Core.Injectable()
    export class DefaultMetadataProvider implements Contracts.IMetadataProvider {

        public constructor( @Core.Inject("ClientAppProfileManager") public clientAppProfileManager: Core.ClientAppProfileManager) {

        }

        private getMetadataPromise: Promise<Contracts.AppMetadata> = null;
        private _appMetadata: Contracts.AppMetadata = null;

        @Core.Log()
        public getMetadataSync(): Contracts.AppMetadata {

            if (this._appMetadata == null)
                throw new Error("Can't load metadata sync");

            return this._appMetadata;
        }

        @Core.Log()
        public async getMetadata(): Promise<Contracts.AppMetadata> {

            if (this.getMetadataPromise == null) {

                this.getMetadataPromise = new Promise<Contracts.AppMetadata>(async (resolve, reject) => {

                    this._appMetadata = null;

                    try {

                        const response = await fetch(`Metadata/V${this.clientAppProfileManager.getClientAppProfile().version}`, { credentials: "include" });
                        if (response.ok) {
                            this._appMetadata = await response.json() as any;
                        }
                        else {
                            reject("Error retriving metadata");
                        }
                    } catch (e) {
                        reject(e);
                        throw e;
                    }

                    resolve(this._appMetadata);

                });

            }

            return this.getMetadataPromise;

        }
    }
}