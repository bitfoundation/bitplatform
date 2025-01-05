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
        alert('Swagger wasn\'t found');
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
        originalAuthorize(args);

        if (!getCookie(ACCESS_TOKEN_COOKIE_NAME)) {
            const accessToken = args.bearerAuth.value;
            const jwt = parseJwt(accessToken);
            setCookie(ACCESS_TOKEN_COOKIE_NAME, args.bearerAuth.value, parseInt(jwt.exp));
        }

        reloadPage(swagger);
    };
}

function parseJwt(token) {
    var base64Url = token.split('.')[1];
    var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    return JSON.parse(jsonPayload);
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

    //UserName
    const userNameLabel = document.createElement("label");
    div.appendChild(userNameLabel);

    const userNameSpan = document.createElement("span");
    userNameSpan.innerText = "UserName";
    userNameLabel.appendChild(userNameSpan);

    const userNameInput = document.createElement("input");
    userNameInput.type = "text";
    userNameInput.autocomplete = "username";
    userNameInput.placeholder = "test";
    userNameInput.style = "margin-left: 10px; margin-right: 10px;";
    userNameLabel.appendChild(userNameInput);

    //Email
    const emailLabel = document.createElement("label");
    div.appendChild(emailLabel);

    const emailSpan = document.createElement("span");
    emailSpan.innerText = "Email";
    emailLabel.appendChild(emailSpan);

    const emailInput = document.createElement("input");
    emailInput.type = "text";
    emailInput.autocomplete = "email";
    emailInput.placeholder = "test@bitplatform.dev";
    emailInput.style = "margin-left: 10px; margin-right: 10px;";
    emailLabel.appendChild(emailInput);

    //Phone
    const phoneLabel = document.createElement("label");
    div.appendChild(phoneLabel);

    const phoneSpan = document.createElement("span");
    phoneSpan.innerText = "Phone";
    phoneLabel.appendChild(phoneSpan);

    const phoneInput = document.createElement("input");
    phoneInput.type = "text";
    phoneInput.autocomplete = "tel";
    phoneInput.placeholder = "+31684207362";
    phoneInput.style = "margin-left: 10px; margin-right: 10px;";
    phoneLabel.appendChild(phoneInput);

    //Password
    const passwordLabel = document.createElement("label");
    div.appendChild(passwordLabel);

    const passwordSpan = document.createElement("span");
    passwordSpan.innerText = "Password";
    passwordLabel.appendChild(passwordSpan);

    const passwordInput = document.createElement("input");
    passwordInput.placeholder = "123456";
    passwordInput.type = "password";
    passwordInput.autocomplete = "current-password";
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
        const userName = userNameInput.value === "" ? null : userNameInput.value;
        const email = emailInput.value === "" ? null : emailInput.value;
        const phone = phoneInput.value === "" ? null : phoneInput.value;
        const password = passwordInput.value === "" ? null : passwordInput.value;

        if (userName === null && email === null && phone === null) {
            alert("Either provide username, email or phone.");
            return;
        }

        if (password === null) {
            alert("Password is required.");
            return;
        }

        signIn(swagger, userName, email, phone, password);
    };

    div.appendChild(signInButton);
}

const signIn = async (swagger, userName, email, phone, password) => {
    const response = await fetch('/api/Identity/SignIn', {
        headers: { "Content-Type": "application/json; charset=utf-8" },
        method: 'POST',
        body: JSON.stringify({ "userName": userName, "email": email, "phoneNumber": phone, "password": password, deviceInfo: "SwaggerUI" })
    })
    if (response.ok) {
        const result = await response.json();
        if (result.requiresTwoFactor == true) {
            alert("Two factor enabled user is not supported.");
            return;
        }
        const accessToken = result.accessToken;
        accessTokenExpiresIn = result.expiresIn;

        const authorizationObject = getAuthorizationRequestObject(accessToken);
        swagger.authActions.authorize(authorizationObject);
    } else {
        alert(await response.text())
    }
}

const getAuthorizationRequestObject = (accessToken) => {
    return {
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