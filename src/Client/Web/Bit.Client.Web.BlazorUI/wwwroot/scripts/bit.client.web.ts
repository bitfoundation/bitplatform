class Bit {
  static setProperty(
    element: { [key: string]: any },
    property: string,
    value: any
  ): void {
    element[property] = value;
  }

  static getProperty(element: HTMLElement, property: string): string | null {
    return element.getAttribute(property);
  }
}
