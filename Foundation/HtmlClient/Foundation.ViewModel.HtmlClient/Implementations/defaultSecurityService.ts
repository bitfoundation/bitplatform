module Foundation.ViewModel.Implementations {

    @Foundation.Core.ObjectDependency({ name: "SecurityService" })
    export class DefaultSecurityService implements Core.Contracts.ISecurityService {

        @Foundation.Core.Log()
        public isLoggedIn(): boolean {
            if (localStorage['access_token'] == null)
                return false;
            if (localStorage['login_date'] == null)
                return false;
            if (localStorage['expires_in'] == null)
                return false;
            if (((Number(new Date()) - Number(new Date(localStorage['login_date']))) / 1000) > Number(localStorage['expires_in']))
                return false;
            return true;
        }

        @Foundation.Core.Log()
        public login(loginCustomArgs: any): void {
            if (loginCustomArgs == null)
                loginCustomArgs = {};
            loginCustomArgs['pathname'] = location.pathname;
            location.pathname = encodeURI("InvokeLogin/" + (JSON.stringify(loginCustomArgs)));
        }

        @Foundation.Core.Log()
        public logout(): void {
            throw new Error('Logout is not implemented yet');
        }

    }
}