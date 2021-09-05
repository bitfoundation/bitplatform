class BitSpinButton {

    static timer: any;

    static changeTheValue(dotnetHelper: any, callback: string, action: BitSpinButtonAction, e: MouseEvent, delay = 200) {
        const eventArgs = BitSpinButton.toMouseEventArgsMapper(e);
        dotnetHelper.invokeMethodAsync(callback, action, eventArgs);
        this.timer = setTimeout(() => this.changeTheValue(dotnetHelper, callback, action, e, 80), delay);
    }
    
    static registerMouseEventsOnBitSpinButton(dotnetHelper: any, componentInputId: string, callback: string): void {
        const input = document.querySelector(`#${componentInputId}`);
        const incrementButton = input?.parentNode?.querySelector("span>button:first-of-type");
        const decrementButton = input?.parentNode?.querySelector("span>button:last-of-type");

        incrementButton?.addEventListener("mousedown", (e) =>
            this.changeTheValue(dotnetHelper, callback, BitSpinButtonAction.Increment, e as MouseEvent));
        incrementButton?.addEventListener("mouseup", () => clearTimeout(this.timer));
        incrementButton?.addEventListener("mouseleave", () => clearTimeout(this.timer));

        decrementButton?.addEventListener("mousedown", (e) =>
            this.changeTheValue(dotnetHelper, callback, BitSpinButtonAction.Decrement, e as MouseEvent));
        decrementButton?.addEventListener("mouseup", () => clearTimeout(this.timer));
        decrementButton?.addEventListener("mouseleave", () => clearTimeout(this.timer));
    }

    static toMouseEventArgsMapper(e: MouseEvent): object {
        return {
            altKey: e.altKey,
            button: e.button,
            buttons: e.buttons,
            clientX: e.clientX,
            clientY: e.clientY,
            ctrlKey: e.ctrlKey,
            detail: e.detail,
            metaKey: e.metaKey,
            offsetX: e.offsetX,
            offsetY: e.offsetY,
            screenY: e.screenY,
            screenX: e.screenX,
            shiftKey: e.shiftKey,
            type: e.type,
        };
    }
}

enum BitSpinButtonAction {
    Increment = 0,
    Decrement = 1
}