/*
based on: https://www.codedesigntips.com/2021/06/28/swagger-ui-with-login-form-and-role-based-api-visibility/
*/
(() => {
    window.addEventListener('load', () => setTimeout(initSignInForm, 0), false);
})();

const ACCESS_TOKEN_COOKIE_NAME = 'access_token';
let accessTokenExpiresIn = 0;

const initSignInForm = () => {
    const swagger = window.ui;
    if (!swagger) {
        console.error('Swagger wasn\'t found');
        return;
    }

    overrideSwaggerAuthorizeEvent(swagger);
    overrideSwaggerLogoutEvent(swagger);
    tryAuthorizeWithLocalData(swagger);
    showSignInUI(swagger);
}

const tryAuthorizeWithLocalData = (swagger) => {
    if (isAuthorized(swagger))
        return;

    const token = getCookie(ACCESS_TOKEN_COOKIE_NAME);
    if (!token)
        return;

    const authorizationObject = getAuthorizationRequestObject(token);
    swagger.authActions.authorize(authorizationObject);
}

const overrideSwaggerAuthorizeEvent = (swagger) => {
    const originalAuthorize = swagger.authActions.authorize;
    swagger.authActions.authorize = async (args) => {
        const result = await originalAuthorize(args);

        if (!getCookie(ACCESS_TOKEN_COOKIE_NAME)) {
            setCookie(ACCESS_TOKEN_COOKIE_NAME, result.payload.bearerAuth.value, accessTokenExpiresIn);
        }

        reloadPage(swagger);
        return result;
    };
}

const overrideSwaggerLogoutEvent = (swagger) => {
    const originalLogout = swagger.authActions.logout;
    swagger.authActions.logout = async (args) => {
        const result = await originalLogout(args);
        removeCookie(ACCESS_TOKEN_COOKIE_NAME);
        reloadPage(swagger);
        return result;
    };
}

const showSignInUI = (swagger) => {
    new MutationObserver(function (mutations, self) {
        const descriptionDiv = isSignInFormMustShow(swagger);
        if (descriptionDiv)
            createSignInUI(swagger, descriptionDiv);
    }).observe(document, {childList: true, subtree: true});
}

const isSignInFormMustShow = (swagger) => {
    const rootDiv = document.querySelector("#swagger-ui > section > div.swagger-ui > div:nth-child(2)");
    if (rootDiv == null)
        return false;

    const informationContainerDiv = rootDiv.querySelector("div.information-container.wrapper");
    if (informationContainerDiv == null)
        return false;

    const descriptionDiv = informationContainerDiv.querySelector("section > div > div > div.description");
    if (descriptionDiv == null)
        return false;

    const signInDiv = descriptionDiv.querySelector("div.signIn");
    if (signInDiv != null)
        return false;

    if (isAuthorized(swagger))
        return false;

    return descriptionDiv;
}

const createSignInUI = function (swagger, rootDiv) {
    const div = document.createElement("div");
    div.className = "signIn";

    rootDiv.appendChild(div);

    //username
    const userNameLabel = document.createElement("label");
    div.appendChild(userNameLabel);

    const userNameSpan = document.createElement("span");
    userNameSpan.innerText = "User";
    userNameLabel.appendChild(userNameSpan);

    const userNameInput = document.createElement("input");
    userNameInput.type = "text";
    userNameInput.placeholder = "test@bitplatform.dev";
    userNameInput.style = "margin-left: 10px; margin-right: 10px;";
    userNameLabel.appendChild(userNameInput);

    //Password
    const passwordLabel = document.createElement("label");
    div.appendChild(passwordLabel);

    const passwordSpan = document.createElement("span");
    passwordSpan.innerText = "Password";
    passwordLabel.appendChild(passwordSpan);

    const passwordInput = document.createElement("input");
    passwordInput.placeholder = "123456";
    passwordInput.type = "password";
    passwordInput.style = "margin-left: 10px; margin-right: 10px;";
    passwordLabel.appendChild(passwordInput);

    //Sign in button
    const signInButton = document.createElement("button")
    signInButton.type = "submit";
    signInButton.type = "button";
    signInButton.classList.add("btn");
    signInButton.classList.add("auth");
    signInButton.classList.add("authorize");
    signInButton.classList.add("button");
    signInButton.innerText = "Sign in";
    signInButton.onclick = function () {
        const userName = userNameInput.value;
        const password = passwordInput.value;

        if (userName === "" || password === "") {
            alert("Insert userName and password!");
            return;
        }

        signIn(swagger, userName, password);
    };

    div.appendChild(signInButton);
}

const signIn = async (swagger, userName, password) => {
    const response = await fetch('/api/Identity/SignIn', {
        headers: { "Content-Type": "application/json; charset=utf-8" },
        method: 'POST',
        body: JSON.stringify({ "userName": userName, "password": password })
    })
    if (response.ok) {
        const result = await response.json();
        const access_token = result.accessToken;
        accessTokenExpiresIn = result.expiresIn;

        const authorizationObject = getAuthorizationRequestObject(access_token);
        swagger.authActions.authorize(authorizationObject);
    } else {
        alert(await response.text())
    }
}

const getAuthorizationRequestObject = (access_token) => {
    return {
        "bearerAuth": {
            "name": "Bearer",
            "schema": {
                "type": "apiKey",
                "description": "JWT Authorization header using the Bearer scheme.",
                "name": "Authorization",
                "in": "header"
            },
            value: access_token
        },
    };
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

function setCookie(name, value, seconds) {
    const date = new Date();
    date.setSeconds(date.getSeconds() + seconds);
    document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/;samesite=strict;`;
}

function getCookie(name) {
    const cookies = document.cookie.split(';');
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].split('=');
        if (trim(cookie[0]) === escape(name)) {
            return unescape(trim(cookie[1]));
        }
    }
    return null;
}

function removeCookie(name) {
    const date = new Date();
    document.cookie = `${name}=;expires=${date.toUTCString()};path=/`;
}

function trim(value) {
    return value.replace(/^\s+|\s+$/g, '');
}