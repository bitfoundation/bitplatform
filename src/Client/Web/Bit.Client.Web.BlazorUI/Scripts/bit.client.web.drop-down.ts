class BitDropDown {
    static closeCallout(calloutId: string) {
        var element = document.getElementById(calloutId) as Element;
        Bit.setProperty(element, 'style', 'display:none;');
    }

    static openCallout(calloutId: string) {
        var element = document.getElementById(calloutId) as Element;
        Bit.setProperty(element, 'style', '');
    }
}
document.addEventListener('click', e => {
    var callouts = document.querySelectorAll('.bit-callout');
    callouts.forEach((callout: Element) => {
        BitDropDown.closeCallout(Bit.getProperty(callout, 'id') as string);
    });
});