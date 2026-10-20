import { defineConfig } from 'vitest/config';

export default defineConfig({
    test: {
        environment: 'happy-dom',
        include: ['**/*.test.js'],
        exclude: ['node_modules/**'],
        restoreMocks: true,
    },
});
