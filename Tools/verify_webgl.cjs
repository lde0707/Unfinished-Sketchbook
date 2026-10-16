const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const crypto = require('node:crypto');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../Builds/WebGLSite/dist');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
assert(!html.includes('{{{'), 'Unity template placeholders must be resolved');
const names = [...new Set([...html.matchAll(/Build\/([^'"\s]+)/g)].map(m => m[1]))];
assert.equal(names.length, 4, 'data/framework/wasm/loader must be referenced');
const files = names.map(name => {
  const bytes = fs.readFileSync(path.join(root, 'Build', name));
  assert(bytes.length <= 25 * 1024 * 1024, 'Static hosting allows at most 25 MiB per file');
  const content = name.endsWith('.unityweb') ? zlib.brotliDecompressSync(bytes) : bytes;
  let kind;
  if (content.subarray(0, 4).equals(Buffer.from([0, 97, 115, 109]))) {
    assert(WebAssembly.validate(content), 'Invalid WebAssembly'); kind = 'wasm';
  } else if (content.subarray(0, 16).toString() === 'UnityWebData1.0\0') kind = 'data';
  else { new vm.Script(content.toString()); kind = name.endsWith('.js') ? 'loader' : 'framework'; }
  return { name, kind, bytes: bytes.length, unpackedBytes: content.length,
    sha256: crypto.createHash('sha256').update(bytes).digest('hex') };
});
assert.deepEqual(files.map(f => f.kind).sort(), ['data','framework','loader','wasm']);
const result = { unity: '6000.5.3f1', scene: 'World12Prototype', renderSize: [1920,1080],
  compression: 'Brotli with JavaScript fallback', totalDownloadBytes: files.reduce((n, f) => n + f.bytes, 0), files };
const target = path.resolve(__dirname, '../Book/Validation/webgl-build.json');
fs.writeFileSync(target, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
