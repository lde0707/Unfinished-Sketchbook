"""Inventory only: classify superseded generated images; never delete files here."""
from pathlib import Path
import hashlib, json, re

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Builds/cleanup-20261023'
OUT.mkdir(parents=True, exist_ok=True)
IMAGE = {'.png', '.jpg', '.jpeg', '.webp', '.gif', '.bmp', '.tif', '.tiff', '.svg'}
reasons = {}
redirects = {}

def mark(path, reason):
    path = path.resolve()
    path.relative_to(ROOT)
    if path.is_file():
        reasons[path] = reason

def obsolete(folder, target, preserve_sources=False):
    directory = ROOT / folder
    if not directory.is_dir():
        return
    for path in directory.rglob('*'):
        if path.suffix.lower() not in IMAGE and path.suffix.lower() != '.md':
            continue
        if preserve_sources and 'sources' in path.relative_to(directory).parts:
            continue
        mark(path, 'superseded generated version: ' + folder)
    # Older preview URLs will open the replacement instead of missing images.
    if (directory / 'preview.html').exists():
        redirects[(directory / 'preview.html').relative_to(ROOT).as_posix()] = target

for name in [f'player-v{i}' for i in range(1,9)] + [
    'player-alternates-v1','player-alternates-v2','player-nonhuman-v1',
    'player-paper-wisp-v1','player-soft-swift-v1',
]:
    obsolete('Art/samples/' + name, '../player-book-spirit-v1/book-spirit.png')
# The most recent attack illustration remains a separate category.
for name in ['region1-backgrounds-v1','region1-backgrounds-v2']:
    obsolete('Art/samples/' + name, '../region1-backgrounds-v3/preview.html')
for name in ['region1-start-v7-concept','region1-zones-v7-concepts']:
    obsolete('Art/samples/' + name, '../region1-tiles-v8/preview.html')
for name in ['regional-pairs-v4','regional-pairs-v5-soft','terrain-dark-v3']:
    obsolete('Art/samples/' + name, '../regional-variants-v6/active-patterns-abc.png', False)

for name in [f'player-c-v{i}' for i in range(1,11)] + [
    'player-c-v6-cloaked','player-c-v6-sprint','player-c-dash-v1',
    'open-source-low-run',
]:
    obsolete('Art/animations/' + name, '../player-state-test/preview.html')
obsolete('Art/animations/enemies-region1-v1', '../enemies-region1-v2/preview.html', True)
for name in ['background.png','background-v2.png']:
    mark(ROOT / 'Art/out/ch1/environment-v1' / name, 'background-v3.png supersedes earlier background')

old_prefixes = [
 'air-attack-cadence-', 'air-buffer-short-dash-', 'attack-continuity-',
 'attack-range-final-', 'attack-turn-', 'combo-pause80-', 'fixed-background-',
 'instant-kiting-', 'slower-tempo-', 'tempo20-', 'thrust-advance-', 'thrust-hold-',
 'wall-hops-', 'background-v3-', 'platform-lift4-', 'projectile-ink-review-',
 'projectile-seed-review-', 'world1-', 'world2-', 'world12-', 'mezzanine-web-',
]
for path in (ROOT / 'Book/Validation').glob('*'):
    name = path.name
    if path.suffix.lower() not in IMAGE:
        continue
    if any(name.startswith(prefix) for prefix in old_prefixes):
        mark(path, 'superseded development comparison screenshot')
    elif ('-player-' in name and name.startswith(('attack-fx-','wall-v1-'))):
        mark(path, 'superseded combat/locomotion screenshot')
    elif name in ['player-resolution-before.png','single-press-jump-before.png',
                  'camera-mine-ground-before-brightness.png','enemy-animation-preview.png']:
        mark(path, 'superseded before-state screenshot')

# Ensure every old preview redirect points to an existing replacement.
for old,target in redirects.items():
    if not (ROOT / old).parent.joinpath(target).resolve().is_file():
        raise RuntimeError('Missing redirect target: '+old+' -> '+target)

# Preserve image links that are still part of canonical Docs.
docs = list((ROOT / 'Docs').rglob('*.md'))
for doc in docs:
    for ref in re.findall(r'!\[[^\]]*\]\(([^)]+)\)', doc.read_text(encoding='utf-8-sig')):
        if not re.match(r'[a-z]+:', ref, re.I):
            reasons.pop((doc.parent / ref.strip('<>')).resolve(), None)

# These are the source sets for the current Unity exporter and current previews.
protected_roots = ['Book/Assets','Docs','Art/room-backdrops-v1','Art/ThirdParty',
 'Art/samples/player-book-spirit-v1','Art/samples/player-soft-swift-attack-v1',
 'Art/samples/player-death-revive-v1','Art/samples/player-hurt-v1',
 'Art/samples/player-ink-attack-v1','Art/samples/player-parry-v1',
 'Art/samples/player-texture-v1','Art/samples/monster-design-v1',
 'Art/samples/region1-backgrounds-v3','Art/samples/region1-tiles-v8',
 'Art/samples/regional-variants-v6','Art/samples/regional-platforms-v1',
 'Art/samples/room-backdrops-v1','Art/samples/special-tiles-v1',
 'Art/animations/player-c-v6-rhythm','Art/animations/player-c-idle-v1',
 'Art/animations/player-c-jump-v1','Art/animations/player-c-jump-v2',
 'Art/animations/player-c-moving-jump-v1','Art/animations/player-c-run-idle-v1',
 'Art/animations/player-c-dash-v2','Art/animations/player-c-fall-v1',
 'Art/animations/player-c-attack-v1','Art/animations/player-c-move-attack-v1',
 'Art/animations/player-c-air-attack-v1','Art/animations/player-c-wall-v1',
 'Art/animations/enemies-region1-v2']
protected = {}
for folder in protected_roots:
    for path in (ROOT / folder).rglob('*'):
        if path.is_file() and (path.suffix.lower() in IMAGE or path.suffix.lower()=='.md'):
            if path.resolve() in reasons:
                raise RuntimeError('Protected source selected for deletion: ' + str(path))
            protected[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()

items=[]
for path,reason in sorted(reasons.items()):
    items.append({'path':path.relative_to(ROOT).as_posix(),'bytes':path.stat().st_size,
                  'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'reason':reason})
report={'root':str(ROOT),'policy':'Keep production assets, approved animation sources, latest preview per category, canonical Docs; remove superseded generated images only.',
        'delete':items,'preserve':protected,'redirects':redirects,
        'canonicalDocs':[p.relative_to(ROOT).as_posix() for p in docs],
        'bytes':sum(i['bytes'] for i in items)}
(OUT/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'files':len(items),'MiB':round(report['bytes']/1048576,1),
 'protectedFiles':len(protected),'Docs':len(docs),'previewRedirects':len(redirects)},ensure_ascii=False))
