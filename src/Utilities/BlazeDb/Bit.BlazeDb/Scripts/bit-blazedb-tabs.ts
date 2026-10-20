// Bit.BlazeDb cross-tab notification module.
// The writer tab posts a message after each checkpoint; replica tabs listen and reload.
// BroadcastChannel only carries the signal - the data itself always travels through storage,
// so a replica reads exactly what was made durable.
//
// Channels are keyed by a handle the caller chooses, not by the channel name: several
// BroadcastChannel objects may share one name (a publisher and a replica for the same database in
// one document, say), and each must be closable on its own without disturbing the others. A
// message posted through one object is delivered to every other object with that name, in this
// document and in others, which is exactly the fan-out wanted.

const channels = new Map<string, BroadcastChannel>();

export function open(handle: string, channelName: string, onMessage?: ((generation: number) => void) | null): void {
    close(handle);
    const channel = new BroadcastChannel(channelName);
    if (onMessage) {
        channel.onmessage = (event: MessageEvent<{ generation?: number } | null | undefined>) => onMessage(event.data?.generation ?? 0);
    }
    channels.set(handle, channel);
}

export function post(handle: string, generation: number): void {
    const channel = channels.get(handle);
    if (channel) {
        channel.postMessage({ generation });
    }
}

export function close(handle: string): void {
    const channel = channels.get(handle);
    if (channel) {
        channel.onmessage = null;
        channel.close();
        channels.delete(handle);
    }
}

export function isSupported(): boolean {
    return typeof BroadcastChannel !== 'undefined';
}
