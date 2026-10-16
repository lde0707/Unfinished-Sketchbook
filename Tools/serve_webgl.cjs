// Local-only server for the generated Unity player. Run from any working directory.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../Builds/WebGLSite/dist');
const port = Number(process.env.PORT || 8080);
if (!fs.existsSync(path.join(root, 'index.html'))) throw new Error('Run Tools/build_webgl.ps1 first.');
const types = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.css':'text/css','.png':'image/png','.json':'application/json','.svg':'image/svg+xml'};
http.createServer((req, res) => {
  let pathname;
  try { pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname); }
  catch { res.writeHead(400).end(); return; }
  const file = path.resolve(root, '.' + (pathname.endsWith('/') ? pathname + 'index.html' : pathname));
  if (!file.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  fs.stat(file, (err, stat) => {
    if (err || !stat.isFile()) { res.writeHead(404).end('Not found'); return; }
    res.writeHead(200, {'Content-Type':types[path.extname(file)] || 'application/octet-stream',
      'Content-Length':stat.size, 'Cache-Control':'no-cache', 'X-Content-Type-Options':'nosniff'});
    if (req.method === 'HEAD') { res.end(); return; }
    fs.createReadStream(file).on('error', () => res.destroy()).pipe(res);
  });
}).listen(port, '127.0.0.1', () => console.log(`Unity player: http://127.0.0.1:${port}`));
