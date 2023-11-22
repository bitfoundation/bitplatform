class App {

    public static goBack() {
        window.history.back();
    }

    public static reloadPage() {
        location.reload();
    }

    public static setBodyStyle(value: string) {
        document.body.setAttribute('style', value);
    }

    public static scrollToContactUs() {
        document.querySelector('#contactus')?.scrollIntoView();
    }

    public static scrollIntoView(id: string) {
        document.getElementById(id)?.scrollIntoView();
    }

    public static goToTop() {
        window.scrollTo({ top: 0 });
    }
}
