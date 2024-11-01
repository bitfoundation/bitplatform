export class OptionalContentConfig {
    constructor(data: any, renderingIntent?: number);
    renderingIntent: number;
    name: any;
    creator: any;
    isVisible(group: any): any;
    setVisibility(id: any, visible?: boolean): void;
    setOCGState({ state, preserveRB }: {
        state: any;
        preserveRB: any;
    }): void;
    get hasInitialVisibility(): boolean;
    getOrder(): any;
    getGroups(): any;
    getGroup(id: any): any;
    getHash(): string;
    #private;
}
