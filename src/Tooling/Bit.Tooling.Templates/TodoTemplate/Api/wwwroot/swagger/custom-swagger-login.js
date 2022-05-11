(function ()
{
    const overrider = () =>
    {
        const swagger = window.ui;
        if (!swagger)
        {
            console.error('Swagger wasn\'t found');
            return;
        }

        ensureAuthorization(swagger);
        reloadSchemaOnAuth(swagger);
        clearInputPlaceHolder(swagger);
        showLoginUI(swagger);
    }

    const getAuthorization = (swagger) => swagger.auth()._root.entries.find(e => e[0] === 'authorized');
    const isAuthorized = (swagger) =>
    {
        const auth = getAuthorization(swagger);
        return auth && auth[1].size !== 0;
    };

    const ensureAuthorization = (swagger) =>
    {
        const getBearer = () =>
        {
            const auth = getAuthorization(swagger);
            const def = auth[1]._root.entries.find(e => e[0] === 'bearerAuth');
            if (!def)
                return undefined;

            const token = def[1]._root.entries.find(e => e[0] === 'value');
            if (!token[1])
                return undefined;

            return token[1];
        }

        const fetch = swagger.fn.fetch;
        swagger.fn.fetch = (req) =>
        {
            if (!req.headers.Authorization && isAuthorized(swagger))
            {
                const bearer = getBearer();
                if (bearer)
                {
                    req.headers.Authorization = bearer;
                }
            }
            return fetch(req);
        }
    };
    const reloadSchemaOnAuth = (swagger) =>
    {
        const getCurrentUrl = () =>
        {
            const spec = swagger.getState()._root.entries.find(e => e[0] === 'spec');
            if (!spec)
                return undefined;

            const url = spec[1]._root.entries.find(e => e[0] === 'url');
            if (!url)
                return undefined;

            return url[1];
        }
        const reload = () =>
        {
            const url = getCurrentUrl();
            if (url)
            {
                swagger.specActions.download(url);
            }
        };

        const handler = (caller, args) =>
        {
            const result = caller(args);
            if (result.then)
            {
                result.then(() => reload())
            }
            else
            {
                reload();
            }
            return result;
        }

        const auth = swagger.authActions.authorize;
        swagger.authActions.authorize = (args) => handler(auth, args);
        const logout = swagger.authActions.logout;
        swagger.authActions.logout = (args) => handler(logout, args);
    };
    
    const clearInputPlaceHolder = (swagger) =>
    {
        //https://github.com/api-platform/core/blob/main/src/Bridge/Symfony/Bundle/Resources/public/init-swagger-ui.js#L6-L41
        new MutationObserver(function (mutations, self)
        {
            var elements = document.querySelectorAll("input[type=text]");
            for (var i = 0; i < elements.length; i++)
                elements[i].placeholder = "";
        }).observe(document, { childList: true, subtree: true });
    }
    
    const showLoginUI = (swagger) =>
    {
        //https://github.com/api-platform/core/blob/main/src/Bridge/Symfony/Bundle/Resources/public/init-swagger-ui.js#L6-L41
        new MutationObserver(function (mutations, self)
        {
            var rootDiv = document.querySelector("#swagger-ui > section > div.swagger-ui > div:nth-child(2)");
            if (rootDiv == null)
                return;

            var informationContainerDiv = rootDiv.querySelector("div.information-container.wrapper");
            if (informationContainerDiv == null)
                return;

            var descriptionDiv = informationContainerDiv.querySelector("section > div > div > div.description");
            if (descriptionDiv == null)
                return;

            var loginDiv = descriptionDiv.querySelector("div.login");
            if (loginDiv != null)
                return;

            if (isAuthorized(swagger))
                return;

            createLoginUI(descriptionDiv);

        }).observe(document, { childList: true, subtree: true });
        
        const createLoginUI = function (rootDiv)
        {
            var div = document.createElement("div");
            div.className = "login";

            rootDiv.appendChild(div);

            //UserName
            var userNameLabel = document.createElement("label");
            div.appendChild(userNameLabel);

            var userNameSpan = document.createElement("span");
            userNameSpan.innerText = "User";
            userNameLabel.appendChild(userNameSpan);

            var userNameInput = document.createElement("input");
            userNameInput.type = "text";
            userNameInput.style = "margin-left: 10px; margin-right: 10px;";
            userNameLabel.appendChild(userNameInput);

            //Password
            var passwordLabel = document.createElement("label");
            div.appendChild(passwordLabel);

            var passwordSpan = document.createElement("span");
            passwordSpan.innerText = "Password";
            passwordLabel.appendChild(passwordSpan);

            var passwordInput = document.createElement("input");
            passwordInput.type = "password";
            passwordInput.style = "margin-left: 10px; margin-right: 10px;";
            passwordLabel.appendChild(passwordInput);

            //Login button
            var loginButton = document.createElement("button")
            loginButton.type = "submit";
            loginButton.type = "button";
            loginButton.classList.add("btn");
            loginButton.classList.add("auth");
            loginButton.classList.add("authorize");
            loginButton.classList.add("button");
            loginButton.innerText = "Login";
            loginButton.onclick = function ()
            {
                var userName = userNameInput.value;
                var password = passwordInput.value;

                if (userName === "" || password === "")
                {
                    alert("Insert userName and password!");
                    return;
                }

                login(userName, password);
            };

            div.appendChild(loginButton);
        }
       
        const login = function (userName, password)
        {
            var xhr = new XMLHttpRequest();

            xhr.onreadystatechange = function ()
            {
                if (xhr.readyState === XMLHttpRequest.DONE)
                {
                    if (xhr.status === 200 || xhr.status === 400)
                    {
                        var response = JSON.parse(xhr.responseText);
                     
                        if (response.exceptionType)
                        {
                            alert(response.message);
                            return;
                        }

                        var accessToken = response.accessToken;

                        var obj = {
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
                    }
                    else
                    {
                        alert('error ' + xhr.status);
                    }
                }
            };

            xhr.open("POST", "/api/Auth/SignIn", true);
            xhr.setRequestHeader("Content-Type", "application/json");

            var json = JSON.stringify({ "userName": userName, "password": password });
           
            xhr.send(json);
        }
    }

    // append to event right after SwaggerUIBundle initialized
    window.addEventListener('load', () => setTimeout(overrider, 0), false);
}());