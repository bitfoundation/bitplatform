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
        for (var key in options) {
            if (options.hasOwnProperty(key)) {
                options[key] = options[key] === null ? undefined : options[key];
            }
        }
        const notification = new Notification(title, options);
    }

}(BitButil));