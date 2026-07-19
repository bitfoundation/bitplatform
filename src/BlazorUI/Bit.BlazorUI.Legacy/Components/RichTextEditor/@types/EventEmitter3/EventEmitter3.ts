/**
 * Minimal `EventEmitter` interface that is molded against the Node.js
 * `EventEmitter` interface.
 */
declare class EventEmitter3<
    EventTypes extends ValidEventTypes = string | symbol,
    Context extends any = any
> {
    static prefixed: string | boolean;

    /**
     * Return an array listing the events for which the emitter has registered
     * listeners.
     */
    eventNames(): Array<EventNames<EventTypes>>;

    /**
     * Return the listeners registered for a given event.
     */
    listeners<T extends EventNames<EventTypes>>(
        event: T
    ): Array<EventListener3<EventTypes, T>>;

    /**
     * Return the number of listeners listening to a given event.
     */
    listenerCount(event: EventNames<EventTypes>): number;

    /**
     * Calls each of the listeners registered for a given event.
     */
    emit<T extends EventNames<EventTypes>>(
        event: T,
        ...args: EventArgs<EventTypes, T>
    ): boolean;

    /**
     * Add a listener for a given event.
     */
    on<T extends EventNames<EventTypes>>(
        event: T,
        fn: EventListener3<EventTypes, T>,
        context?: Context
    ): this;
    addListener<T extends EventNames<EventTypes>>(
        event: T,
        fn: EventListener3<EventTypes, T>,
        context?: Context
    ): this;

    /**
     * Add a one-time listener for a given event.
     */
    once<T extends EventNames<EventTypes>>(
        event: T,
        fn: EventListener3<EventTypes, T>,
        context?: Context
    ): this;

    /**
     * Remove the listeners of a given event.
     */
    removeListener<T extends EventNames<EventTypes>>(
        event: T,
        fn?: EventListener3<EventTypes, T>,
        context?: Context,
        once?: boolean
    ): this;
    off<T extends EventNames<EventTypes>>(
        event: T,
        fn?: EventListener3<EventTypes, T>,
        context?: Context,
        once?: boolean
    ): this;

    /**
     * Remove all listeners, or those of the specified event.
     */
    removeAllListeners(event?: EventNames<EventTypes>): this;
}

declare interface ListenerFn<Args extends any[] = any[]> {
    (...args: Args): void;
}

declare interface EventEmitterStatic {
    new <
        EventTypes extends ValidEventTypes = string | symbol,
        Context = any
    >(): EventEmitter3<EventTypes, Context>;
}

/**
 * `object` should be in either of the following forms:
 * ```
 * interface EventTypes {
 *   'event-with-parameters': any[]
 *   'event-with-example-handler': (...args: any[]) => void
 * }
 * ```
 */
declare type ValidEventTypes = string | symbol | object;

declare type EventNames<T extends ValidEventTypes> = T extends string | symbol
    ? T
    : keyof T;

declare type ArgumentMap<T extends object> = {
    [K in keyof T]: T[K] extends (...args: any[]) => void
    ? Parameters<T[K]>
    : T[K] extends any[]
    ? T[K]
    : any[];
};

declare type EventListener3<
    T extends ValidEventTypes,
    K extends EventNames<T>
> = T extends string | symbol
    ? (...args: any[]) => void
    : (
        ...args: ArgumentMap<Exclude<T, string | symbol>>[Extract<K, keyof T>]
    ) => void;

declare type EventArgs<
    T extends ValidEventTypes,
    K extends EventNames<T>
> = Parameters<EventListener3<T, K>>;
