module Bit.Implementations {

    export class DefaultSecurityService implements Contracts.ISecurityService {

        @Log()
        public isLoggedIn(): boolean {

            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig("ClientHostVirtualPath", "/");

            if (localStorage[`${path}access_token`] == null)
                return false;
            if (localStorage[`${path}login_date`] == null)
                return false;
            if (localStorage[`${path}expires_in`] == null)
                return false;
            if (((Number(new Date()) - Number(new Date(localStorage[`${path}login_date`]))) / 1000) > Number(localStorage[`${path}expires_in`]))
                return false;
            return true;
        }

        @Log()
        public login(loginCustomArgs?: any): void {
            if (loginCustomArgs == null)
                loginCustomArgs = {};
            loginCustomArgs["pathname"] = location.pathname;
            location.assign(encodeURI(`InvokeLogin/${JSON.stringify(loginCustomArgs)}`));
        }

        @Log()
        public logout(): void {
            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig("ClientHostVirtualPath", "/");
            location.assign(encodeURI(`InvokeLogout?id_token=${localStorage[`${path}id_token`]}`));
        }

    }
}