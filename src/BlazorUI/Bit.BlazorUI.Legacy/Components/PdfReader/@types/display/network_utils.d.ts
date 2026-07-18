declare function createHeaders(isHttp: any, httpHeaders: any): Headers;
declare function createResponseStatusError(status: any, url: any): MissingPDFException | UnexpectedResponseException;
declare function extractFilenameFromHeader(responseHeaders: any): string | null;
declare function validateRangeRequestCapabilities({ responseHeaders, isHttp, rangeChunkSize, disableRange, }: {
    responseHeaders: any;
    isHttp: any;
    rangeChunkSize: any;
    disableRange: any;
}): {
    allowRangeRequests: boolean;
    suggestedLength: undefined;
};
declare function validateResponseStatus(status: any): boolean;
