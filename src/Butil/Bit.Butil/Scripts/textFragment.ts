var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // A text directive lives after `:~:` in the fragment, which is the "fragment directive" the
    // browser strips out of location.hash before any page code sees it.
    const DELIMITER = ':~:';

    // encodeURIComponent leaves `-`, `,` and `&` alone, and all three are structural inside a text
    // directive: `-` separates prefix/suffix, `,` separates the parts, `&` separates directives.
    function encodePart(value: string) {
        return encodeURIComponent(value ?? '')
            .replace(/-/g, '%2D')
            .replace(/,/g, '%2C')
            .replace(/&/g, '%26');
    }

    function encode(directive: any) {
        if (!directive?.start) return '';

        let text = '';
        if (directive.prefix) text += `${encodePart(directive.prefix)}-,`;
        text += encodePart(directive.start);
        if (directive.end) text += `,${encodePart(directive.end)}`;
        if (directive.suffix) text += `,-${encodePart(directive.suffix)}`;

        return `text=${text}`;
    }

    function decode(raw: string) {
        // text=[prefix-,]start[,end][,-suffix]
        const body = raw.slice('text='.length);
        const parts = body.split(',');
        const result: any = { prefix: '', start: '', end: '', suffix: '' };

        let index = 0;
        if (parts[index]?.endsWith('-')) {
            result.prefix = decodeURIComponent(parts[index].slice(0, -1));
            index++;
        }
        if (parts[parts.length - 1]?.startsWith('-')) {
            result.suffix = decodeURIComponent(parts[parts.length - 1].slice(1));
            parts.length -= 1;
        }
        result.start = decodeURIComponent(parts[index] ?? '');
        if (parts[index + 1] !== undefined) result.end = decodeURIComponent(parts[index + 1]);

        return result;
    }

    function fragmentOf(url: string) {
        const hash = url.indexOf('#');
        return hash < 0 ? '' : url.slice(hash + 1);
    }

    butil.textFragment = {
        isSupported() { return 'fragmentDirective' in document; },
        build(directives: any[]) {
            const encoded = (directives ?? []).map(encode).filter(d => !!d);
            return encoded.length ? `${DELIMITER}${encoded.join('&')}` : '';
        },
        buildUrl(url: string, directives: any[]) {
            const fragment = butil.textFragment.build(directives);
            if (!fragment) return url;

            // Only the fragment can hold a directive, so the split is on the first `#`: a `:~:` in
            // the path or the query is ordinary text and has to survive untouched. Anything already
            // after `:~:` is a fragment directive of its own and is replaced; an ordinary `#anchor`
            // in front of it is kept.
            const hash = url.indexOf('#');
            const head = hash < 0 ? url : url.slice(0, hash);
            const anchor = (hash < 0 ? '' : url.slice(hash + 1)).split(DELIMITER)[0];

            return `${head}#${anchor}${fragment}`;
        },
        parse(url: string) {
            const fragment = fragmentOf(url ?? '');
            const start = fragment.indexOf(DELIMITER);
            if (start < 0) return [];

            return fragment.slice(start + DELIMITER.length)
                .split('&')
                .filter(part => part.startsWith('text='))
                .map(decode);
        },
        fromSelection() {
            const selection = window.getSelection();
            const text = selection?.toString().trim() ?? '';
            if (!text) return null;

            // A long selection makes an unwieldy URL and matches no better, so past a threshold the
            // directive becomes a start/end range - which is exactly what the spec's `,` form is for.
            const words = text.split(/\s+/);
            if (words.length <= 8) return { prefix: '', start: text, end: '', suffix: '' };

            return {
                prefix: '',
                start: words.slice(0, 4).join(' '),
                end: words.slice(-4).join(' '),
                suffix: ''
            };
        },
        navigate(url: string, replace: boolean) {
            // A text directive is only acted on during a navigation, and only a same-document one
            // if the fragment actually changes - hence assign/replace rather than a hash write.
            if (replace) window.location.replace(url);
            else window.location.assign(url);
        }
    };
}(BitButil));
