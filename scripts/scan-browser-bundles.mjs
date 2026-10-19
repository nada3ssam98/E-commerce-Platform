// TH-8 (M0-7): browser bundles must contain no trusted-path key names or values and no internal API URL.
// Usage (after the production builds): node scripts/scan-browser-bundles.mjs
// Key values and the internal URL are also searched for when present in the environment (e.g., in CI).
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const browserFolders = ['frontend/dist/storefront/browser', 'frontend/dist/admin/browser'];
const textExtensions = new Set(['.js', '.mjs', '.html', '.css', '.json', '.txt', '.map', '.xml', '.webmanifest']);

const forbiddenNames = [
  'x-platform-api-edge-key',
  'x-platform-ssr-ingress-key',
  'x-platform-ssr-api-key',
  'PLATFORM_API_EDGE_KEY',
  'PLATFORM_SSR_INGRESS_KEY',
  'PLATFORM_SSR_API_KEY',
  'INTERNAL_API_BASE_URL',
];
const forbiddenValues = ['PLATFORM_SSR_INGRESS_KEY', 'PLATFORM_SSR_INGRESS_KEY_PREVIOUS', 'PLATFORM_SSR_API_KEY', 'PLATFORM_API_EDGE_KEY', 'INTERNAL_API_BASE_URL']
  .map((name) => ({ name, value: process.env[name]?.trim() }))
  .filter((entry) => entry.value);

function* files(folder) {
  for (const entry of readdirSync(folder)) {
    const path = join(folder, entry);
    if (statSync(path).isDirectory()) {
      yield* files(path);
    } else if (textExtensions.has(extname(entry))) {
      yield path;
    }
  }
}

const findings = [];
for (const folder of browserFolders) {
  let scanned = 0;
  for (const file of files(join(root, folder))) {
    scanned++;
    const text = readFileSync(file, 'utf8');
    const lower = text.toLowerCase();
    for (const name of forbiddenNames) {
      if (lower.includes(name.toLowerCase())) {
        findings.push(`${relative(root, file)}: contains the name ${name}`);
      }
    }
    for (const { name, value } of forbiddenValues) {
      if (text.includes(value)) {
        findings.push(`${relative(root, file)}: contains the value of ${name}`); // the value itself is never printed
      }
    }
  }
  if (scanned === 0) {
    findings.push(`${folder}: no files scanned (build missing?)`);
  }
  console.log(`Scanned ${scanned} files in ${folder}`);
}

if (findings.length > 0) {
  console.error(['Browser bundle scan failed (TH-8):', ...findings.map((finding) => `  - ${finding}`)].join('\n'));
  process.exit(1);
}
console.log(`Browser bundle scan passed (TH-8): ${forbiddenNames.length} names, ${forbiddenValues.length} values checked.`);
