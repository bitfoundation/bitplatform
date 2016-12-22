module Foundation.ViewModel.Implementations {

    @Core.ObjectDependency({ name: "SecurityService" })
    export class DefaultSecurityService implements Core.Contracts.ISecurityService {

        @Core.Log()
        public isLoggedIn(): boolean {
            if (localStorage["access_token"] == null)
                return false;
            if (localStorage["login_date"] == null)
                return false;
            if (localStorage["expires_in"] == null)
                return false;
            if (((Number(new Date()) - Number(new Date(localStorage["login_date"]))) / 1000) > Number(localStorage["expires_in"]))
                return false;
            return true;
        }

        @Core.Log()
        public login(loginCustomArgs: any): void {
            if (loginCustomArgs == null)
                loginCustomArgs = {};
            loginCustomArgs["pathname"] = location.pathname;
            location.assign(encodeURI("InvokeLogin/" + (JSON.stringify(loginCustomArgs))));
        }

        @Core.Log()
        public logout(): void {
            location.assign(encodeURI("InvokeLogout?id_token=" + localStorage["id_token"]));
        }

    }
}