module Bit.Implementations {

    export class DefaultSecurityService implements Contracts.ISecurityService {

        @Log()
        public isLoggedIn(): boolean {

            const token = this.getCurrentToken();

            if (token == null) {
                return false;
            }

            return ((Number(new Date()) - Number(token.login_date)) / 1000) < token.expires_in;
        }

        @Log()
        public login(state?: any, client_id?: string, acr_values?: { key: string, value: string }[]): void {
            location.assign(this.getLoginUrl(state, client_id, acr_values));
        }

        public getLoginUrl(state?: any, client_id?: string, acr_values?: { key: string, value: string }[]): string {

            const loginData: Array<string> = [];

            if (client_id != null) {
                loginData.push("client_id" + "=" + encodeURIComponent(client_id));
            }

            if (acr_values != null) {
                loginData.push("acr_values" + "=" + encodeURIComponent(acr_values.map(p => `${p.key}:${escape(p.value)}`).join(' ')));
            }

            if (state == null) {
                state = {};
            }

            state["pathname"] = location.pathname;

            loginData.push("state" + "=" + encodeURIComponent(JSON.stringify(state)));

            return `InvokeLogin?${loginData.join("&")}`;
        }

        @Log()
        public getCurrentToken(): Contracts.Token {

            let path = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig<string>("HostVirtualPath", "/");

            if (localStorage[`${path}access_token`] == null) {
                return null;
            }
            if (localStorage[`${path}login_date`] == null) {
                return null;
            }
            if (localStorage[`${path}expires_in`] == null) {
                return null;
            }

            return {
                access_token: localStorage[`${path}access_token`],
                expires_in: Number(localStorage[`${path}expires_in`]),
                login_date: new Date(localStorage[`${path}login_date`]),
                token_type: localStorage[`${path}token_type`],
                id_token: localStorage[`${path}id_token`]
            };
        }

        @Log()
        public async loginWithCredentials(userName: string, password: string, client_id: string, client_secret: string, acr_values: { key: string, value: string }[] = null, scopes = ["openid", "profile", "user_info"]): Promise<Contracts.Token> {

            if (userName == null) {
                throw new Error("userName is null");
            }
            if (password == null) {
                throw new Error("password is null");
            }
            if (client_id == null) {
                throw new Error("client_id is null");
            }
            if (client_secret == null) {
                throw new Error("client_secret is null");
            }

            const loginData: Array<string> = [];

            loginData.push("scope" + "=" + encodeURIComponent(scopes.join(" ")));
            loginData.push("grant_type" + "=" + "password");
            loginData.push("username" + "=" + encodeURIComponent(userName));
            loginData.push("password" + "=" + encodeURIComponent(password));
            loginData.push("client_id" + "=" + encodeURIComponent(client_id));
            loginData.push("client_secret" + "=" + encodeURIComponent(client_secret));

            if (acr_values != null) {
                loginData.push("acr_values" + "=" + encodeURIComponent(acr_values.map(p => `${p.key}:${escape(p.value)}`).join(' ')));
            }

            const loginDataStr = loginData.join("&");

            const loginHeaders = new Headers({
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "Content-Length": loginDataStr.toString(),
            });

            const res = await fetch("core/connect/token", {
                method: "POST", body: loginDataStr, headers: loginHeaders, credentials: "include"
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
            } else {
                throw new Error(json.error_description || "LoginFailed");
            }
        }

        @Log()
        public logout(state?: any, client_id?: string): void {
            const token = this.getCurrentToken();
            if (token?.id_token != null) {
                location.assign(this.getLogoutUrl(token.id_token, state, client_id));
            } else {

                const defaultPath = ClientAppProfileManager.getCurrent().getClientAppProfile().getConfig<string>("HostVirtualPath", "/");
                const defaultPathWithoutEndingSlashIfIsNotRoot = defaultPath == "/" ? defaultPath : defaultPath.substr(0, defaultPath.length - 1);

                localStorage.removeItem(`${defaultPath}access_token`);
                localStorage.removeItem(`${defaultPath}expires_in`);
                localStorage.removeItem(`${defaultPath}login_date`);
                localStorage.removeItem(`${defaultPath}scope`);
                localStorage.removeItem(`${defaultPath}token_type`);

                const cookies = document.cookie.split("; ");

                for (let i = 0; i < cookies.length; i++) {
                    const cookie = cookies[i];
                    const eqPos = cookie.indexOf("=");
                    const name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
                    if (name == "access_token" || name == "token_type") {
                        document.cookie = name + `=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=${defaultPathWithoutEndingSlashIfIsNotRoot}`;
                    }
                }
            }
        }

        public getLogoutUrl(id_token: string, state?: any, client_id?: string): string {
            if (state == null) {
                state = {};
            }
            state["pathname"] = location.pathname;
            let url = `InvokeLogout?state=${JSON.stringify(state)}&id_token=${id_token}`;
            if (client_id != null) {
                url += `&client_id${client_id}`;
            }
            return encodeURI(url);
        }

        @Log()
        public getCurrentBitJwtToken(): Contracts.BitJwtToken {
            const token = this.getCurrentToken();
            return JSON.parse(this.parseJwt(token.access_token).primary_sid);
        }

        private b64DecodeUnicode(input: string): string {
            return decodeURIComponent(
                Array.prototype.map.call(atob(input), c =>
                    '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
                ).join(''));
        }

        private parseJwt(input: string): { primary_sid: string } {
            return JSON.parse(
                this.b64DecodeUnicode(
                    input.split('.')[1].replace('-', '+').replace('_', '/')
                )
            )
        }
    }
}
