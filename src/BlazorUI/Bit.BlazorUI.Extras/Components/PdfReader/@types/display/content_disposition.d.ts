/**
 * Extract file name from the Content-Disposition HTTP response header.
 *
 * @param {string} contentDisposition
 * @returns {string} Filename, if found in the Content-Disposition header.
 */
declare function getFilenameFromContentDispositionHeader(contentDisposition: string): string;
