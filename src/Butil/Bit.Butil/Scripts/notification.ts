var BitButil = BitButil || {};

(function (butil: any) {
    butil.notification = {
        isSupported,
        show,
        getPermission,
        requestPermission
    };

    function isSupported() {
        return ('Notification' in window);
    }

    function show(title: string, options?: NotificationOptions) {
        const notification = new Notification(title, options);
    }

    function getPermission() {
        return Notification.permission;
    }

    async function requestPermission() {
        return await Notification.requestPermission();
    }

}(BitButil));