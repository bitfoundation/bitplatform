
class Bit {
    static setProperty(element: { [key: string]: any }, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: { [key: string]: any }, property: string): string | null {
        return element[property];
    }

    static getHeight(selector: string): number | null {
        console.log("start get ");
        const element = document.querySelector(selector);
        console.log("element:", element);
        let height = 0;
        if (element) {
            height = element?.clientHeight;
            console.log("height :", height );
            return height;
        }
        else {
            return 0;
        }
    }
    static addClass(selector: string, className: string ) {

        const element = document.querySelector(selector);
        console.log("element for add:", element);
        const childElements = element?.children;
        if (childElements) {
            for (let i = 0; i < childElements.length; i++) {
                const child = childElements[i];
                child.className +=' '+ className;
            }
        }
    }
}
