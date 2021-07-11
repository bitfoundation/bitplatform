﻿class Bit {
    static setProperty(element: { [key: string]: any }, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: { [key: string]: any }, property: string): string | null {
        return element[property];
    }
}