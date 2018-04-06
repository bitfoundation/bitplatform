module Bit.Implementations {

    export class DefaultSecurityService implements Contracts.ISecurityService {

        @Log()
        public isLoggedIn(): boolean {

            const token = this.getCurrentToken();

            if (token == null)
                return false;

            return ((Number(new Date()) - Number(token.login_date)) / 1000) < token.expires_in;
        }

        @Log()
        public login(state?: any, client_id?: string): void {
            location.assign(this.getLoginUrl(state, client_id));
        }

        public getLoginUrl(state?: any, client_id?: string): string {
            if (state == null)
                state = {};
            state["pathname"] = location.pathname;
            let url = `InvokeLogin?state=${JSON.stringify(state)}`;
            if (client_id != null)
                url += `&client_id${client_id}`;
            return encodeURI(url);
        }

        @Log()
        public getCurrentToken(): Contracts.Token {

            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig("HostVirtualPath", "/");

            if (localStorage[`${path}access_token`] == null)
                return null;
            if (localStorage[`${path}login_date`] == null)
                return null;
            if (localStorage[`${path}expires_in`] == null)
                return null;

            return {
                access_token: localStorage[`${path}access_token`],
                expires_in: Number(localStorage[`${path}expires_in`]),
                login_date: new Date(localStorage[`${path}login_date`]),
                token_type: localStorage[`${path}token_type`],
                id_token: localStorage[`${path}id_token`]
            };
        }

        @Log()
        public async loginWithCredentials(username: string, password: string, client_id: string, client_secret: string, scopes = ["openid", "profile", "user_info"]): Promise<Contracts.Token> {

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

            const json: Contracts.Token & { error_description: string } = await res.json();

            if (res.ok) {

                json.login_date = new Date();

                const defaultPath = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig<string>("HostVirtualPath", "/");
                const defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.substring(0, defaultPath.length - 1);

                const expiresTimeInSeconds = json.expires_in;
                const expiresDate = new Date();
                expiresDate.setTime(expiresDate.getTime() + (expiresTimeInSeconds * 1000));
                const expiresDateAsUTCString = expiresDate.toUTCString();

                localStorage[`${defaultPath}access_token`] = json.access_token;
                localStorage[`${defaultPath}expires_in`] = json.expires_in;
                localStorage[`${defaultPath}login_date`] = json.login_date;
                localStorage[`${defaultPath}token_type`] = json.token_type;
                localStorage[`${defaultPath}scope`] = scopes.join("%20");

                document.cookie = `access_token=${json.access_token}` + ";expires=" + expiresDateAsUTCString + `;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;
                document.cookie = `token_type=${json.token_type}` + ";expires=" + expiresDateAsUTCString + `;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;

                return json;
            }
            else {
                throw new Error(json.error_description || "LoginFailed");
            }
        }

        @Log()
        public logout(state?: any, client_id?: string): void {
            const token = this.getCurrentToken();
            if (token != null && token.id_token != null) {
                location.assign(this.getLogoutUrl(token.id_token, state, client_id));
            }
            else {

                const defaultPath = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig<string>("HostVirtualPath", "/");
                const defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.substr(0, defaultPath.length - 1);

                localStorage.removeItem(`${defaultPath}access_token`);
                localStorage.removeItem(`${defaultPath}expires_in`);
                localStorage.removeItem(`${defaultPath}login_date`);
                localStorage.removeItem(`${defaultPath}scope`);
                localStorage.removeItem(`${defaultPath}token_type`);

                const cookies = document.cookie.split('; ');

                for (let i = 0; i < cookies.length; i++) {
                    const cookie = cookies[i];
                    const eqPos = cookie.indexOf('=');
                    const name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                    if (name == 'access_token' || name == 'token_type')
                        document.cookie = name + `=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;
                }
            }
        }

        public getLogoutUrl(id_token: string, state?: any, client_id?: string): string {
            if (state == null)
                state = {};
            state["pathname"] = location.pathname;
            let url = `InvokeLogout?state=${JSON.stringify(state)}&id_token=${id_token}`;
            if (client_id != null)
                url += `&client_id${client_id}`;
            return encodeURI(url);
        }
    }
}