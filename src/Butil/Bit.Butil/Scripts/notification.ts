var BitButil = BitButil || {};

(function (butil: any) {
    butil.notification = {
        isSupported,
        getPermission,
        requestPermission,
        show,
    };

    function isSupported() {
        return ('Notification' in window);
    }

    function getPermission() {
        return Notification.permission;
    }

    async function requestPermission() {
        return await Notification.requestPermission();
    }

    function show(title: string, options?: NotificationOptions) {
        for (const key in options) {
            if (options.hasOwnProperty(key)) {
                options[key] = options[key] === null ? undefined : options[key];
            }
        }

        try {
            const notification = new Notification(title, options);
        } catch (e) {
            navigator.serviceWorker?.getRegistration().then(reg => {
                reg.showNotification(title, options);
            });
        }
    }

}(BitButil));