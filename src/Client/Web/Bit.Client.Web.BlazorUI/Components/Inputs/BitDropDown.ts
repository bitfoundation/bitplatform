class BitDropDown {
    static registerOnScrollEvent(dotnetHelper: any, componentId: string, callback: string) {
        window.addEventListener('scroll', e => {
            var element = document.getElementById(componentId);
            if (element != null) {
                var top = element.offsetTop;
                var left = element.offsetLeft;
                var width = element.offsetWidth;
                var height = element.offsetHeight;
                dotnetHelper.invokeMethodAsync(callback
                    , (top + height) - (window.pageYOffset + window.innerHeight)
                    , (left + width) - (window.pageXOffset + window.innerWidth));
            }
        });
    }
}