const fs = require('node:fs');
const path = require('node:path');
const sharp = require('C:/Users/doun0/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');

(async () => {
  const root = __dirname;
  const manifest = JSON.parse(fs.readFileSync(path.join(root, 'prompts.json'), 'utf8'));
  fs.mkdirSync(path.join(root, 'sources'), { recursive: true });
  const tiles = [];
  const report = [];
  for (const asset of manifest.assets) {
    fs.copyFileSync(asset.finalSource, path.join(root, 'sources', asset.file));
    const tile = await sharp(asset.finalSource).resize(256, 256, { fit: 'fill', kernel: 'lanczos3' }).png().toBuffer();
    fs.writeFileSync(path.join(root, asset.file), tile);
    const info = await sharp(tile).metadata();
    if (info.width !== 256 || info.height !== 256) throw new Error('Incorrect export size');
    const { data, info: raw } = await sharp(tile).removeAlpha().raw().toBuffer({ resolveWithObject: true });
    let sum = 0, count = 0;
    for (let y = 0; y < 256; y++) for (let x = 0; x < 256; x++) {
      const inside = asset.orientation === 'floor' ? y >= 128 : asset.orientation === 'ceiling' ? y < 128 : asset.orientation === 'wall-left' ? x < 128 : x >= 128;
      if (inside) { const k = (y * 256 + x) * raw.channels; sum += data[k] * 0.2126 + data[k + 1] * 0.7152 + data[k + 2] * 0.0722; count++; }
    }
    report.push({ file: asset.file, width: info.width, height: info.height, deepInteriorMeanRgbLuma: +(sum / count).toFixed(2) });
    tiles.push(tile);
  }
  // Contact sheet: exact final exports, no retouching or rotation.
  await sharp({ create: { width: 1280, height: 256, channels: 3, background: '#10151D' } })
    .composite(tiles.map((input, i) => ({ input, left: i * 256, top: 0 })))
    .png().toFile(path.join(root, 'five-tiles-preview.png'));

  // Orientation study only: different regional materials placed in one test room.
  const placed = [];
  for (let x = 0; x < 8; x++) {
    placed.push({ input: tiles[2], left: x * 256, top: 0 });
    placed.push({ input: tiles[x < 4 ? 0 : 1], left: x * 256, top: 1024 });
  }
  for (let y = 1; y < 4; y++) {
    placed.push({ input: tiles[3], left: 0, top: y * 256 });
    placed.push({ input: tiles[4], left: 1792, top: y * 256 });
  }
  await sharp({ create: { width: 2048, height: 1280, channels: 3, background: '#10151D' } })
    .composite(placed).png().toFile(path.join(root, 'room-layout-preview.png'));
  fs.writeFileSync(path.join(root, 'validation.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report));
})().catch(error => { console.error(error); process.exitCode = 1; });
