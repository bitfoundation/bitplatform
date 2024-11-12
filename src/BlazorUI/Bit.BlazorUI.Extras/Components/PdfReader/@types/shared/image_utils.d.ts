declare function convertBlackAndWhiteToRGBA({ src, srcPos, dest, width, height, nonBlackColor, inverseDecode, }: {
    src: any;
    srcPos?: number | undefined;
    dest: any;
    width: any;
    height: any;
    nonBlackColor?: number | undefined;
    inverseDecode?: boolean | undefined;
}): {
    srcPos: number;
    destPos: number;
};
declare function convertToRGBA(params: any): {
    srcPos: number;
    destPos: number;
} | null;
declare function grayToRGBA(src: any, dest: any): void;
