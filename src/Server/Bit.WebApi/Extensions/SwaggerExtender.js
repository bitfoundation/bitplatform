function bitOnOAuthComplete(data, OAuthSchemeKey) {

    if (data != null && data.access_token != null && data.token_type != null) {

        var now = new Date();
        var time = now.getTime();
        var expireTime = time + (data.expires_in * 1000);
        now.setTime(expireTime);
        var nowAsGMTString = now.toUTCString();

        document.cookie = "access_token=" + data.access_token + ";expires=" + nowAsGMTString + ";path=" + window.swaggerApi.basePath;
        document.cookie = "token_type=" + data.token_type + ";expires=" + nowAsGMTString + ";path=" + window.swaggerApi.basePath;
    }

    return window.onOAuthComplete_original.apply(this, arguments);

}

function bitAccessTokenRequest() {

    if (arguments[0].length == 0)
        arguments[0] = ["openid", "profile", "user_info"];

    return SwaggerUi.Views.AuthView.prototype.accessTokenRequest_original.apply(this, arguments);
}

if (window.onOAuthComplete_original == null) {
    window.onOAuthComplete_original = window.onOAuthComplete;
    window.onOAuthComplete = bitOnOAuthComplete;
}

if (SwaggerUi.Views.AuthView.prototype.accessTokenRequest_original == null) {
    SwaggerUi.Views.AuthView.prototype.accessTokenRequest_original = SwaggerUi.Views.AuthView.prototype.accessTokenRequest;
    SwaggerUi.Views.AuthView.prototype.accessTokenRequest = bitAccessTokenRequest;
}