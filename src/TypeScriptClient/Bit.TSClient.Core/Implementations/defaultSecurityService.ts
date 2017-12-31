module Bit.Implementations {

    export class DefaultSecurityService implements Contracts.ISecurityService {

        @Log()
        public isLoggedIn(): boolean {

            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig("HostVirtualPath", "/");

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
        public async loginWithCredentials(username: string, password: string, client_id: string, client_secret: string, scopes = ["openid", "profile", "user_info"], saveToken = true): Promise<{ access_token: string, expires_in: number, token_type: string }> {

            if (username == null)
                throw new Error("username is null");
            if (password == null)
                throw new Error("password is null");
            if (client_id == null)
                throw new Error("client_id is null");
            if (client_secret == null)
                throw new Error("client_secret is null");

            const loginData = `scope=${scopes.join("+")}&grant_type=password&username=${username}&password=${password}&client_id=${client_id}&client_secret=${client_secret}`;

            const loginHeaders = new Headers({
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "Content-Length": loginData.length.toString(),
            });

            const res = await fetch("core/connect/token", {
                method: "POST", body: loginData, headers: loginHeaders
            });

            const json = await res.json();

            if (res.ok) {

                if (saveToken == true) {

                    const defaultPath = Bit.ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig<string>("HostVirtualPath", "/");
                    const defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.substring(0, defaultPath.length - 1);

                    const now = new Date();
                    const time = now.getTime();
                    const expireTime = time + (json.expires_in * 1000);
                    now.setTime(expireTime);
                    const nowAsGMTString = now.toUTCString();

                    localStorage[`${defaultPath}access_token`] = json.access_token;
                    localStorage[`${defaultPath}expires_in`] = json.expires_in;
                    localStorage[`${defaultPath}login_date`] = now;
                    localStorage[`${defaultPath}token_type`] = json.token_type;
                    localStorage[`${defaultPath}scope`] = scopes.join("%20");

                    document.cookie = `access_token=${json.access_token}` + ";expires=" + nowAsGMTString + `;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;
                    document.cookie = `token_type=${json.token_type}` + ";expires=" + nowAsGMTString + `;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;
                }

                return json;
            }
            else {
                throw new Error(json.error_description || "LoginFailed");
            }
        }

        @Log()
        public logout(): void {
            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig("HostVirtualPath", "/");
            location.assign(encodeURI(`InvokeLogout?id_token=${localStorage[`${path}id_token`]}`));
        }

    }
}