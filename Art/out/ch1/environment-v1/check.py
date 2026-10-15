"""Read-only asset QA; run from any working directory."""
import sys, json
from pathlib import Path
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[3] / 'Tools'))
from seam_check import seam, fmt

results = {}
for name in ('background', 'background-v2', 'background-v3', 'floor', 'ceiling'):
    im = Image.open(HERE / (name + '.png')).convert('RGBA')
    a = np.asarray(im)
    alpha = a[:, :, 3].astype(float) / 255
    # Compare what is actually visible, including transparent silhouette edges.
    visible = a[:, :, :3] * alpha[:, :, None] + np.array([189,188,181]) * (1-alpha[:, :, None])
    r = seam(visible, visible, 'h')
    r.update(size=list(im.size), transparent_fraction=float((alpha == 0).mean()),
             alpha_edge_difference=float(np.abs(alpha[:,0]-alpha[:,-1]).mean()),
             description=fmt(r))
    if name in ('floor', 'ceiling'):
        left, right = np.where(alpha[:,0] >= .5)[0], np.where(alpha[:,-1] >= .5)[0]
        r['contour_gap_px'] = int(abs((left[0] if name == 'floor' else left[-1]) - (right[0] if name == 'floor' else right[-1]))) if len(left) and len(right) else None
    results[name] = r
(HERE / 'validation.json').write_text(json.dumps(results, ensure_ascii=False, indent=2, default=lambda v: v.item()), encoding='utf-8')
for name, result in results.items():
    print(name, result)
