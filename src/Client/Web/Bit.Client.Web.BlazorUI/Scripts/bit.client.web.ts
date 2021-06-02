
class Bit {
    static setProperty(element: { [key: string]: any }, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: { [key: string]: any }, property: string): string | null {
        return element[property];
    }

    static getHeight(selector: string): number | null {
        const element = document.querySelector(selector);
        let height = null;
        if (element) {
            height = element.clientHeight;
            return height;
        }
        else {
            return null;
        }
    }
    static addClass(selector: string, className: string ) {

        const element = document.querySelector(selector);
        const childElements = element?.children;
        if (childElements) {
            for (let i = 0; i < childElements.length; i++) {
                const child = childElements[i];
                child.className +=' '+ className;
            }
        }
    }
}
