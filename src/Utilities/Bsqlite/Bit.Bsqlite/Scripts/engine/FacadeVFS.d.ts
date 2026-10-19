// Types for the vendored wa-sqlite FacadeVFS (engine/FacadeVFS.js, wa-sqlite v1.1.2): the base class that
// turns SQLite's C callbacks into JavaScript-friendly j* methods, which a storage provider's VFS
// overrides. The module is fetched and checksummed at restore, not compiled here.

import { Base } from './VFS.js';

type Result = number | Promise<number>;

export declare class FacadeVFS extends Base {
    constructor(name: string, module: unknown, options?: unknown);
    getFilename(pFile: number): string;
    jOpen(filename: string | null, pFile: number, flags: number, pOutFlags: DataView): Result;
    jDelete(filename: string, syncDir: number): Result;
    jAccess(filename: string, flags: number, pResOut: DataView): Result;
    jFullPathname(filename: string, zOut: Uint8Array): Result;
    jGetLastError(zBuf: Uint8Array): Result;
    jClose(pFile: number): Result;
    jRead(pFile: number, pData: Uint8Array, iOffset: number): Result;
    jWrite(pFile: number, pData: Uint8Array, iOffset: number): Result;
    jTruncate(pFile: number, size: number): Result;
    jSync(pFile: number, flags: number): Result;
    jFileSize(pFile: number, pSize: DataView): Result;
    jLock(pFile: number, lockType: number): Result;
    jUnlock(pFile: number, lockType: number): Result;
    jCheckReservedLock(pFile: number, pResOut: DataView): Result;
    jFileControl(pFile: number, op: number, pArg: DataView): Result;
    jSectorSize(pFile: number): Result;
    jDeviceCharacteristics(pFile: number): Result;
}
