// Copies the Cytoscape.js UMD build from node_modules into public/vendor/
// so index.html can load it as a same-origin <script> instead of pulling
// it from a CDN at runtime. Runs automatically via the "postinstall" npm
// lifecycle hook, so a plain `npm install` (or `npm ci` in CI) is enough
// to get a fully working local copy of the site under public/ — no
// separate manual step needed.

const fs = require("fs");
const path = require("path");

const src = path.join(__dirname, "..", "node_modules", "cytoscape", "dist", "cytoscape.min.js");
const destDir = path.join(__dirname, "..", "public", "vendor");
const dest = path.join(destDir, "cytoscape.min.js");

if (!fs.existsSync(src)) {
  console.error("vendor-cytoscape: " + src + " not found — is the \"cytoscape\" dependency installed?");
  process.exit(1);
}

fs.mkdirSync(destDir, { recursive: true });
fs.copyFileSync(src, dest);
console.log("vendor-cytoscape: copied cytoscape.min.js -> " + path.relative(process.cwd(), dest));
