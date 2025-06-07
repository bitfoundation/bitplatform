// disable auto-zoom of iOS Safari when focusing an input
(/iPad|iPhone|iPod/.test(navigator.userAgent)) &&
    (document.querySelector('meta[name="viewport"]').content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, viewport-fit=cover');

if (window.opener != null && (location.pathname == '/sign-in' || location.pathname == '/sign-up')) {
    // The IExternalNavigationService is responsible for opening pages in a new window,
    // such as during social sign-in flows. Once the external navigation is complete,
    // and the user is redirected back to the newly opened window,
    // the following code ensures that the original window is notified.
    // If IExternalNavigationService fails to navigate to the new window (Typically on iOS/Safari), the window.opener will be null and the page normally loads.
    window.opener.postMessage({ key: 'PUBLISH_MESSAGE', message: 'SOCIAL_SIGN_IN', payload: window.location.href });
    window.close();
}