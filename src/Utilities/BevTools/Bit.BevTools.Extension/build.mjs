// Assembles the unpacked extension in dist/: the extension's own files plus the companion the Bit.BevTools package
// builds (wwwroot/bit-bevtools-companion.js and .css), so the panel and the companion window are the same frontend.
// Build Bit.BevTools first (dotnet build), then: node build.mjs
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const dist = path.join(here, "dist");
const wwwroot = path.resolve(here, "../Bit.BevTools/wwwroot");
const own = ["manifest.json", "devtools.html", "devtools.js", "panel.html", "panel.js", "devtools-source.js"];
const companion = ["bit-bevtools-companion.js", "bit-bevtools-companion.css"];

for (const file of companion) {
    if (!fs.existsSync(path.join(wwwroot, file))) {
        console.error(`Missing ${path.join(wwwroot, file)}: build Bit.BevTools first (dotnet build ../Bit.BevTools).`);
        process.exit(1);
    }
}

fs.rmSync(dist, { recursive: true, force: true });
fs.mkdirSync(dist, { recursive: true });
for (const file of own) fs.copyFileSync(path.join(here, file), path.join(dist, file));
for (const file of companion) fs.copyFileSync(path.join(wwwroot, file), path.join(dist, file));
console.log("Unpacked extension written to " + dist);
