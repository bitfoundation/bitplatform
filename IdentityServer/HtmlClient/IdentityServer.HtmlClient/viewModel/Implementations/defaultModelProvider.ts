/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.core.htmlclient/foundation.core.d.ts" />
module IdentityServer.ViewModel.Implementations {
    export class DefaultModelProvider implements Core.Contracts.IModelProvider {

        private model: IdentityServer.Core.Models.ISsoModel;

        @Foundation.Core.Log()
        public getModel(): IdentityServer.Core.Models.ISsoModel {

            if (this.model == null) {

                let loginModelJson = document.getElementById("modelJson");

                let encodedJson = "";

                if (typeof (loginModelJson.textContent) !== "undefined") {
                    encodedJson = loginModelJson.textContent;
                } else {
                    encodedJson = loginModelJson.innerHTML;
                }

                let json = window["Encoder"].htmlDecode(encodedJson);

                this.model = JSON.parse(json);

                if (this.model.autoRedirect && this.model.redirectUrl) {
                    if (this.model.autoRedirectDelay < 0) {
                        this.model.autoRedirectDelay = 0;
                    }
                    setTimeout(() => {
                        location.assign(this.model.redirectUrl);
                    }, this.model.autoRedirectDelay * 1000);
                }
            }

            return this.model;
        }

    }
}