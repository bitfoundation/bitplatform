/// <reference path="../../../../../../bit-framework/Foundation/htmlclient/foundation.core.htmlclient/foundation.core.d.ts" />
module IdentityServer.Core.Contracts {
    export interface IModelProvider {
        getModel(): Core.Models.ISsoModel;
    }
}