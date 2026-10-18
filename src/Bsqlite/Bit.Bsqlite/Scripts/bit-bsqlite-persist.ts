export function requestPersistence(): Promise<boolean> | boolean {
    return typeof navigator.storage?.persist === 'function'
        ? navigator.storage.persist()
        : false;
}
