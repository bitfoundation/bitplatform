/*
base on: https://www.codedesigntips.com/2021/06/28/swagger-ui-with-login-form-and-role-based-api-visibility/
*/
(() => {
    window.addEventListener('load', () => setTimeout(initLoginForm, 0), false);
})();

const initLoginForm = () => {
    const swagger = window.ui;
    if (!swagger) {
        console.error('Swagger wasn\'t found');
        return;
    }

    overrideSwaggerAuthorizeEvent(swagger);
    overrideSwaggerLogoutEvent(swagger);
    showLoginUI(swagger);
}

const overrideSwaggerAuthorizeEvent = (swagger) => {
    const auth = swagger.authActions.authorize;
    swagger.authActions.authorize = (args) => attachReloadPageToEvent(swagger, auth, args);
}

const overrideSwaggerLogoutEvent = (swagger) => {
    const logout = swagger.authActions.logout;
    swagger.authActions.logout = (args) => attachReloadPageToEvent(swagger, logout, args);
}

const showLoginUI = (swagger) => {
    new MutationObserver(function (mutations, self) {
        const descriptionDiv = isLoginFormMustShow(swagger);
        if (descriptionDiv)
            createLoginUI(swagger, descriptionDiv);
    }).observe(document, {childList: true, subtree: true});
}

const isLoginFormMustShow = (swagger) => {
    const rootDiv = document.querySelector("#swagger-ui > section > div.swagger-ui > div:nth-child(2)");
    if (rootDiv == null)
        return false;

    const informationContainerDiv = rootDiv.querySelector("div.information-container.wrapper");
    if (informationContainerDiv == null)
        return false;

    const descriptionDiv = informationContainerDiv.querySelector("section > div > div > div.description");
    if (descriptionDiv == null)
        return false;

    const loginDiv = descriptionDiv.querySelector("div.login");
    if (loginDiv != null)
        return false;

    if (isAuthorized(swagger))
        return false;

    return descriptionDiv;
}

const createLoginUI = function (swagger, rootDiv) {
    const div = document.createElement("div");
    div.className = "login";

    rootDiv.appendChild(div);

    //UserName
    const userNameLabel = document.createElement("label");
    div.appendChild(userNameLabel);

    const userNameSpan = document.createElement("span");
    userNameSpan.innerText = "User";
    userNameLabel.appendChild(userNameSpan);

    const userNameInput = document.createElement("input");
    userNameInput.type = "text";
    userNameInput.style = "margin-left: 10px; margin-right: 10px;";
    userNameLabel.appendChild(userNameInput);

    //Password
    const passwordLabel = document.createElement("label");
    div.appendChild(passwordLabel);

    const passwordSpan = document.createElement("span");
    passwordSpan.innerText = "Password";
    passwordLabel.appendChild(passwordSpan);

    const passwordInput = document.createElement("input");
    passwordInput.type = "password";
    passwordInput.style = "margin-left: 10px; margin-right: 10px;";
    passwordLabel.appendChild(passwordInput);

    //Login button
    const loginButton = document.createElement("button")
    loginButton.type = "submit";
    loginButton.type = "button";
    loginButton.classList.add("btn");
    loginButton.classList.add("auth");
    loginButton.classList.add("authorize");
    loginButton.classList.add("button");
    loginButton.innerText = "Login";
    loginButton.onclick = function () {
        const userName = userNameInput.value;
        const password = passwordInput.value;

        if (userName === "" || password === "") {
            alert("Insert userName and password!");
            return;
        }

        login(swagger, userName, password);
    };

    div.appendChild(loginButton);
}

const login = async (swagger, userName, password) => {
    const response = await fetch('/api/Auth/SignIn', {
        headers: {"Content-Type": "application/json; charset=utf-8"},
        method: 'POST',
        body: JSON.stringify({"userName": userName, "password": password})
    })
    if (response.ok) {
        const result = await response.json();
        const accessToken = result.accessToken;

        const obj = {
            "bearerAuth": {
                "name": "Bearer",
                "schema": {
                    "type": "apiKey",
                    "description": "JWT Authorization header using the Bearer scheme.",
                    "name": "Authorization",
                    "in": "header"
                },
                value: accessToken
            },
        };
        swagger.authActions.authorize(obj);
    } else {
        alert(await response.text())
    }
}

const attachReloadPageToEvent = async (swagger, caller, args) => {
    const result = await caller(args);
    reloadPage(swagger);
    return result;
}

const getCurrentUrl = (swagger) => {
    const spec = swagger.getState()._root.entries.find(e => e[0] === 'spec');
    if (!spec)
        return undefined;

    const url = spec[1]._root.entries.find(e => e[0] === 'url');
    if (!url)
        return undefined;

    return url[1];
}

const reloadPage = (swagger) => {
    const url = getCurrentUrl(swagger);
    if (url) {
        swagger.specActions.download(url);
    }
};

function getAuthorization(swagger) {
    return swagger.auth()._root.entries.find(e => e[0] === 'authorized');
}

function isAuthorized(swagger) {
    const auth = getAuthorization(swagger);
    return auth && auth[1].size !== 0;
}