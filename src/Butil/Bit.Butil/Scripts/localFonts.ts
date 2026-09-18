var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.localFonts = {
        isSupported() { return typeof (window as any).queryLocalFonts === 'function'; },
        query,
        getData
    };

    // The returned FontData objects are handles to the installed font files. Only their metadata
    // crosses the boundary here; the SFNT bytes are a separate, explicit call, because a font file
    // is megabytes and nobody wants one per row of a picker.
    async function query(postscriptNames: string[] | null) {
        if (typeof (window as any).queryLocalFonts !== 'function') return [];
        try {
            const options = postscriptNames && postscriptNames.length ? { postscriptNames } : undefined;
            const fonts = await (window as any).queryLocalFonts(options);
            return fonts.map((font: any) => ({
                postscriptName: font.postscriptName ?? '',
                fullName: font.fullName ?? '',
                family: font.family ?? '',
                style: font.style ?? ''
            }));
        } catch { return []; } // the user dismissed the local-fonts prompt
    }

    // The raw SFNT blob for one font, by PostScript name - what a document exporter needs in order
    // to embed the face the user picked.
    async function getData(postscriptName: string) {
        if (typeof (window as any).queryLocalFonts !== 'function') return null;
        try {
            const fonts = await (window as any).queryLocalFonts({ postscriptNames: [postscriptName] });
            if (!fonts.length) return null;
            const blob = await fonts[0].blob();
            return new Uint8Array(await blob.arrayBuffer());
        } catch { return null; }
    }
}(BitButil));
