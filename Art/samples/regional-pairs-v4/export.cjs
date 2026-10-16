const fs = require('node:fs');
const path = require('node:path');
const sharp = require('C:/Users/doun0/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = __dirname;
const spec = JSON.parse(fs.readFileSync(path.join(root, 'prompts.json'), 'utf8'));
const png = async (src, crop, w, h) => sharp(src).extract(crop).resize(w, h, { fit: 'fill', kernel: 'lanczos3' }).removeAlpha().png().toBuffer();
const textSvg = (w, h, text, size = 19) => Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><text x="12" y="${h - 12}" fill="#d4d7de" font-family="Malgun Gothic, sans-serif" font-size="${size}">${text}</text></svg>`);
const blank = (w, h, color = '#141923') => sharp({ create: { width: w, height: h, channels: 3, background: color } });

(async () => {
  for (const dir of ['sources', 'layers', 'previews']) fs.mkdirSync(path.join(root, dir), { recursive: true });
  const pairs = [], checks = [];
  for (const region of spec.regions) {
    const pair = {};
    for (const kind of ['floor', 'wall']) {
      const asset = region[kind];
      fs.copyFileSync(asset.source, path.join(root, 'sources', asset.file));
      const m = await sharp(asset.source).metadata();
      const zoom = spec.motifExportScale;
      let output, surface;
      if (kind === 'floor') {
        const width = Math.round(m.width / zoom);
        const left = Math.floor((m.width - width) / 2);
        const top = Math.round(m.height * spec.floorSurface.sourceSurfaceFraction);
        const height = Math.round(m.height * 252 / 256 / zoom);
        surface = await png(asset.source, { left, top: 0, width, height: top }, 256, 4);
        const body = await png(asset.source, { left, top, width, height }, 256, 252);
        fs.writeFileSync(path.join(root, 'layers', region.id + '-surface-4px.png'), surface);
        fs.writeFileSync(path.join(root, 'layers', region.id + '-front-252px.png'), body);
        output = await blank(256, 256).composite([{ input: surface, left: 0, top: 0 }, { input: body, left: 0, top: 4 }]).png().toBuffer();
      } else {
        const width = Math.round(m.width / zoom), height = Math.round(m.height / zoom);
        output = await png(asset.source, { left: m.width - width, top: Math.floor((m.height - height) / 2), width, height }, 256, 256);
      }
      fs.writeFileSync(path.join(root, asset.file), output);
      pair[kind] = output;
      const om = await sharp(output).metadata();
      if (om.width !== 256 || om.height !== 256) throw new Error(asset.file + ': invalid size');
      const raw = await sharp(output).removeAlpha().raw().toBuffer();
      if (kind === 'floor') {
        const actual = await sharp(output).extract({ left: 0, top: 0, width: 256, height: 4 }).removeAlpha().raw().toBuffer();
        const expected = await sharp(surface).removeAlpha().raw().toBuffer();
        if (!actual.equals(expected)) throw new Error(asset.file + ': surface compositing changed');
      }
      let luma = 0, count = 0;
      for (let y = 0; y < 256; y++) for (let x = 0; x < 256; x++) {
        if (kind === 'floor' ? y >= 160 : x < 96) {
          const p = (y * 256 + x) * 3;
          luma += raw[p] * 0.2126 + raw[p + 1] * 0.7152 + raw[p + 2] * 0.0722; count++;
        }
      }
      checks.push({ file: asset.file, width: om.width, height: om.height, floorSurfaceHeight: kind === 'floor' ? 4 : null, deepInteriorMeanLuma: +(luma / count).toFixed(2) });
    }
    // Five independent open-sided regional layouts. No rotated wall or ceiling.
    const pieces = [{ input: pair.wall, left: 0, top: 0 }];
    for (let x = 1; x < 4; x++) pieces.push({ input: pair.floor, left: x * 256, top: 256 });
    pieces.push({ input: await blank(256, 256, '#090B10').png().toBuffer(), left: 0, top: 256 });
    await blank(1024, 512).composite(pieces).png().toFile(path.join(root, 'previews', region.id + '-layout.png'));
    pairs.push(pair);
  }
  const sheet = [];
  for (let i = 0; i < pairs.length; i++) {
    const x = 20 + i * 272;
    sheet.push({ input: textSvg(256, 42, spec.regions[i].label), left: x, top: 0 });
    sheet.push({ input: pairs[i].floor, left: x, top: 52 });
    sheet.push({ input: textSvg(256, 32, '바닥 · 윗면 4px', 15), left: x, top: 308 });
    sheet.push({ input: pairs[i].wall, left: x, top: 356 });
    sheet.push({ input: textSvg(256, 32, '벽 · 오른쪽 노출 면', 15), left: x, top: 612 });
  }
  await blank(1380, 660).composite(sheet).png().toFile(path.join(root, 'regional-pairs-preview.png'));
  // Directly abutting materials at identical y, specifically to inspect surface depth.
  await blank(1280, 288).composite([
    ...pairs.map((p, i) => ({ input: p.floor, left: i * 256, top: 32 })),
    { input: textSvg(1280, 28, '5개 지역 공통: 표면 띠 256×4px · 동일 높이 · 원근 변화 없음', 17), left: 0, top: 0 }
  ]).png().toFile(path.join(root, 'floor-alignment-preview.png'));
  const layouts = [];
  for (let i = 0; i < pairs.length; i++) {
    const input = await sharp(path.join(root, 'previews', spec.regions[i].id + '-layout.png')).resize(768, 384).png().toBuffer();
    layouts.push({ input: textSvg(768, 40, spec.regions[i].label), left: 0, top: i * 424 });
    layouts.push({ input, left: 0, top: i * 424 + 40 });
  }
  await blank(768, 2120).composite(layouts).png().toFile(path.join(root, 'regional-layouts-preview.png'));
  fs.writeFileSync(path.join(root, 'validation.json'), JSON.stringify(checks, null, 2) + '\n');
  console.log(JSON.stringify(checks));
})().catch(error => { console.error(error); process.exitCode = 1; });
