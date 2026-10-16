/* Visual tile layers only. Collision tiles remain owned by the game. */
function createTerrainLayers({tileSize: T, width: W, height: H, scale, rooms, zones, regionX}) {
  const chunkSize = 128, patchSize = 96, cache = new Map();
  let grid;
  const hash = (x, y, salt = 0) => {
    let n = Math.imul(x, 374761393) ^ Math.imul(y, 668265263) ^ Math.imul(salt, 1442695041);
    n = Math.imul(n ^ (n >>> 13), 1274126177);
    return ((n ^ (n >>> 16)) >>> 0) / 4294967296;
  };
  const solid = (x, y) => x < 0 || y < 0 || x >= W || y >= H || grid[y * W + x] === 1;
  // Checker parity chooses disjoint variant pairs, so adjacent patches never repeat.
  const variantAt = (x, y) => ((x + y) & 1) + 2 * Math.floor(hash(x, y, 19) * 2);
  function shape(p, x, y, tl, tr, br, bl) {
    p.moveTo(x + tl, y); p.lineTo(x + T - tr, y);
    p.quadraticCurveTo(x + T, y, x + T, y + tr);
    p.lineTo(x + T, y + T - br); p.quadraticCurveTo(x + T, y + T, x + T - br, y + T);
    p.lineTo(x + bl, y + T); p.quadraticCurveTo(x, y + T, x, y + T - bl);
    p.lineTo(x, y + tl); p.quadraticCurveTo(x, y, x + tl, y); p.closePath();
  }
  function makeChunk(cx, cy) {
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = chunkSize * scale;
    const c = canvas.getContext('2d'), wx = cx * chunkSize, wy = cy * chunkSize;
    c.setTransform(scale, 0, 0, scale, -wx * scale, -wy * scale);
    const mask = new Path2D(), edge = new Path2D(), floors = new Path2D();
    // Include a tile outside the chunk so strokes and rounded corners cross cache boundaries.
    for (let y = Math.max(0, wy / T - 1); y < Math.min(H, (wy + chunkSize) / T + 1); y++) {
      for (let x = Math.max(0, wx / T - 1); x < Math.min(W, (wx + chunkSize) / T + 1); x++) {
        if (!solid(x, y)) continue;
        const px = x * T, py = y * T;
        const top = !solid(x, y - 1), bottom = !solid(x, y + 1);
        const left = !solid(x - 1, y), right = !solid(x + 1, y);
        const tl = top && left ? 1.6 : 0, tr = top && right ? 1.6 : 0;
        const bl = bottom && left ? 2.4 : 0, br = bottom && right ? 2.4 : 0;
        shape(mask, px, py, tl, tr, br, bl);
        if (top) {
          edge.moveTo(px, py + tl); edge.quadraticCurveTo(px, py, px + tl, py);
          edge.lineTo(px + T - tr, py); edge.quadraticCurveTo(px + T, py, px + T, py + tr);
          floors.moveTo(px + tl, py + 2); floors.lineTo(px + T - tr, py + 2);
        }
        if (bottom) {
          edge.moveTo(px, py + T - bl); edge.quadraticCurveTo(px, py + T, px + bl, py + T);
          edge.lineTo(px + T - br, py + T); edge.quadraticCurveTo(px + T, py + T, px + T, py + T - br);
        }
        if (left) { edge.moveTo(px, py + tl); edge.lineTo(px, py + T - bl); }
        if (right) { edge.moveTo(px + T, py + tr); edge.lineTo(px + T, py + T - br); }
      }
    }
    c.save(); c.clip(mask);
    // Fill layer: one continuous chapter blend, with broad, softly blended zone accents.
    const base = c.createLinearGradient((regionX - 14) * T, 0, (regionX + 14) * T, 0);
    base.addColorStop(0, '#686c72'); base.addColorStop(1, '#8b7969');
    c.fillStyle = base; c.fillRect(wx, wy, chunkSize, chunkSize);
    for (const r of rooms) {
      const rx = (r.x + r.w / 2) * T, ry = (r.y + r.h / 2) * T;
      const radius = Math.max(r.w, r.h) * T * 0.7 + 64;
      if (rx + radius < wx || rx - radius > wx + chunkSize || ry + radius < wy || ry - radius > wy + chunkSize) continue;
      const tint = c.createRadialGradient(rx, ry, 0, rx, ry, radius);
      tint.addColorStop(0, zones[r.z].dot + '38'); tint.addColorStop(1, zones[r.z].dot + '00');
      c.fillStyle = tint; c.fillRect(wx, wy, chunkSize, chunkSize);
    }
    // Detail layer: four motif families span six collision tiles, with seeded size/density/offset.
    // All motifs are world anchored. They do not change on camera movement or respawn.
    for (let y = Math.floor(wy / patchSize) - 1; y <= Math.floor((wy + chunkSize) / patchSize) + 1; y++) {
      for (let x = Math.floor(wx / patchSize) - 1; x <= Math.floor((wx + chunkSize) / patchSize) + 1; x++) {
        const v = variantAt(x, y), px = (x + 0.18 + hash(x, y, 1) * 0.64) * patchSize;
        const py = (y + 0.18 + hash(x, y, 2) * 0.64) * patchSize;
        const radius = 24 + hash(x, y, 3) * 30;
        const wash = c.createRadialGradient(px, py, 0, px, py, radius);
        wash.addColorStop(0, v < 2 ? 'rgba(232,222,202,.09)' : 'rgba(35,39,47,.12)');
        wash.addColorStop(1, 'rgba(100,100,100,0)');
        c.fillStyle = wash; c.beginPath(); c.arc(px, py, radius, 0, Math.PI * 2); c.fill();
        c.strokeStyle = 'rgba(224,221,207,.16)'; c.lineWidth = 0.65; c.lineCap = 'round';
        c.beginPath();
        const count = 3 + Math.floor(hash(x, y, 4) * 5);
        for (let k = 0; k < count; k++) {
          const length = 9 + hash(x, y, k + 5) * 22, offset = k * (3 + hash(x, y, 16) * 2);
          if (v === 0) { // sparse clean diagonal strokes
            c.moveTo(px + offset, py); c.lineTo(px + offset - length, py + length);
          } else if (v === 1) { // long soft strata
            c.moveTo(px - length, py + offset);
            c.bezierCurveTo(px - 8, py + offset - 3, px + 7, py + offset + 4, px + length, py + offset + 2);
          } else if (v === 2) { // rounded nested material contours
            c.moveTo(px - 12 - offset, py + 17);
            c.bezierCurveTo(px - 19 - offset, py - 8, px + 12 + offset, py - 14, px + 18 + offset, py + 9);
          } else { // short offset mineral marks, never a full repeating grid
            c.moveTo(px + offset - length, py + k * 2);
            c.quadraticCurveTo(px + offset - length / 2, py + k * 2 - 3, px + offset, py + k * 2 - 1);
          }
        }
        c.stroke();
      }
    }
    // Edge layer: darker surface band, crisp smooth outline, subtle walkable-top highlight.
    c.lineJoin = 'round'; c.lineCap = 'round';
    c.strokeStyle = 'rgba(35,39,47,.20)'; c.lineWidth = 8; c.stroke(edge);
    c.strokeStyle = '#30333b'; c.lineWidth = 2.2; c.stroke(edge);
    c.strokeStyle = 'rgba(237,231,213,.28)'; c.lineWidth = 0.7; c.stroke(floors);
    c.restore();
    return canvas;
  }
  return {
    setGrid(value) { grid = value; cache.clear(); },
    draw(c, ox, oy, width, height) {
      c.save(); c.imageSmoothingEnabled = true; c.imageSmoothingQuality = 'high';
      for (let y = Math.max(0, Math.floor(oy / chunkSize)); y <= Math.min(Math.ceil(H * T / chunkSize) - 1, Math.floor((oy + height) / chunkSize)); y++) {
        for (let x = Math.max(0, Math.floor(ox / chunkSize)); x <= Math.min(Math.ceil(W * T / chunkSize) - 1, Math.floor((ox + width) / chunkSize)); x++) {
          const key = x + ',' + y;
          let canvas = cache.get(key);
          if (!canvas) canvas = makeChunk(x, y);
          cache.delete(key); cache.set(key, canvas);
          if (cache.size > 48) cache.delete(cache.keys().next().value);
          c.drawImage(canvas, x * chunkSize, y * chunkSize, chunkSize, chunkSize);
        }
      }
      c.restore();
    }
  };
}
