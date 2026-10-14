"""
Unfin 에셋 목록 + 완성 프롬프트 생성기

블록(MSB · UNIFY · MAT · TYPE · NEG)과 에셋 목록을 한곳에서 관리하고,
복사해서 바로 쓰는 완성 프롬프트를 만들어 줘.

사용법:
  python Tools/build_prompts.py                 # Art/ 폴더에 전부 생성
결과물:
  Art/asset_catalog.json   : 에셋 목록 (기계용)
  Art/prompts_full.csv     : 에셋마다 완성 프롬프트 · 네거티브 · 레퍼런스 · 컨트롤 · 파일명 (스프레드시트용)
  Art/ALL_PROMPTS.md       : 분류별 완성 프롬프트 (복사·붙여넣기용)

⚠️ 블록 문구를 바꾸면 버전을 올리고 17 문서 변경 이력에 적기.
"""
import json, csv, os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Art')

# ───────────────────────── 블록 ─────────────────────────
MSB = ("hand-drawn 2D side-scrolling game art, drawn on a page of a child's sketchbook, "
       "off-white warm paper with visible paper grain and a very faint square grid, "
       "muted art-supply colors with low saturation, soft ink-gray outlines (not pure black), "
       "outline weight thick and consistent on gameplay objects, thinner on background, "
       "slightly wobbly hand-drawn lines, a few intentionally unfinished strokes and eraser marks, "
       "matte paper look, flat lighting, shading done with drawing-material texture (not gradients), "
       "calm melancholic and gentle mood, clear readable silhouettes, orthographic side view, "
       "quiet composition with generous empty paper space, "
       "high-resolution smooth hand-drawn illustration (not pixel art), soft natural anti-aliased edges, "
       "organic irregular contours, every wall and floor surface drawn uniquely by hand with no repeating pattern")

UNIFY = ("UNIFIED LOOK: identical flat ambient light from the front for every asset, no directional light, "
         "no cast shadows, no highlights, no rim light, only a soft faint contact shadow under grounded objects; "
         "the same fine paper grain pattern visible through every surface; all shading uses the same "
         "45-degree diagonal hatching direction (top-right to bottom-left); every outline uses the same ink-gray "
         "and the same wobble; same line weight for the same layer; same muted color temperature; "
         "looks like part of one continuous hand-drawn game; "
         "SMOOTH AND ORGANIC: soft natural edges everywhere, rounded imperfect corners, no hard square tile edges, "
         "no repeated stamps or copy-pasted texture, hatching density and stroke placement vary naturally across every surface")

MAT = {
    1: ("drawn with graphite pencil, 45-degree pencil hatching for solid shapes, soft eraser smudges and half-erased lines, "
        "almost monochrome warm gray, tiny pinkish eraser crumbs, half-drawn sketchy trees"),
    2: ("drawn with thick wax crayons, heavy layered overpainted strokes in crossing directions, paper grain showing through, "
        "waxy texture, warm sunset palette of muted orange, beige and dusty red, brown-tinted outlines, drips of melted crayon wax"),
    3: ("painted with transparent watercolor, soft bleeding edges, dried water-ring outlines, layered pale washes, "
        "misty pale blue, gray-blue and dusty pink, slightly wrinkled wet paper"),
    4: ("made of cut and folded colored paper, flat single-color shapes, one crisp fold line, one-step soft paper shadow, "
        "straight ruler-drawn lines, muted red, mustard yellow, sage green and slate blue"),
    5: ("drawn with dip pen and spreading black-violet ink, ink blots seeping into paper, fragments of pencil, crayon, "
        "watercolor and colored paper half-sunk in ink, faint remaining colors glowing softly through the ink"),
}
PALETTE_WORDS = {1: "warm light gray paper, graphite gray, ink gray", 2: "muted sunset orange, wax beige, brown-gray",
                 3: "misty pale blue, gray-blue, dusty pink", 4: "muted red, mustard, sage, slate blue on off-white",
                 5: "black-violet ink, faded paper, faint colored fragments"}

TYPE = {
    'TERRAIN': ("large terrain fill texture sheet, 2048x2048, flat front view, no perspective, no lighting direction, "
                "fills the whole image, NO visible repeating motif, strokes and hatching vary in density, length and placement everywhere, "
                "irregular organic variation like one big hand-drawn surface, edges wrap seamlessly only so the sheet can stretch over large areas"),
    'EDGE': ("long horizontal strip of a hand-drawn terrain edge on transparent background, 4096x512, "
             "one continuous wobbly organic contour line with the material texture fading into it, "
             "no straight ruler line, no repeating bumps, natural irregular rhythm, soft anti-aliased edge"),
    'CORNER': ("single organic rounded corner piece of terrain on transparent background, soft natural anti-aliased edge, "
               "imperfect hand-drawn curve, matches a continuous edge strip"),
    'PLATFORM': ("single long one-way platform plank on transparent background, side view, thick hand-drawn line on top, "
                 "soft natural edges, irregular hand-drawn shape, no two planks identical"),
    'SPECIAL': ("single tall special terrain block on transparent background, side view, soft natural edges, "
                "drawn as one unique hand-drawn object, not a repeating tile"),
    'BG_FAR': ("very distant background layer, extremely faint unfinished sketch, only outlines, barely visible against the paper, "
               "low contrast (5-10% darker than paper), wide panoramic 32:9, one panel of one long continuous world panorama, its edges continue into the neighboring area panels, same horizon height everywhere, no repeated landmarks"),
    'BG_MID': ("middle-distance background layer, simple shapes of landmarks, thin lines, low saturation, 15-20% darker than paper, "
               "no ground plane in the lower 30%, wide panoramic 32:9, one panel of one long continuous world panorama, its edges continue into the neighboring area panels, same horizon height everywhere, every landmark different, no repeated shapes"),
    'BG_FORE': ("foreground overlay elements only at the bottom fifth of the image, sparse, soft and slightly faded, transparent background"),
    'DECOR': ("decoration overlay for one room on a fully transparent background, the provided layout shows where terrain is, "
              "do NOT draw the terrain itself, do NOT fill walls, floors or air, everything except the decorations stays transparent, "
              "place a few unique small details hugging the terrain surfaces: cracks, eraser marks, half-erased lines, little scribbles, "
              "roots, vines, small landmark objects that give this room its own character, "
              "decorations sit on or just inside terrain edges, keep walkable air and every platform top completely clear, "
              "soft natural anti-aliased edges, sparse, no repeated motif, 16:9"),
    'BG_LINK': ("transition panel of a continuous panoramic background, the left half continues the previous area panel exactly "
                "(outpainting from its right edge), the right half gradually becomes the next area, landmarks and ridgelines flow through "
                "without a visible seam, same horizon height, same layer brightness, 32:9"),
    'PROP': ("single game prop, isolated on transparent background, centered, entire object visible, side view, "
             "same outline weight as gameplay objects"),
    'CHAR': ("single 2D game sprite, side view facing right, full body visible, isolated on transparent background, "
             "simple readable silhouette, thick consistent outline, minimal interior detail, designed for hand-drawn frame animation"),
    'POSE': ("single animation key pose, side view facing right, full body, isolated on transparent background, "
             "body weight clearly readable, line of action is a smooth curve"),
    'FX': ("hand-drawn 2D game effect, single frame, transparent background, no glow, no light, no bloom, "
           "made of drawing-material pieces"),
    'UI': ("hand-drawn game UI element looking like a small paper note or doodle on a sketchbook, semi-transparent off-white paper, "
           "wobbly hand-drawn border, transparent background, no text"),
    'KEY': ("illustration page from the same sketchbook, slightly more detailed than gameplay art but same materials, outline and lighting, "
            "flat lighting, 16:9"),
}

CHAR_LOCK = ("the same tiny protagonist in every image: round head about 45% of total height, rounded-rectangle body, "
             "very short thin stick-like arms and legs drawn as single thick lines, one small dot eye, no mouth, no nose, "
             "paper-white body with soft ink-gray outline, no colors, no clothes, no accessories, "
             "exact same proportions and line weight as the reference character sheet")

NEG_COMMON = ("pixel art, pixelated, 8-bit, 16-bit, retro game sprite, dot art, dithering, aliased jagged edges, stair-step edges, blocky, "
              "low resolution, mosaic, grid of square tiles, repeating tile pattern, tiled texture, copy-pasted stamps, identical repeated bricks, "
              "photorealistic, realistic lighting, 3D render, CGI, voxel, vector clean flat corporate style, anime cel shading, "
              "glossy, shiny, metallic, chrome, neon, glow, bloom, lens flare, light rays, rim light, specular highlight, "
              "directional shadows, cast shadow, smooth digital gradient, airbrush, heavy drop shadow, pure black outline, high contrast HDR, "
              "saturated rainbow colors, dark horror atmosphere, gore, text, letters, numbers, logo, watermark, signature, UI, frame border, "
              "blurry, jpeg artifacts, perspective view, isometric, top-down")
NEG_ENV = "characters, people, animals, creatures, faces"
NEG_DECOR = ", solid ground fill, filled walls, full scene, paper background, opaque background, platforms, terrain blocks, sky"
NEG_CHAR = ("background scenery, multiple characters, cropped body, extra limbs, detailed facial features, mouth, nose, "
            "clothing details, armor ornaments, motion blur")
NEG_POSE = NEG_CHAR + ", overlapping duplicate poses, different proportions, longer limbs, realistic anatomy, muscles, finger details, stiff straight pose"

GOLDEN = "G1 G2 G3 G4 G5"
def refs(ch, extra=''):
    r = GOLDEN + (f" C{ch}-G1 C{ch}-G2" if ch else '')
    return (r + ' ' + extra).strip()

# ───────────────────────── 에셋 목록 ─────────────────────────
A = []
def add(id, cat, ch, typ, subject, file, ref_extra='', control='', neg='', note='', ar='', lock=False):
    A.append(dict(id=id, cat=cat, ch=ch, type=typ, subject=subject, file=file, ref=refs(ch, ref_extra),
                  control=control, neg_extra=neg, note=note, ar=ar, char_lock=lock))

# 0. 골든 이미지 (가장 먼저)
add('G1', '0 골든', 1, 'KEY', "wide game screenshot-like scene, a small paper-white child figure standing on pencil-hatched rock ledges "
    "in a half-drawn pencil forest cave, platforms at different heights, lots of empty paper air", 'golden/G1_scene.png', ar='16:9', note='전체 분위기 기준')
add('G2', '0 골든', 1, 'TERRAIN', "solid ground made of pencil hatching that varies naturally everywhere, with an organic wobbly top edge", 'golden/G2_ground.png', ar='1:1', neg=NEG_ENV, note='지형 질감 기준 (반복 없음)')
add('G3', '0 골든', 0, 'CHAR', "character sheet of the protagonist: " + CHAR_LOCK + ", standing pose, three views of the same side pose redrawn by hand",
    'golden/G3_player_sheet.png', ar='16:9', neg=NEG_CHAR, note='캐릭터 선 굵기 기준', lock=True)
add('G4', '0 골든', 1, 'CHAR', "a small graphite worm creature made of segmented pencil-lead pieces, one pointed tip at the front", 'golden/G4_enemy.png', ar='1:1', neg=NEG_CHAR, note='적 형태 언어 기준')
add('G5', '0 골든', 1, 'PROP', "a pencil-holder shaped stone statue holding three pencils, used as a save point", 'golden/G5_prop.png', ar='1:1', neg=NEG_ENV, note='소품 기준')
add('C1-G1', '0 골든', 1, 'KEY', "pencil forest mine tunnel scene with graphite deposits, plank platforms and a pool of ink", 'golden/C1_G1_scene.png', ar='16:9', neg=NEG_ENV)
add('C1-G2', '0 골든', 1, 'TERRAIN', "ground texture of a graphite mine wall, dense hatching with graphite flecks", 'golden/C1_G2_ground.png', ar='1:1', neg=NEG_ENV)
add('C2-G1', '0 골든', 2, 'KEY', "sunset crayon hills scene, thick crayon-painted ridges, melting orange crayon platforms, wax puddles", 'golden/C2_G1_scene.png', ar='16:9', neg=NEG_ENV)
add('C2-G2', '0 골든', 2, 'TERRAIN', "ground texture of overpainted crayon earth, layered crossing crayon strokes", 'golden/C2_G2_ground.png', ar='1:1', neg=NEG_ENV)

# 1. 지형 (반복 타일 대신: 큰 질감 원본 + 가장자리 띠 + 둥근 모서리 + 각각 다른 발판·특수 블록)
#    이 재료가 월드 전체를 한 줄로 잇는 '바탕 지형'(Unity Sprite Shape)이 됨 → 방 경계에서 끊기지 않음.
#    방 개성은 '3 방 장식'(투명 배경 오버레이)이 담당
TERRAIN_SET = [
    ('fill_a', 'TERRAIN', "interior of solid ground, hatching only, variation A"),
    ('fill_b', 'TERRAIN', "interior of solid ground, hatching only, variation B with a few faint eraser smudges"),
    ('fill_c', 'TERRAIN', "interior of solid ground, hatching only, variation C with sparse material flecks"),
    ('fill_d', 'TERRAIN', "interior of solid ground, hatching only, variation D slightly denser near one side"),
    ('edge_top', 'EDGE', "top surface edge of ground, thick wobbly contour line on top, material texture below"),
    ('edge_bottom', 'EDGE', "bottom edge of a ceiling, thick wobbly contour line on the bottom, material texture above"),
    ('edge_side', 'EDGE', "vertical wall edge (drawn horizontally, will be rotated), thick wobbly contour line, material texture on one side"),
    ('corner_out', 'CORNER', "outer convex corner of ground, rounded"),
    ('corner_in', 'CORNER', "inner concave corner where floor meets wall, soft curve"),
    ('platform_a', 'PLATFORM', "one-way plank platform, variation A"),
    ('platform_b', 'PLATFORM', "one-way plank platform, variation B, slightly bent"),
    ('platform_c', 'PLATFORM', "one-way plank platform, variation C, with a knot"),
]
CH_SPECIAL = {
    1: [('crumble', 'PLATFORM', "crumbling platform plank with pencil cracks, pieces about to break"),
        ('ink_hazard', 'EDGE', "pool surface of dark black-violet ink with a gently wavy surface line, dangerous"),
        ('ink_water', 'TERRAIN', "deep ink water body, semi-transparent black-violet, faint uneven surface lines"),
        ('scribble_wall', 'SPECIAL', "a tall wall made of angry chaotic scribbles in dusty red-gray"),
        ('graphite_vine', 'SPECIAL', "tall tangled graphite vines in sage gray-green"),
        ('wax_wall', 'SPECIAL', "tall smooth wax wall with vertical sheen stripes drawn with lines, beige, slippery looking"),
        ('target_door', 'SPECIAL', "a tall door panel painted muted yellow with a small target symbol"),
        ('oneway_gate', 'SPECIAL', "wooden one-way gate plank with a drawn downward arrow"),
        ('hatch', 'PLATFORM', "a wooden trapdoor hatch lying flat, hinge on one side")],
    2: [('melting', 'PLATFORM', "platform made of a melting orange crayon, wax drips hanging from its underside, thick line on top only"),
        ('crayon_swamp', 'EDGE', "surface of a melted brown-orange crayon swamp, thick wavy waxy surface line, dangerous"),
        ('wax_puddle', 'EDGE', "shallow pale beige wax puddle lying on the ground surface, sticky looking"),
        ('paint_water', 'TERRAIN', "pool of muted blue paint water, semi-transparent, faint uneven surface lines"),
        ('scribble_wall', 'SPECIAL', "a tall wall of chaotic scribbles drawn over with orange crayon, dusty red-gray"),
        ('graphite_vine', 'SPECIAL', "tall tangled graphite vines with crayon smudges, sage gray-green"),
        ('wax_wall', 'SPECIAL', "tall smooth beige wax wall with vertical sheen stripes drawn with lines, slippery looking"),
        ('paint_door', 'SPECIAL', "tall door painted muted purple with three vertical stripe slots for red, blue and yellow paint"),
        ('target_door', 'SPECIAL', "a tall door panel painted muted yellow with a small target symbol, crayon texture")],
}
AR = {'TERRAIN': '1:1', 'EDGE': '8:1', 'CORNER': '1:1', 'PLATFORM': '4:1', 'SPECIAL': '1:3'}
for ch in (1, 2):
    for key, typ, subj in TERRAIN_SET + CH_SPECIAL[ch]:
        add(f'T{ch}-{key}', '1 지형', ch, typ, subj, f'ch{ch}/terrain/ch{ch}_{key}.png', ar=AR[typ], neg=NEG_ENV,
            note='반복 금지: 같은 종류는 변형 여러 장을 섞어 씀' if typ in ('TERRAIN', 'PLATFORM') else '')

# 2. 배경 (구역마다 먼 배경·중경)
ZONES = {
    'A': (1, 'blank-paper fields', "rolling blank paper meadow with sparse pencil grass and half-drawn distant trees"),
    'B': (1, 'graphite mine', "deep graphite mine with wooden supports, hanging lamps drawn in pencil, ore veins"),
    'C': (1, 'sketch tree', "inside and around a giant sketched tree, knots, branches and hollow trunk"),
    'D': (1, 'tangled forest depths', "dense tangled forest of scribbled vines and dark knotted lines"),
    'E': (2, 'sunset ridges', "soft rolling hills under a crayon sunset sky, layered ridgelines"),
    'F': (2, 'wax workshop', "crayon-making workshop with wax vats, chimneys and drying racks"),
    'G': (2, 'painted canyon', "deep canyon with painted cliff walls, paint waterfalls and colored strata"),
    'H': (2, "knight's tower", "tall crayon tower with banners drawn in crayon, wax walls and a training yard"),
}
for z, (ch, name, subj) in ZONES.items():
    add(f'B{ch}-{z}-far', '2 배경', ch, 'BG_FAR', subj, f'ch{ch}/bg/ch{ch}_bg_{z}_far.png', ar='32:9', neg=NEG_ENV, note=name)
    add(f'B{ch}-{z}-mid', '2 배경', ch, 'BG_MID', subj, f'ch{ch}/bg/ch{ch}_bg_{z}_mid.png', ar='32:9', neg=NEG_ENV, note=name)
ZORDER = list(ZONES)   # 지도 왼쪽→오른쪽 이어 그리기 순서 (실제로 맞닿은 구역 쌍 기준으로 조정 가능)
for z1, z2 in zip(ZORDER, ZORDER[1:]):
    c1, n1, s1 = ZONES[z1]; c2, n2, s2 = ZONES[z2]
    for lay in ('far', 'mid'):
        add(f'BL-{z1}{z2}-{lay}', '2 배경', c2, 'BG_LINK', f"{lay} layer: {s1} on the left, gradually becoming {s2} on the right"
            + (", " + MAT[c1] + " gradually becoming " + MAT[c2] if c1 != c2 else ''),
            f'link/bg_{z1}{z2}_{lay}.png', ref_extra=f'B{c1}-{z1}-{lay} B{c2}-{z2}-{lay}', ar='32:9', neg=NEG_ENV,
            note=f'{n1} → {n2} 이음 패널 (양쪽 패널을 레퍼런스로, 아웃페인팅)')
add('B1-fore', '2 배경', 1, 'BG_FORE', "pencil-drawn grass blades and small paper scraps", 'ch1/bg/ch1_bg_fore.png', ar='32:9', neg=NEG_ENV)
add('B2-fore', '2 배경', 2, 'BG_FORE', "crayon grass tufts and tiny wax drips", 'ch2/bg/ch2_bg_fore.png', ar='32:9', neg=NEG_ENV)

# 3. 방 장식 (투명 배경 · 위치 참고 = Art/control/)
ROOMS1 = {1: "start path on blank paper, a few pencil grass tufts, gentle beginning", 2: "windy grass field, pencil grass bending, a narrow chimney opening above",
    3: "paper hill with folded paper slopes", 4: "shadowy grass cave, side exploration, narrow chimney in the ceiling",
    5: "crossroads forest hub, signpost drawn in pencil, many exits", 6: "forest loft with sharpened pencil turret nests and a target door",
    7: "mine entrance with wooden frame and minecart tracks", 8: "long graphite tunnel with ink pits",
    9: "collapsed mine shaft, cracked walls, ink pool at the bottom, crumbling planks", 10: "miners' rest area, lantern and bench, calm",
    11: "foreman's quarry, piles of graphite, eraser shavings everywhere", 12: "small storage behind vines with crates",
    13: "knot room inside the sketch tree", 14: "tall hollow tree tower with crumbling step planks",
    15: "treetop branch walkway high above, leaves drawn in pencil", 16: "erased nest, faint half-erased bird nest", 17: "bird nest room with feathers",
    18: "vine bridge across a gap, hanging vines", 19: "upper fork with ink puddle to the east", 20: "hunter's den with tangled vines and bones of scribbles",
    21: "rest area before the boss nest, quiet", 22: "round boss arena nest made of tangled angry scribbles", 23: "root slope descending, roots drawn in pencil, a lever",
    24: "tiny secret room behind scribbles", 25: "small study behind a target door with books drawn in pencil", 26: "wax nest at the top of a wax shaft",
    27: "flooded tunnel end under ink water", 28: "cloud shelf high up, pencil clouds", 29: "hidden den with an unfinished dog drawing", 30: "sealed torn-page cave with a long ink pool"}
ROOMS2 = {1: "paint-smeared gate where pencil turns into crayon", 2: "first ridge with melting crayon platforms", 3: "sunset hill slopes",
    4: "three-color plaza hub with a big paint door", 5: "brazier workshop with melting platforms over swamp", 6: "workshop entrance",
    7: "wax storage with sticky wax puddles and shelves", 8: "erased painting storage behind scribbles", 9: "red paint bucket room",
    10: "turtle's den, rounded cave", 11: "pinwheel hill high up with melting stepping platforms", 12: "canyon midway ledge",
    13: "paint storage behind vines", 14: "across the color bar bridge over a cliff", 15: "canyon floor with a villager covered in paint layers",
    16: "canyon target room", 17: "melted crayon vein over a swamp", 18: "tall paint waterfall hall with a wax east wall",
    19: "rest before the knight", 20: "knight's training yard arena with banners", 21: "small crevice beside the tower",
    22: "tower top with sunset sky path to the west", 23: "crevice behind the waterfall", 24: "flooded puddle cave", 25: "windy hill beyond the swamp",
    26: "sun attic, small hidden room with an uncolored sun drawing"}
for n, s in ROOMS1.items():
    add(f'R1-{n:02d}', '3 방 장식', 1, 'DECOR', 'decorations for: ' + s, f'ch1/decor/ch1_decor_{n:02d}.png', control=f'Art/control/r1_{n:02d}.png', neg=NEG_ENV + NEG_DECOR, ar='16:9')
for n, s in ROOMS2.items():
    add(f'R2-{n:02d}', '3 방 장식', 2, 'DECOR', 'decorations for: ' + s, f'ch2/decor/ch2_decor_{n:02d}.png', control=f'Art/control/r2_{n:02d}.png', neg=NEG_ENV + NEG_DECOR, ar='16:9')
add('R-border-1-2', '3 방 장식', 2, 'DECOR', "decorations for the transition zone: pencil scribbles on the left, crayon strokes slowly painted over them toward the right",
    'border/border_1_2_decor.png', control='Art/control/border_1_2.png', neg=NEG_ENV + NEG_DECOR, ar='16:9', note='경계 장식')
# 1↔2장 전환 지형 (바탕 지형이 끊기지 않게 재료가 서서히 바뀌는 재료 세트)
for key, typ, subj in [('fill_a', 'TERRAIN', "interior of solid ground, variation A"), ('fill_b', 'TERRAIN', "interior of solid ground, variation B"),
                       ('edge_top', 'EDGE', "top surface edge of ground, thick wobbly contour line on top"),
                       ('edge_bottom', 'EDGE', "bottom edge of a ceiling, thick wobbly contour line on the bottom")]:
    add(f'T12-{key}', '1 지형', 2, typ, subj + ", transition: " + MAT[1] + " on the left gradually overpainted by " + MAT[2] + " toward the right",
        f'border/terrain_12_{key}.png', ref_extra='T1-fill_a T2-fill_a', ar=AR[typ], neg=NEG_ENV, note='1→2장 전환 구간 전용 (2~3화면 폭)')

# 4. 소품 · 구조물
PROPS = [
    (1, 'save_statue', "a pencil-holder shaped stone statue holding three pencils, save point"),
    (2, 'save_statue', "a crayon-box shaped stone statue holding three crayons, save point"),
    (0, 'ribbon_end', "a standing bookmark ribbon stuck in the ground, muted red, notched top, gently curving"),
    (0, 'ribbon_secret', "a torn paper scrap with a slate-blue bookmark ribbon stuck in the ground"),
    (0, 'ribbon_knot', "a standing bookmark ribbon tied with a black knot in the middle, pale blue-gray"),
    (1, 'sketch_dog_unfinished', "a dog drawn in a clumsy child's drawing style, body and head finished, its four legs missing and shown only as faint dashed lines"),
    (1, 'sketch_dog_finished', "the same clumsy child-drawn dog now with four short legs drawn, happy, tail up"),
    (2, 'sketch_sun_unfinished', "a sun drawn in a clumsy child's drawing style, round face with dot eyes and a flat mouth, uncolored paper-white, eight rays as faint dashed lines"),
    (2, 'sketch_sun_finished', "the same child-drawn sun colored with orange crayon, smiling, eight solid crayon rays"),
    (0, 'orb_heal', "small floating orb of muted rose paint in a tiny jar"),
    (0, 'orb_shield', "small folded paper shield charm"),
    (0, 'orb_paint', "small jar of muted blue paint droplets"),
    (2, 'healup_bottle', "a small bottle of rose healing paint with a cork"),
    (0, 'heart_piece', "half of a hand-drawn heart cut from paper, muted rose"),
    (0, 'graphite_shard', "a small shard of graphite, diamond shaped, dark gray"),
    (0, 'ability_orb', "a softly floating paper orb with a pencil swirl inside, ability pickup"),
    (1, 'lever', "a wooden lever drawn in pencil, standing on a base"),
    (1, 'target', "a round target on a short stick, yellow and white rings"),
    (2, 'brazier_lit', "a small crayon-drawn brazier bowl with an orange crayon flame"),
    (2, 'brazier_off', "the same brazier bowl, flame out, a thin wisp of scribbled smoke"),
    (2, 'paint_bucket_red', "a paint bucket with a muted red paint splash, crayon texture"),
    (2, 'paint_bucket_blue', "a paint bucket with a muted blue paint splash, crayon texture"),
    (2, 'paint_bucket_yellow', "a paint bucket with a muted yellow paint splash, crayon texture"),
    (2, 'color_bar', "a tall standing crayon stick, orange, pointed top, can fall like a bridge"),
    (2, 'villager_painted', "a round villager covered in too many layers of purple crayon, barely able to move, sad"),
    (0, 'diary_page', "a small torn diary page with childish handwriting lines (no readable text)"),
]
for ch, key, subj in PROPS:
    c = ch or 1
    add(f'P-{key}' + (f'-{ch}' if ch else ''), '4 소품·구조물', c, 'PROP', subj, f'{"common" if ch==0 else f"ch{ch}"}/props/{key}.png',
        neg=NEG_ENV if 'villager' not in key and 'sketch' not in key else NEG_CHAR, ar='1:1',
        note='기능색(리본 붉은색·금빛·장미색·주황)은 사람이 마지막에 칠함' if key.startswith(('ribbon', 'orb', 'heart', 'sketch', 'target')) else '')

# 5. 캐릭터 · 키 포즈 (18 문서 §6)
PLAYER_POSES = [
    ('idle_breath', ["standing relaxed, weight centered, head slightly tilted"]),
    ('idle_look', ["turning head to look back over its shoulder, body still facing right", "looking up curiously, head tilted back slightly"]),
    ('idle_redraw', ["looking down at its own faded arm line, touching it with the other hand", "shaking its redrawn arm proudly"]),
    ('idle_stretch', ["stretching both arms high above head, body stretched tall", "head tilted back yawning, arms relaxed"]),
    ('idle_sit', ["sitting on the ground with legs out, head nodding sleepily"]),
    ('run_start', ["leaning forward strongly, back leg pushing off, arms swinging back", "first long stride, front leg reaching forward"]),
    ('run_loop', ["contact: front heel touching ground, legs wide, body at mid height", "down: front knee bent, body lowest, back leg lifting",
                  "passing: legs crossing under the body, body rising", "up: pushing off, body highest, arms at widest swing"]),
    ('run_stop', ["braking: leaning back, front foot planted, arms thrown forward", "settling into a relaxed stance"]),
    ('run_turn', ["skidding turn: body twisting, one foot planted, head already looking the new direction"]),
    ('jump', ["quick crouch, knees bent, arms back", "body stretched upward, arms raised, legs trailing", "floating at the top, limbs relaxed and spread",
              "falling, arms up, legs reaching down", "landing squash, knees bent, body wider"]),
    ('land_hard', ["heavy landing, deeply squashed, one hand touching the ground"]),
    ('wall_slide', ["pressed against a wall, one hand and one foot on the wall, sliding down"]),
    ('wall_jump', ["coiled against the wall", "kicking off the wall, body twisting away"]),
    ('dash', ["low crouch before dashing", "body stretched horizontally, dashing, eraser dust trail behind", "compressed stop after dash"]),
    ('attack1', ["wind-up: eraser arm pulled far back, body twisted", "swing contact: arm fully extended forward", "follow-through: arm crossed in front, leaning forward"]),
    ('attack2', ["reverse backhand wind-up", "backhand contact", "upward backhand finish"]),
    ('attack3_fold', ["big leap wind-up, eraser raised over head", "slamming down, body folded like paper", "recovering from the slam"]),
    ('dash_slash', ["lunging forward, body stretched horizontally, eraser leading"]),
    ('parry', ["eraser held up in front to block, braced stance", "recoiling from a successful parry, golden spark at the eraser"]),
    ('counter', ["spinning counter swing"]),
    ('charge', ["crouched gathering an ink stroke, trembling", "releasing a heavy ink stroke, whole body thrust forward"]),
    ('hurt', ["recoiling backward from a hit, body bent, outline partly smudged"]),
    ('death', ["knocked back into the air", "kneeling, one hand on the ground, trembling", "collapsed flat on the ground like paper", "being erased from the top, eraser crumbs flying"]),
    ('respawn', ["being drawn back from the feet up with a pencil line"]),
    ('heal', ["rubbing rose paint onto its own arm, lines becoming clearer"]),
    ('throw', ["throwing a paint droplet forward"]),
    ('draw', ["leaning toward a drawing, holding a pencil, drawing a line"]),
    ('ability_get', ["lifting a glowing paper orb with both hands", "looking at its own clearer hand"]),
    ('climb', ["climbing a wax wall by drawing crayon footholds"]),
    ('swim', ["paddling in paint water, little splash"]),
    ('glide', ["gliding with folded paper wings, legs dangling"]),
]
for anim, poses in PLAYER_POSES:
    for i, p in enumerate(poses, 1):
        add(f'PL-{anim}-{i}', '5 플레이어 키 포즈', 0, 'POSE', p, f'common/player/keyposes/pl_{anim}_key{i}.png', ref_extra='G3(1순위)',
            neg=NEG_POSE, ar='1:1', lock=True, note='프레임 위치·타이밍은 18 문서 §6')

# 6. 적 · 엘리트 · 보스 (디자인 시트 + 키 포즈)
ENEMIES = [
    (1, 'graphite_worm', "small worm made of segmented pencil-lead pieces, one sharp tip at the front"),
    (1, 'graphite_worm_red', "the same segmented worm with a dusty red tint and three small spikes on its back, slightly bigger"),
    (1, 'sharpener_turret', "small pencil sharpener turret with a crank handle and a hole that shoots shavings"),
    (1, 'paper_moth', "small moth made of folded paper with simple pencil patterns on its wings"),
    (1, 'crumpled_ball', "crumpled paper ball creature with two dot eyes peeking from the creases"),
    (1, 'graphite_mole', "mole made of graphite with big digging paws, mostly hidden under a dirt mound"),
    (1, 'eraser_spider', "spider with an eraser body hanging on a pencil-drawn thread"),
    (1, 'ink_blob', "bouncy black-violet ink droplet creature"),
    (2, 'crayon_soldier', "small soldier made of an orange crayon, pointed crayon tip as a helmet, holding a crayon spear"),
    (2, 'wax_thrower', "lumpy beige wax creature that throws wax balls"),
    (2, 'pigment_bat', "bat made of colored crayon dust, muted sage wings"),
    (2, 'melt_drip', "small creeping drop of melted orange crayon leaving a trail"),
    (1, 'range_ink_shooter', "small pencil sharpener turret variant with a crayon-stained base (appears in chapter 2 too)"),
]
ELITES = [
    (1, 'foreman_eraser', "big rectangular eraser foreman with a worn corner and a helmet lamp drawn in pencil, charges forward", "one worn rounded corner"),
    (1, 'vine_hunter', "tall hunter made of tangled graphite vines with a vine whip arm", "a whip arm of vines"),
    (2, 'sharpened_turtle', "turtle with a shell made of a sharpened crayon stub, the front of the shell blocks attacks, soft back", "a sharpened crayon-stub shell"),
]
BOSSES = [
    (1, 'tangled_scribble', "large boss made of a giant angry tangle of scribbled lines, two dot eyes inside the mess, a small bird drawing hidden at its core", "a ball of angry scribbles"),
    (2, 'broken_crayon_knight', "large knight made of a thick red-orange crayon, its spear is a crayon snapped in half, proud stance", "a spear snapped in half"),
]
ENEMY_POSES = ["idle stance", "wind-up telegraph, body coiled and color slightly brighter", "attack at full extension", "hurt recoil", "defeated, breaking apart into its drawing material"]
for ch, key, subj in ENEMIES:
    add(f'E-{key}', '6 적', ch, 'CHAR', subj, f'ch{ch}/enemies/{key}_sheet.png', neg=NEG_CHAR, ar='1:1', note='기준 시트 (이걸 확정한 뒤 키 포즈)')
    for i, p in enumerate(ENEMY_POSES, 1):
        add(f'E-{key}-k{i}', '6 적', ch, 'POSE', f"{subj}, {p}", f'ch{ch}/enemies/keyposes/{key}_key{i}.png', ref_extra=f'E-{key}', neg=NEG_POSE.replace(', different proportions', ''), ar='1:1')
for ch, key, subj, feat in ELITES:
    add(f'EL-{key}', '7 엘리트', ch, 'CHAR', f"larger elite enemy, 1.5 to 2 times bigger, one defining feature: {feat}, {subj}", f'ch{ch}/elites/{key}_sheet.png', neg=NEG_CHAR, ar='1:1')
    for i, p in enumerate(ENEMY_POSES + ["exhausted vulnerable state after a failed attack"], 1):
        add(f'EL-{key}-k{i}', '7 엘리트', ch, 'POSE', f"{subj}, {p}", f'ch{ch}/elites/keyposes/{key}_key{i}.png', ref_extra=f'EL-{key}', neg=NEG_POSE, ar='1:1')
for ch, key, subj, feat in BOSSES:
    add(f'BS-{key}', '8 보스', ch, 'CHAR', f"large boss, fills one third of the frame, one broken or unfinished part as its symbol: {feat}, {subj}", f'ch{ch}/bosses/{key}_sheet.png', neg=NEG_CHAR, ar='1:1')
    for i, p in enumerate(["idle, menacing but sad", "long slow wind-up", "fast attack at full extension", "stuck or exhausted, vulnerable",
                           "phase two, more broken", "defeated, falling apart and revealing what is inside"], 1):
        add(f'BS-{key}-k{i}', '8 보스', ch, 'POSE', f"{subj}, {p}", f'ch{ch}/bosses/keyposes/{key}_key{i}.png', ref_extra=f'BS-{key}', neg=NEG_POSE, ar='1:1')

# 7. 이펙트
FX = [(0, 'paper_scraps', "small torn paper scraps flying"), (0, 'pencil_ring', "a thin pencil-drawn circle expanding"),
      (0, 'hit_spark', "short crossed pencil strokes marking an impact"), (0, 'smear_swing', "a curved streak of pencil strokes for a fast eraser swing"),
      (0, 'parry_flash', "a small golden-yellow star burst drawn with crayon lines"), (0, 'eraser_crumbs', "pinkish gray eraser crumbs scattering"),
      (0, 'erase_wipe', "a band of eraser marks wiping across"), (0, 'redraw_line', "a single pencil line being drawn"),
      (0, 'dust_land', "small puff of paper dust on landing"), (1, 'graphite_dust', "graphite powder puff"), (1, 'ink_splash', "black-violet ink splash and drops"),
      (2, 'crayon_crumbs', "crumbs of orange and beige crayon"), (2, 'wax_drip', "drops of melted wax falling"), (2, 'crayon_flame', "small orange crayon-drawn flame"),
      (2, 'scribble_smoke', "a wisp of scribbled gray smoke")]
for ch, key, subj in FX:
    add(f'FX-{key}', '9 이펙트', ch or 1, 'FX', subj, f'{"common" if ch==0 else f"ch{ch}"}/fx/{key}.png', neg=NEG_ENV, ar='1:1')

# 8. UI
UI = [('heart_full', "a hand-drawn heart icon filled with muted rose"), ('heart_half', "a hand-drawn heart icon half filled with muted rose"),
      ('heart_empty', "an empty hand-drawn heart outline"), ('item_note', "a small paper note taped on the corner, for an item slot"),
      ('ability_wj', "small doodle icon of a hand gripping a wall"), ('ability_cut', "small doodle icon of a sharpened pencil"),
      ('ability_dash', "small doodle icon of an eraser with speed lines"), ('ability_parry', "small doodle icon of a deflected wax ball"),
      ('ability_climb', "small doodle icon of a crayon climbing a wall"), ('map_page', "a sketchbook page used as the map screen background"),
      ('title_underline', "a single hand-drawn crayon underline stroke"), ('pencil_hint', "a tiny pencil icon pointing down"),
      ('up_hint', "a tiny hand-drawn caret arrow pointing up")]
for key, subj in UI:
    add(f'UI-{key}', '10 UI', 1, 'UI', subj, f'common/ui/{key}.png', neg=NEG_ENV, ar='1:1', note='글자는 엔진에서 폰트로')

# 9. 키 아트
add('K-cover', '11 키 아트', 1, 'KEY', "the sketchbook cover with an empty title area, worn edges, a small paper figure sitting on it", 'common/key/cover.png', ar='16:9', note='제목 Unfin은 손글씨로 합성')
add('K-ch1', '11 키 아트', 1, 'KEY', "chapter one title page: pencil forest drawn in graphite, a tiny paper figure at the edge", 'ch1/key/ch1_title.png', ar='16:9')
add('K-ch2', '11 키 아트', 2, 'KEY', "chapter two title page: crayon hills at sunset, the broken crayon knight far away", 'ch2/key/ch2_title.png', ar='16:9')

# ───────────────────────── 조립 ─────────────────────────
def assemble(a):
    # 순서: 무엇을(SUBJECT) → 캐릭터 고정 → 에셋 종류 → 재료 → 마스터 → 통일
    # (이미지 AI는 앞쪽 단어를 더 강하게 반영. 그림체 통일은 뒤쪽 블록 + 골든 레퍼런스가 책임짐)
    parts = [a['subject']]
    if a['char_lock']: parts.append(CHAR_LOCK)
    parts.append(TYPE[a['type']])
    if a['ch']: parts.append(MAT[a['ch']] + ', palette: ' + PALETTE_WORDS[a['ch']])
    parts += [MSB, UNIFY]
    prompt = ', '.join(parts)
    neg = NEG_COMMON + (', ' + a['neg_extra'] if a['neg_extra'] else '')
    return prompt, neg

def main():
    os.makedirs(ROOT, exist_ok=True)
    rows = []
    A.sort(key=lambda a: int(a['cat'].split()[0]))   # 분류 순서대로 (안정 정렬)
    for a in A:
        p, n = assemble(a); rows.append(dict(a, prompt=p, negative=n))
    json.dump(dict(version='MSB v2 · UNIFY v2 · MAT v1 · 지형 구조 v3 (이어진 바탕 + 방 장식)', blocks=dict(MSB=MSB, UNIFY=UNIFY, MAT=MAT, TYPE=TYPE, CHAR_LOCK=CHAR_LOCK,
              NEG_COMMON=NEG_COMMON, NEG_ENV=NEG_ENV, NEG_DECOR=NEG_DECOR, NEG_CHAR=NEG_CHAR, NEG_POSE=NEG_POSE), assets=A),
              open(os.path.join(ROOT, 'asset_catalog.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    with open(os.path.join(ROOT, 'prompts_full.csv'), 'w', encoding='utf-8-sig', newline='') as f:
        w = csv.writer(f); w.writerow(['ID', '분류', '장', '종류', '파일', '비율', '레퍼런스', '컨트롤 이미지', '메모', '프롬프트', '네거티브'])
        for r in rows: w.writerow([r['id'], r['cat'], r['ch'], r['type'], 'Art/out/' + r['file'], r['ar'], r['ref'], r['control'], r['note'], r['prompt'], r['negative']])
    md = ['# Unfin 완성 프롬프트 (자동 생성 · 직접 고치지 말 것)', '', '> `python Tools/build_prompts.py`로 다시 만들어. 규칙은 Docs/19 문서.', '']
    cat = None
    for r in rows:
        if r['cat'] != cat:
            cat = r['cat']; md += ['', f'## {cat}', '']
        md += [f"### {r['id']} · `{r['file']}`", f"- 레퍼런스: {r['ref']}" + (f" · 컨트롤: `{r['control']}`" if r['control'] else '') + (f" · 비율 {r['ar']}" if r['ar'] else '') + (f" · {r['note']}" if r['note'] else ''),
               '```', r['prompt'], '```', '네거티브:', '```', r['negative'], '```', '']
    open(os.path.join(ROOT, 'ALL_PROMPTS.md'), 'w', encoding='utf-8').write('\n'.join(md))
    from collections import Counter
    c = Counter(r['cat'] for r in rows)
    print('에셋', len(rows), dict(c))

if __name__ == '__main__': main()
