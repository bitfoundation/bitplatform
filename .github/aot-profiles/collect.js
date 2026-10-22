const { chromium } = require('playwright-core');
const path = require('path');
const fs = require('fs');

const [url, output, writeAt, stepsArgument, frameworkFolder] = process.argv.slice(2);
const origin = new URL(url).origin;
const steps = (stepsArgument || '').split(',').filter(Boolean);
const contentTypes = { '.wasm': 'application/wasm', '.js': 'text/javascript', '.dat': 'application/octet-stream', '.json': 'application/json' };

const configureProfiler = writeAt => {
    const config = { aotProfilerOptions: { writeAt, sendTo: 'System.Runtime.InteropServices.JavaScript.JavaScriptExports::DumpAotProfileData' } };
    const wrap = (blazor, start) => options => {
        options ??= {};
        const configureRuntime = options.configureRuntime;
        options.configureRuntime = builder => { configureRuntime?.(builder); builder.withConfig(config); };
        options.webAssembly ??= {};
        const configureWebAssemblyRuntime = options.webAssembly.configureRuntime;
        options.webAssembly.configureRuntime = builder => { configureWebAssemblyRuntime?.(builder); builder.withConfig(config); };
        return start.call(blazor, options);
    };
    let blazor;
    Object.defineProperty(window, 'Blazor', {
        configurable: true,
        get: () => blazor,
        set: value => {
            blazor = value;
            if (!value) return;
            let start = typeof value.start === 'function' ? wrap(value, value.start) : undefined;
            Object.defineProperty(value, 'start', { configurable: true, get: () => start, set: f => start = typeof f === 'function' ? wrap(value, f) : f });
        }
    });
};

(async () => {
    const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
    const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, serviceWorkers: 'block' });
    await context.addInitScript(configureProfiler, writeAt);

    await context.route(request => request.origin === origin, async route => {
        const request = route.request();
        if (request.resourceType() === 'document') {
            const response = await route.fetch();
            const html = (await response.text()).replace(/<script src="(_framework\/blazor\.web[^"]*)"><\/script>/, '<script src="$1" autostart="false"></script><script>Blazor.start();</script>');
            return route.fulfill({ response, body: html });
        }
        const name = decodeURIComponent(new URL(request.url()).pathname);
        if (frameworkFolder && name.startsWith('/_framework/') && name.startsWith('/_framework/bit.blazor.web') === false) {
            const file = path.join(frameworkFolder, name.slice('/_framework/'.length));
            if (fs.existsSync(file)) return route.fulfill({ status: 200, body: fs.readFileSync(file), headers: { 'content-type': contentTypes[path.extname(file)] ?? 'application/octet-stream' } });
        }
        return route.continue();
    });

    const page = await context.newPage();
    page.on('pageerror', e => console.log('page error:', e.message));
    await page.goto(url, { waitUntil: 'load', timeout: 180000 });
    await page.waitForFunction(() => window.getDotnetRuntime?.(0), null, { timeout: 180000 });
    await page.waitForTimeout(8000);

    for (const step of steps) {
        if (step.startsWith('click:')) {
            await page.locator(step.slice('click:'.length)).first().click();
        } else if (step.startsWith('role:')) {
            const [role, name] = step.slice('role:'.length).split('|');
            await page.getByRole(role, { name }).first().click();
        } else if (step.startsWith('key:')) {
            await page.locator('body').click({ position: { x: 5, y: 5 } });
            await page.keyboard.press(step.slice('key:'.length));
        } else {
            await page.evaluate(p => window.Blazor.navigateTo(p), step);
        }
        await page.waitForTimeout(3000);
        console.log('done:', step);
    }

    await page.waitForFunction(() => window.getDotnetRuntime(0).INTERNAL.aotProfileData, null, { timeout: 60000 });
    const data = await page.evaluate(() => {
        const bytes = window.getDotnetRuntime(0).INTERNAL.aotProfileData;
        let text = '';
        for (let i = 0; i < bytes.length; i++) text += String.fromCharCode(bytes[i]);
        return btoa(text);
    });
    fs.writeFileSync(output, Buffer.from(data, 'base64'));
    console.log(`${output}: ${fs.statSync(output).size} bytes`);
    await browser.close();
})();
