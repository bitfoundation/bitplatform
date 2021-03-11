
function setElementProperty(element: HTMLElement, property: string, value: any): void {
    const _element: { [key: string]: any } = element;
    _element[property] = value;
}

function getElementProperty(element: HTMLElement, property: string): string | null {
    return element.getAttribute(property);
}