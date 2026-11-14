"""Audit explicit superseded assets against Unity GUID references before cleanup."""
from pathlib import Path
import hashlib, json, re

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Builds/runtime-cleanup-20261025'
OUT.mkdir(parents=True, exist_ok=True)
GROUPS = {
    'Book/Assets/Unfin/Art/PlayerC': 'Unused joint-based player art; whole-frame PlayerSprites is active',
    'Book/Assets/Unfin/Animations/PlayerC': 'Unused joint-based animation/controller',
    'Book/Assets/Unfin/Prefabs/PaperPlayerC.prefab': 'Unused joint-based player prefab',
    'Book/Assets/Unfin/Runtime/PaperPlayerRig.cs': 'Superseded by whole-frame sprite player',
    'Book/Assets/Unfin/Runtime/PaperPlayerDriver.cs': 'Superseded duplicate locomotion controller',
    'Book/Assets/Unfin/Editor/PaperPlayerImporter.cs': 'Importer for removed joint-based player only',
    'Tools/package_player_animations.py': 'Packager for removed joint-based player only',
    'Tools/prepare_boss_region1.cjs': 'Superseded by prepare_boss_region1_v2.cjs',
    'Art/samples/boss-region1-v1': 'Boss v2 is canonical and includes its own background',
    'Art/samples/gates-items-map-v1': 'Superseded by gates-items-map-v2',
    'Art/samples/player-texture-v1': 'Superseded by player-texture-v2 and runtime PlayerSurface',
}
files = {}
for relative, reason in GROUPS.items():
    base = ROOT / relative
    candidates = list(base.rglob('*')) if base.is_dir() else [base]
    candidates.append(Path(str(base) + '.meta'))
    for path in candidates:
        if path.is_file():
            assert path.resolve().is_relative_to(ROOT)
            files[path] = reason

# Assets may be serialized by GUID even when no C# code mentions their names.
removed_guids = {}
for path in files:
    if path.suffix == '.meta':
        match = re.search(r'^guid: ([a-f0-9]{32})$', path.read_text(encoding='utf-8-sig'), re.M)
        if match: removed_guids[match[1]] = path.relative_to(ROOT).as_posix()
blockers = []
text_ext = {'.unity', '.prefab', '.asset', '.mat', '.controller', '.anim', '.overrideController', '.meta'}
for path in (ROOT / 'Book/Assets').rglob('*'):
    if path in files or path.suffix not in text_ext or not path.is_file(): continue
    text = path.read_text(encoding='utf-8-sig', errors='ignore')
    for guid in set(re.findall(r'guid: ([a-f0-9]{32})', text)) & removed_guids.keys():
        blockers.append({'from': path.relative_to(ROOT).as_posix(), 'to': removed_guids[guid]})
if blockers:
    raise RuntimeError('Live Unity references: ' + json.dumps(blockers))

# Removing a class must leave neither code references nor serialized references.
for path in (ROOT / 'Book/Assets').rglob('*.cs'):
    if path in files: continue
    text = path.read_text(encoding='utf-8-sig')
    if re.search(r'\b(PaperPlayerRig|PaperPlayerDriver|PaperPlayerImporter)\b', text):
        raise RuntimeError('Legacy class still used: ' + str(path))

entries = []
for path, reason in sorted(files.items()):
    entries.append({'path': path.relative_to(ROOT).as_posix(), 'reason': reason,
                    'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'bytes': path.stat().st_size})
manifest = {'root': str(ROOT), 'delete': entries, 'bytes': sum(e['bytes'] for e in entries),
            'checkedUnityGuids': len(removed_guids), 'externalUnityReferences': blockers,
            'preserved': ['Active scene/prefab references', 'Resources dynamic loads',
                          'Current source frames and import recipes', 'Art/latest-assets.json categories',
                          'Docs', 'Other Unity projects and third-party licenses']}
(OUT / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'files':len(entries),'bytes':manifest['bytes'],'externalReferences':len(blockers)}))
