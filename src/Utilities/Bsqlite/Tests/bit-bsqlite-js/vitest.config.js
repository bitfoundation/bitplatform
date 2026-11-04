import { existsSync } from 'node:fs';
import { defineConfig } from 'vitest/config';

export default defineConfig({
    plugins: [{
        name: 'ts-behind-js-import',
        enforce: 'pre',
        resolveId(source) {
            const match = /^(?:\/@fs)?(.+)\.js(\?.*)?$/.exec(source);
            if (!match) return;
            const path = match[1].replace(/^\/(?=[A-Za-z]:)/, '');
            if (!existsSync(`${path}.js`) && existsSync(`${path}.ts`))
                return `${path}.ts${match[2] ?? ''}`;
        },
    }],
});
