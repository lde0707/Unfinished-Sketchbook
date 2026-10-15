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
import sys, json, csv, os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Art')

# ───────────────────────── 블록 ─────────────────────────
MSB = ("stylized 2D side-scrolling game art with a soft graphic design look, looks like a screen from a polished indie metroidvania game, "
       "set inside a sketchbook world, clean confident outlines with crisp smooth curves and consistent line weight, no wobble, no sketchy or rough lines, "
       "soft rounded shapes, flat color fills with one soft shade tone and one soft light tone, gentle subtle gradients allowed only inside shapes, "
       "simplified readable game shapes, not realistic, not photographic, "
       "muted warm gray-beige page tone like a page seen under a dim desk lamp, mid-key overall brightness (neither bright nor dark), "
       "a very subtle uniform paper texture and a very faint square grid, "
       "muted art-supply colors with low saturation, dark ink-gray outlines (not pure black), "
       "outline weight thick and consistent on gameplay objects, thinner on background, "
       "matte, flat lighting, calm melancholic mood with a quiet tension of adventure and combat, clear readable silhouettes, orthographic side view, "
       "quiet composition with breathing room, the unfinished theme is shown with clean dashed outlines and unfilled outline-only areas, never with messy strokes, "
       "high-resolution clean illustration (not pixel art), smooth anti-aliased edges, "
       "every wall and floor surface designed uniquely with no repeating pattern")

UNIFY = ("UNIFIED LOOK: identical flat ambient light from the front for every asset, no directional light, "
         "no cast shadows, no glossy highlights, no rim light, only a soft faint contact shadow under grounded objects; "
         "every outline uses the same dark ink-gray, the same crisp clean stroke with rounded line caps, and the same line weight for the same layer; "
         "the same corner rounding on all shapes; shading is always one soft shade tone and one soft light tone inside each shape, "
         "the shade tone always on the lower-right side of a shape; surface patterns are clean parallel lines at 45 degrees (top-right to bottom-left) "
         "or clean simple motifs, never sketchy pencil hatching; the same very subtle paper texture over everything; same muted color temperature; "
         "looks like part of one continuous designed game; "
         "MID-KEY VALUES: the page paper sits around 70 percent brightness, far background slightly darker than the paper, "
         "middle background darker again, terrain mid-dark, outlines dark ink-gray, the protagonist is the lightest shape on screen, "
         "no large pure white areas, no crushed pure black areas, moderate contrast that keeps combat easy to read; "
         "GAME LOOK: stylized and simplified like game art, details grouped into big simple masses, shapes read at a glance, "
         "no realistic micro-detail, no painterly realism; "
         "CLEAN AND SMOOTH: crisp smooth outlines, soft rounded corners, no hard square tile edges, "
         "no repeated stamps or copy-pasted texture, pattern density and placement vary naturally across every surface")

MAT = {
    1: ("pencil-themed world: graphite-gray near-monochrome palette with warm grays, shapes inspired by pencils, erasers and graphite, "
        "clean diagonal stripe patterns that echo clean diagonal pencil-stripe pattern, small pinkish eraser-crumb dots, trees as simple clean pencil-line shapes"),
    2: ("crayon-themed world: warm muted palette of orange, beige and dusty red with brown-tinted outlines, chunky rounded crayon-like shapes, "
        "clean wavy stripe motifs that echo crayon stripe motifs, smooth wax-drip blob shapes"),
    3: ("watercolor-themed world: misty pale blue, gray-blue and dusty pink, soft smooth gradient washes inside clean shapes, "
        "round water-drop and ring motifs"),
    4: ("colored-paper-themed world: flat single-color paper layers with one soft offset shadow, crisp fold lines, "
        "muted red, mustard yellow, sage green and slate blue"),
    5: ("ink-themed world: deep black-violet smooth ink-blot silhouettes, clean ink drip shapes, "
        "small fragments of the earlier chapters' colors glowing softly through"),
}
PALETTE_WORDS = {1: "toned-down warm gray paper, graphite gray, dark ink gray", 2: "dusky muted sunset orange, wax beige, brown-gray on toned-down beige paper",
                 3: "misty gray-blue, muted blue, dusty pink on toned-down blue-gray paper", 4: "muted red, mustard, sage, slate blue on toned-down cream paper",
                 5: "black-violet ink, dim faded paper, faint colored fragments"}

TYPE = {
    'TERRAIN': ("large terrain fill texture sheet for the floors, ceilings and walls of an enclosed interior, square, flat front view, no perspective, no lighting direction, "
                "fills the whole image, NO visible repeating motif, pattern lines vary in density, length and placement everywhere, "
                "irregular organic variation like one big designed surface, "
                "big-mass variation is required: darker denser patches and lighter sparse patches, one or two faint strata lines, "
                "a few areas left half-filled with paper showing through, a few small embedded material bits (pebbles and crumbs of the drawing material), "
                "very subtle uniform paper texture (not canvas, not fabric weave), never one flat even gray, "
                "seamless tileable texture in all four directions: the left edge continues exactly into the right edge and the top edge continues exactly into the bottom edge, no border, no frame, no vignette, no edge darkening, "
                "all variations share the same outer border band so any variation can sit beside or above any other without a seam"),
    'EDGE': ("long horizontal strip of a clean terrain edge on transparent background, very wide 4:1 strip, built in three zones: "
             "(1) a thick bold dark clean contour with an irregular rhythm of large bumps and small bends, a few chips, dents and one or two slight breaks, "
             "the contour rises and falls by about a quarter of the strip height, never a ruler line, never an even sine wave; "
             "(2) a dense darker surface band just inside the contour, about one fifth of the strip deep, with small details; "
             "(3) the interior beyond it gets lighter and sparser with a faint strata line and some half-filled paper showing; "
             "very subtle uniform paper texture (not canvas or fabric weave), same value and texture family as the matching floor or ceiling piece, soft anti-aliased edge, "
             "the left end continues exactly into the right end (same contour height, same band depth, same texture) so the strip repeats along any length without a seam"),
    'TERRAIN_LINK': ("transition sheet of solid terrain fill between two neighboring zones of the same chapter, square, flat front view, "
                     "same drawing material and value family on both sides, the first-zone side matches that zone's fill border band exactly, "
                     "the second-zone side matches the other zone's fill border band exactly, the zone details change gradually in between, "
                     "the two remaining edges continue into each other so it repeats along the whole boundary, very subtle uniform paper texture, no seam, no border"),
    'CORNER': ("single organic rounded corner piece of terrain on transparent background, soft natural anti-aliased edge, "
               "smooth clean curve, matches a continuous edge strip"),
    'PLATFORM': ("single long one-way platform plank on transparent background, side view, thick clean line on top, "
                 "soft natural edges, organic clean shape, no two planks identical"),
    'SPECIAL': ("single tall special terrain block on transparent background, side view, soft natural edges, "
                "designed as one unique object, not a repeating tile"),
    'BG_WALL': ("back wall of an enclosed interior space inside the sketchbook world, seen flat and straight on, fills the whole frame edge to edge, "
                "NO sky, NO horizon, NO ground, NO outdoor landscape, no up or down direction, it is repeated sideways and stacked upward to fill wide halls and tall shafts, "
                "large soft value variation (a few darker hollows and lighter worn patches), very low contrast, 5-12% darker than the page paper, "
                "faint stripe pattern and a few faint drawn cracks or marks, far-distance layer so details are soft and sparse, "
                "no focal point, no objects, no repeating motif, square, seamless tileable texture in all four directions: the left edge continues exactly into the right edge and the top edge continues exactly into the bottom edge, no border, no frame, no vignette, no edge darkening, "
                "all variations of the same area share the same outer border band so any variation can sit beside or above any other without a seam"),
    'BG_SET': ("single background set piece for an enclosed interior, isolated on transparent background, sits in the middle-distance layer behind the gameplay, "
               "simple readable silhouette, thin lines, low contrast, 15-20% darker than the page paper and lighter than the gameplay terrain, "
               "it will be placed in front of the back wall, so it must never look like it floats in the air: "
               "its ends are either cut off by the image edges or fade softly into the dark back wall, "
               "no flat horizontal tops that could look like walkable platforms (tops are rounded, slanted or ragged), "
               "soft natural anti-aliased edges, a few outline-only unfilled details"),
    'BG_WALL_TALL': ("tall back wall for one very high interior room or shaft, one single continuous image, flat straight-on view, fills the frame edge to edge, "
                     "NO sky, NO horizon, NO ground, the wall slowly changes from bottom to top to show progress while climbing, "
                     "seamless on all four sides: the left edge continues exactly into the right edge, "
                     "and the top and bottom border bands match the area's square back wall border band so it stacks above and below the square walls without a seam, "
                     "very low contrast, 5-12% darker than the page paper, faint stripe pattern, no objects, no repeating motif"),
    'BG_FORE': ("foreground frame piece of an enclosed interior on transparent background (hanging roots or rock lumps that frame the screen edge), "
                "dark soft silhouette, slightly faded, sparse, must not cover the middle of the screen, "
                "the left edge continues exactly into the right edge so it repeats sideways without a seam"),
    'DECOR': ("decoration overlay for one room on a fully transparent background, the provided layout shows where terrain is, "
              "do NOT draw the terrain itself, do NOT fill walls, floors or air, everything except the decorations stays transparent, "
              "place a few unique small details hugging the terrain surfaces: cracks, faded outline-only marks, little clean scribble motifs, "
              "roots, vines, small landmark objects that give this room its own character, "
              "decorations sit on or just inside terrain edges, keep walkable air and every platform top completely clear, "
              "soft natural anti-aliased edges, sparse, no repeated motif, 16:9"),
    'BG_WALL_LINK': ("transition sheet between the back walls of two neighboring interior areas, same rules as a back wall (no sky, no horizon, no objects, very low contrast), "
                     "one side matches the first area's wall texture exactly and it gradually becomes the second area's wall texture toward the other side, "
                     "the first-area side matches that area's wall border band exactly, the second-area side matches the other area's wall border band exactly, "
                     "the two remaining edges continue into each other so the transition can be repeated along the whole boundary, "
                     "no visible seam, same brightness, square"),
    'PROP': ("single game prop, isolated on transparent background, centered, entire object visible, side view, "
             "same outline weight as gameplay objects"),
    'CHAR': ("single 2D game sprite, side view facing right, full body visible, isolated on transparent background, "
             "simple readable silhouette, thick consistent outline, minimal interior detail, designed for smooth frame animation"),
    'POSE': ("single animation key pose, side view facing right, full body, isolated on transparent background, "
             "body weight clearly readable, line of action is a smooth curve"),
    'FX': ("clean 2D game effect, single frame, transparent background, no glow, no light, no bloom, "
           "made of drawing-material pieces"),
    'UI': ("clean graphic game UI element looking like a small paper note or doodle on a sketchbook, semi-transparent cream paper slightly lighter than the page, "
           "clean rounded border, transparent background, no text"),
    'KEY': ("illustration page from the same sketchbook, slightly more detailed than gameplay art but still stylized game art, same materials, outline, lighting and mid-key values, "
            "flat lighting, 16:9"),
}

CHAR_LOCK = ("the same tiny protagonist in every image: round head about 45% of total height, rounded-rectangle body, "
             "very short thin stick-like arms and legs drawn as single thick lines, one small dot eye, no mouth, no nose, "
             "chalk-white body (the lightest value on screen) with soft dark ink-gray outline, no colors, no clothes, no accessories, "
             "exact same proportions and line weight as the reference character sheet")

NEG_COMMON = ("pixel art, pixelated, 8-bit, 16-bit, retro game sprite, dot art, dithering, aliased jagged edges, stair-step edges, blocky, "
              "low resolution, mosaic, grid of square tiles, repeating tile pattern, tiled texture, copy-pasted stamps, identical repeated bricks, "
              "sketchy lines, rough pencil strokes, wobbly lines, messy hatching, scribbly texture, charcoal smudges, painterly brush strokes, rough texture, "
              "photorealistic, realistic painting, hyper-detailed, intricate realistic texture, realistic rendering, matte painting, "
              "realistic lighting, 3D render, CGI, voxel, generic corporate flat icons, clip art, stock vector illustration, anime cel shading, "
              "glossy, shiny, metallic, chrome, neon, glow, bloom, lens flare, light rays, rim light, specular highlight, "
              "directional shadows, cast shadow, strong rainbow gradients, airbrush glow, heavy drop shadow, pure black outline, high contrast HDR, "
              "overexposed, high-key, washed out, bright white paper, blown-out highlights, pitch black, crushed shadows, low-key noir, "
              "saturated rainbow colors, dark horror atmosphere, gore, text, letters, numbers, logo, watermark, signature, UI, frame border, "
              "blurry, jpeg artifacts, perspective view, isometric, top-down")
NEG_ENV = "characters, people, animals, creatures, faces"
NEG_BG = (", sky, horizon, open landscape, meadow, outdoor scenery, sunlight, clouds, birds, flying creatures, flying shapes, foreground grass, ground plane, "
          "centered symmetrical composition, empty edges, vignette framing, mixed foreground and background layers, identical repeated shapes, "
          "brick grid, tiled stones, canvas texture, fabric weave, high contrast, sharp detailed rendering, visible seams, mismatched edges, border frame, edge darkening, "
          "floating objects with a visible bottom, flat platform-like ledges, horizontal planks, bridges that look walkable")
NEG_BG_GLIMPSE = NEG_BG.replace('sky, horizon, open landscape, meadow, outdoor scenery, sunlight, clouds, ', '')
NEG_TERRAIN = ", canvas texture, fabric weave, flat even gray, uniform texture, perfectly even stripe pattern, ruler-straight edge, even sine wave edge, thin faint contour"
NEG_DECOR = ", solid ground fill, filled walls, full scene, paper background, opaque background, platforms, terrain blocks, sky"
NEG_CHAR = ("background scenery, multiple characters, cropped body, extra limbs, detailed facial features, mouth, nose, "
            "clothing details, armor ornaments, motion blur")
NEG_POSE = NEG_CHAR + ", overlapping duplicate poses, different proportions, longer limbs, realistic anatomy, muscles, finger details, stiff straight pose"

GOLDEN = "G1 G2 G3 G4 G5"
def refs(ch, extra=''):
    r = GOLDEN + (f" C{ch}-G1 C{ch}-G2" if ch else '')
    return (r + ' ' + extra).strip()

# ───────────────────────── 에셋 목록 ─────────────────────────
# AI가 실제로 뽑을 수 있는 크기와 최종 크기를 나눔 (대부분의 이미지 AI는 한 변 1024~1536px, 비율 1:1·3:2·2:3·16:9 정도만 됨)
AR_FIX = {'1:3': '1:2', '3:1': '2:1', '4:3': '3:2', '8:1': '4:1', '32:9': '2:1'}   # 최종 비율을 만들기 쉬운 값으로
GEN = {  # 최종 비율: (AI에 요청할 비율, 보통 나오는 크기, 최종 비율로 만드는 법)
    '1:1': ('1:1', '1024×1024', '그대로'),
    '3:2': ('3:2', '1536×1024', '그대로'),
    '2:3': ('2:3', '1024×1536', '그대로'),
    '16:9': ('16:9 (안 되면 3:2)', '1792×1024 또는 1536×1024', '3:2로 나왔으면 위아래를 잘라 16:9'),
    '1:2': ('2:3', '1024×1536', '좌우를 잘라 1:2 (768×1536)'),
    '2:1': ('3:2', '1536×1024', '위아래를 잘라 2:1 (1536×768)'),
    '4:1': ('3:2', '1536×1024', '가운데 가로 띠를 잘라 4:1 (1536×384)'),
}
FINAL_LONG = {'TERRAIN': 2048, 'TERRAIN_LINK': 2048, 'EDGE': 2048, 'CORNER': 1024, 'PLATFORM': 2048, 'SPECIAL': 2048,
              'BG_WALL': 2048, 'BG_WALL_LINK': 2048, 'BG_WALL_TALL': 4096, 'BG_SET': 2048, 'BG_FORE': 2048,
              'DECOR': 3840, 'KEY': 3840, 'PROP': 1024, 'CHAR': 1024, 'POSE': 1024, 'FX': 1024, 'UI': 512}
def final_size(ar, long):
    p, q = map(int, ar.split(':'))
    return (long, round(long * q / p)) if p >= q else (round(long * p / q), long)
A = []
def add(id, cat, ch, typ, subject, file, ref_extra='', control='', neg='', note='', ar='', lock=False):
    ar = AR_FIX.get(ar, ar)
    g = GEN.get(ar, (ar, '', '그대로'))
    fl = FINAL_LONG.get(typ, 1024); fw, fh = final_size(ar, fl) if ar else (fl, fl)
    how = g[2] + (f' → 일러스트용 업스케일러로 {fw}×{fh}' if typ not in ('BG_WALL_TALL',) else '')
    if typ == 'BG_WALL_TALL':
        how = '한 번에 못 뽑음 → tile_kit.py tall-base로 구획 뒷벽 A를 세로로 이은 밑그림(2048×4096)을 만들고, 1024 크기 조각씩 인페인팅해서 아래→위 변화를 그림'
    A.append(dict(id=id, cat=cat, ch=ch, type=typ, subject=subject, file=file, ref=refs(ch, ref_extra),
                  control=control, neg_extra=neg, note=note, ar=ar, char_lock=lock,
                  gen_ar=g[0], gen_size=g[1], final=f'{fw}×{fh}', final_long=fl, how=how))

# 0. 골든 이미지 (가장 먼저)
add('G1', '0 골든', 1, 'KEY', "wide game screenshot-like scene, a small chalk-white child figure standing on pencil-hatched rock ledges "
    "in a half-drawn pencil forest cave with a ceiling above, a faint hatched back wall behind (no sky), platforms at different heights, breathing room of empty toned paper, mid-key brightness", 'golden/G1_scene.png', ar='16:9', note='전체 분위기 기준')
add('G2', '0 골든', 1, 'TERRAIN', "solid ground made of clean diagonal pencil-stripe pattern: a bold irregular top contour, a dense dark surface band with pebbles and graphite flecks just below it, "
    "and a lighter sparser interior with a faint strata line and half-filled paper patches", 'golden/G2_ground.png', ar='1:1', neg=NEG_ENV + NEG_TERRAIN, note='지형 질감 기준 (반복 없음)')
add('G3', '0 골든', 0, 'CHAR', "character sheet of the protagonist: " + CHAR_LOCK + ", standing pose, three views of the same side pose redrawn by hand",
    'golden/G3_player_sheet.png', ar='16:9', neg=NEG_CHAR, note='캐릭터 선 굵기 기준', lock=True)
add('G4', '0 골든', 1, 'CHAR', "a small graphite worm creature made of segmented pencil-lead pieces, one pointed tip at the front", 'golden/G4_enemy.png', ar='1:1', neg=NEG_CHAR, note='적 형태 언어 기준')
add('G5', '0 골든', 1, 'PROP', "a pencil-holder shaped stone statue holding three pencils, used as a save point", 'golden/G5_prop.png', ar='1:1', neg=NEG_ENV, note='소품 기준')
add('C1-G1', '0 골든', 1, 'KEY', "pencil forest mine tunnel scene with graphite deposits, plank platforms and a pool of ink", 'golden/C1_G1_scene.png', ar='16:9', neg=NEG_ENV)
add('C1-G2', '0 골든', 1, 'TERRAIN', "ground texture of a graphite mine wall, dense stripe pattern with graphite flecks", 'golden/C1_G2_ground.png', ar='1:1', neg=NEG_ENV)
add('C2-G1', '0 골든', 2, 'KEY', "crayon cavern under the sunset hills, thick crayon-painted rock, cracks lined with muted sunset orange, melting orange crayon platforms, wax puddles", 'golden/C2_G1_scene.png', ar='16:9', neg=NEG_ENV)
add('C2-G2', '0 골든', 2, 'TERRAIN', "ground texture of overpainted crayon earth, layered crossing crayon stripe motifs", 'golden/C2_G2_ground.png', ar='1:1', neg=NEG_ENV)

# 1. 지형 (반복 타일 대신: 큰 질감 원본 + 가장자리 띠 + 둥근 모서리 + 각각 다른 발판·특수 블록)
#    이 재료가 월드 전체를 한 줄로 잇는 '바탕 지형'(Unity Sprite Shape)이 됨 → 방 경계에서 끊기지 않음.
#    방 개성은 '3 방 장식'(투명 배경 오버레이)이 담당
TERRAIN_SET = [
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
        ('wax_wall', 'SPECIAL', "tall smooth wax wall with vertical sheen stripes shown as clean lines, beige, slippery looking"),
        ('target_door', 'SPECIAL', "a tall door panel painted muted yellow with a small target symbol"),
        ('oneway_gate', 'SPECIAL', "wooden one-way gate plank with a drawn downward arrow"),
        ('hatch', 'PLATFORM', "a wooden trapdoor hatch lying flat, hinge on one side")],
    2: [('melting', 'PLATFORM', "platform made of a melting orange crayon, wax drips hanging from its underside, thick line on top only"),
        ('crayon_swamp', 'EDGE', "surface of a melted brown-orange crayon swamp, thick wavy waxy surface line, dangerous"),
        ('wax_puddle', 'EDGE', "shallow pale beige wax puddle lying on the ground surface, sticky looking"),
        ('paint_water', 'TERRAIN', "pool of muted blue paint water, semi-transparent, faint uneven surface lines"),
        ('scribble_wall', 'SPECIAL', "a tall wall of chaotic scribbles drawn over with orange crayon, dusty red-gray"),
        ('graphite_vine', 'SPECIAL', "tall tangled graphite vines with crayon smudges, sage gray-green"),
        ('wax_wall', 'SPECIAL', "tall smooth beige wax wall with vertical sheen stripes shown as clean lines, slippery looking"),
        ('paint_door', 'SPECIAL', "tall door painted muted purple with three vertical stripe slots for red, blue and yellow paint"),
        ('target_door', 'SPECIAL', "a tall door panel painted muted yellow with a small target symbol, crayon texture")],
}
AR = {'TERRAIN': '1:1', 'EDGE': '8:1', 'CORNER': '1:1', 'PLATFORM': '4:1', 'SPECIAL': '1:3'}
# 구역별 지형: 장 재료는 같고(한 지역), 구역마다 바닥·천장·벽의 디테일과 아주 옅은 색 기운이 다름
ZONE_TINT = {'A': "a faint warm cream-gray tint", 'B': "a faint cool blue-gray graphite tint", 'C': "a faint sage green-gray tint",
             'D': "a faint dusty rose-gray tint", 'E': "a faint warm orange-beige tint", 'F': "a faint wax-beige tint",
             'G': "a faint blue-violet gray tint", 'H': "a faint brick-rose gray tint"}
ZONE_EN = {'A': 'crumpled paper burrow', 'B': 'graphite mine', 'C': 'sketch tree interior', 'D': 'tangled root depths',
           'E': 'sunset crayon caverns', 'F': 'wax workshop', 'G': 'underground painted gorge', 'H': "knight's tower"}
ZONE_TERRAIN = {  # 구역: (장, 한국어, 속 채움 특징, 바닥 윗면 디테일, 천장 아랫면 디테일, 벽 옆면 디테일)
    'A': (1, '구겨진 종이굴', "layered creased paper sheets pressed together, soft paper pulp patches",
          "creased paper folds, torn paper flakes and a few pencil grass roots poking out", "peeling paper layers and dangling grass roots", "folded paper layer edges and small tears"),
    'B': (1, '흑연 광산', "dense graphite rock stripe pattern with matte ore seams and chisel marks",
          "graphite chunks, pickaxe chips and a few old rail spikes", "jagged graphite lumps and short graphite stalactite stubs", "pickaxe scars and ore seams"),
    'C': (1, '스케치 나무 속', "curved wood grain and growth-ring stripe pattern, small knots",
          "wood grain ridges, bark flakes and small root nubs", "hanging bark strips and knot bumps", "wood grain lines and split cracks"),
    'D': (1, '엉킨 뿌리 심부', "knotted tangled root fibers and scribble stripe pattern",
          "tangled root fibers, small thorn scribbles and crushed leaves", "hanging tangled root hair and thorny knots", "twisting root strands"),
    'E': (2, '노을 크레파스 굴', "thick layered crayon earth with crossing stripe motifs",
          "crayon crumbs and thin cracks lined with muted sunset orange", "melted crayon drips and orange-lined cracks", "layered crayon strata and smears"),
    'F': (2, '밀랍 공방', "smooth wax-beige plaster with drawn drip stains and soot smudges",
          "worn smooth wax edges, old wax drips and soot smudges", "hanging wax drips and soot stains", "plaster cracks and wax runs"),
    'G': (2, '색칠 지하 협곡', "uneven paint strata bands in muted blue, red and yellow",
          "dried paint flakes and paint drips at the edge", "dripping paint and strata ends", "colored strata bands and paint runs"),
    'H': (2, '기사의 탑', "uneven clean crayon stones, every stone a different shape, no grid",
          "chipped crayon stones with scratched mortar lines and a few small rubble bits", "worn stone undersides with crumbling mortar", "irregular stone faces with chips"),
}
for z, (ch, kname, fill, top, bottom, side) in ZONE_TERRAIN.items():
    tint = ZONE_TINT[z]
    for v in 'abc':
        add(f'TZ{ch}-{z}-fill_{v}', '1 지형', ch, 'TERRAIN', f"interior of solid ground in the {ZONE_EN[z]}: {fill}, {tint}, variation {v.upper()}",
            f'ch{ch}/terrain/{z}/ch{ch}_{z}_fill_{v}.png', ar='1:1', neg=NEG_ENV + NEG_TERRAIN,
            note=f'{z} {kname} · 속 채움 · 4방향 이어짐 · ' + ('테두리 원본 (먼저 만들기)' if v == 'a' else 'A의 테두리 띠 + 안쪽만 새로'))
    for key, subj in (('edge_top', f"walkable top surface edge of ground, the boldest contour on top so the floor reads instantly, surface band with {top}, material texture below"),
                      ('edge_bottom', f"bottom edge of a ceiling, bold contour on the bottom with a few sagging lumps, surface band with {bottom}, material texture above"),
                      ('edge_side', f"vertical wall edge (drawn horizontally, will be rotated), bold contour with chips and small ledges, surface band with {side}, material texture on one side")):
        add(f'TZ{ch}-{z}-{key}', '1 지형', ch, 'EDGE', f"{subj}, in the {ZONE_EN[z]}, {tint}", f'ch{ch}/terrain/{z}/ch{ch}_{z}_{key}.png',
            ar='8:1', neg=NEG_ENV + NEG_TERRAIN, note=f'{z} {kname} · 가장자리 띠 · 양 끝 이어짐')
for ch in (1, 2):
    for key, typ, subj in TERRAIN_SET + CH_SPECIAL[ch]:
        add(f'T{ch}-{key}', '1 지형', ch, typ, subj, f'ch{ch}/terrain/ch{ch}_{key}.png', ar=AR[typ], neg=NEG_ENV + (NEG_TERRAIN if typ in ('TERRAIN', 'EDGE') else ''),
            note=('4방향 이어짐 · 변형끼리 같은 테두리 띠 (fill_a가 테두리 원본)' if typ == 'TERRAIN' else '반복 금지: 같은 종류는 변형 여러 장을 섞어 씀' if typ == 'PLATFORM' else ''))

# 2. 배경 — 실내형 v6: 구역(8) → 배경 구획(22) → 방(56)
#    ① 구획마다 뒷벽 변형 3장 (하늘·지평선 없음, 위아래 방향 없음 → 수직굴도 채움)
#    ② 구역마다 공용 세트 조각 5개 (투명 낱개, 방마다 골라 배치)
#    ③ 방마다 대표 배경 조각 1개 (그 방에만 있는 그림 → 같은 구역이라도 방마다 다르게 보임)
#    ④ 맞닿은 구획 사이 뒷벽 이음 (Art/bg_links.json, seam_check.py)
ZONES = {   # 구역: (장, 영어 이름, 공용 세트 조각 [(키, 비율, 내용)])
    'A': (1, 'crumpled paper burrow',
          [('column', '1:3', "a column of stacked folded paper sheets reaching up"),
           ('roots', '1:3', "a long tangle of faint pencil grass roots hanging down"),
           ('opening', '4:3', "a torn opening in the paper wall framed by ragged paper edges, through it a very faint distant meadow sketch (the only outdoor glimpse)"),
           ('boulder', '3:2', "a crumpled paper boulder resting half in shadow"),
           ('support', '1:3', "a leaning paper-board support propping up the ceiling")]),
    'B': (1, 'graphite mine',
          [('frame', '1:1', "a wooden mine support frame of two posts and a cross beam"),
           ('lantern', '1:3', "an unlit lantern hanging on a long chain"),
           ('stalactites', '3:2', "a cluster of graphite stalactites hanging from above"),
           ('trestle', '3:1', "a broken minecart rail trestle tilted and collapsing in the distance"),
           ('ladder', '1:3', "a long rope ladder hanging down a shaft")]),
    'C': (1, 'sketch tree interior',
          [('root_arch', '3:1', "a thick root arching across the hollow"),
           ('fungus', '1:1', "shelf fungus growing in a column on the wood"),
           ('knot_hole', '1:1', "a knot hole letting in a faint glow of paper from outside"),
           ('moss', '1:3', "long strands of moss hanging down"),
           ('branch_stub', '3:2', "a broken branch stub reaching inward")]),
    'D': (1, 'tangled root depths',
          [('vine_curtain', '1:3', "a curtain of hanging tangled vines"),
           ('root_pillar', '1:3', "a twisted root pillar from floor to ceiling"),
           ('thorn_knot', '1:1', "a thorny scribble knot"),
           ('nest', '1:1', "an old bird nest wedged between roots, empty"),
           ('root_bridge', '3:1', "a sagging root bridge far in the back")]),
    'E': (2, 'sunset crayon caverns',
          [('stalagmite', '1:3', "a crayon stalagmite with a melted rounded tip"),
           ('glow_crack', '1:3', "a tall crack in the wall glowing with muted sunset orange crayon"),
           ('drips', '3:2', "hanging drips of melted crayon wax from above"),
           ('broken_pillar', '1:3', "a broken giant crayon standing as a pillar"),
           ('ledge', '3:1', "a far slanted crayon rock outcrop with wax runs")]),
    'F': (2, 'wax workshop interior',
          [('vat', '1:1', "a wax vat on a stand with a ladle"),
           ('rack', '3:2', "a drying rack full of crayons"),
           ('flue', '1:3', "a tall chimney flue pipe going up"),
           ('shelves', '1:1', "shelves of crayon boxes"),
           ('pulley', '1:3', "a hanging pulley with a long chain")]),
    'G': (2, 'underground painted gorge',
          [('paint_fall', '1:3', "a thin paint waterfall falling down a crevice"),
           ('arch', '3:2', "a natural rock arch with paint stains"),
           ('paint_stalactites', '3:2', "dripping paint stalactites"),
           ('boulder', '1:1', "a leaning boulder splashed with paint"),
           ('strata_spire', '1:3', "a tall striped rock spire")]),
    'H': (2, "knight's tower interior",
          [('banner', '1:3', "a long torn crayon banner hanging"),
           ('window', '1:2', "a tall arched tower window with a faint sunset sky inside it (framed glimpse)"),
           ('weapon_rack', '3:2', "a weapon rack with broken crayon spears"),
           ('stair', '1:2', "a segment of a spiral stair far in the back"),
           ('column', '1:3', "a stone column")]),
}
AREAS = {   # 배경 구획: (구역, 한국어 이름, 방 번호들, 뒷벽 문장) — 같은 구역 안에서도 구획마다 뒷벽이 다름
    'A1': ('A', '종이 틈 입구', [1, 2, 3], "the entrance of a paper burrow: layered creased paper sheets with thin bright cracks where the page above is torn, slightly lighter than deeper areas, faint pencil grass roots here and there"),
    'A2': ('A', '풀뿌리 갈림굴', [4, 5, 6, 25], "a deeper paper burrow: soft paper pulp walls densely threaded with dangling pencil grass roots and faint pencil arrow scribbles"),
    'B1': ('B', '광산 입구·갱도', [7, 8, 12, 27], "a timbered graphite mine tunnel wall: dark graphite rock with faint pickaxe marks and old rail scratches"),
    'B2': ('B', '깊은 채굴장', [9, 10, 11, 29], "a deep quarry wall: huge matte graphite veins, cracked and collapsed rock, heavy stripe pattern"),
    'B3': ('B', '찢긴 페이지 굴', [30], "a sealed cave where the rock turns into torn page layers with faint ruled lines and ink seeping through"),
    'C1': ('C', '나무 밑동 속', [13, 26], "the base inside a giant hollow tree: wide curved growth rings, knots and thick root bases"),
    'C2': ('C', '속 빈 줄기', [14], "the inside of a tall hollow trunk: long vertical wood grain lines and stacked growth rings, no up or down feeling"),
    'C3': ('C', '가지 속·둥지', [15, 16, 17], "a narrow branch tunnel inside the tree: fine bark grain, small knots, faint pencil feather marks"),
    'D1': ('D', '덩굴 다리 위층', [18, 19, 28], "an upper root ceiling layer: loose hanging vines and airy tangled root gaps"),
    'D2': ('D', '사냥꾼의 굴', [20, 21, 23], "a dense dark wall of tangled scribbled roots with old snares"),
    'D3': ('D', '낙서 둥지', [22, 24], "a wall of angry chaotic scribbles coiling in every direction, the most aggressive lines of chapter one"),
    'E1': ('E', '물감 번진 관문', [1], "a wall where clean diagonal pencil-stripe pattern is being overpainted by thick crayon smears"),
    'E2': ('E', '노을빛 굴', [2, 3, 11], "thick layered crayon earth with crossing stripe motifs and thin cracks lined with muted sunset-orange crayon"),
    'E3': ('E', '삼색 광장', [4], "a huge cavern wall of crayon earth with faint vertical paint drips in muted red, blue and yellow"),
    'F1': ('F', '공방 작업장', [5, 6], "workshop back wall of plastered wax-beige panels with soot marks near a furnace, uneven clean panels, no brick grid"),
    'F2': ('F', '밀랍 저장고', [7, 8, 9], "storage room wall of stacked wax blocks drawn unevenly, sticky drip stains"),
    'F3': ('F', '물 고인 아래층', [10, 24], "a damp lower cave of wax and crayon earth with water rings and soft rounded hollows"),
    'G1': ('G', '협곡 중턱', [12, 13, 14, 25], "an underground gorge wall of uneven layered paint strata in muted blue, red and yellow bands"),
    'G2': ('G', '협곡 바닥', [15, 16, 17], "the deep gorge floor wall: dark paint strata with dull melted crayon veins"),
    'G3': ('G', '물감 폭포 홀', [18, 23], "a wet paint-misted cavern wall with long vertical paint streaks"),
    'H1': ('H', '탑 아래층', [19, 20, 21], "tower hall interior wall of clean uneven crayon stones, every stone a different shape, no grid pattern"),
    'H2': ('H', '탑 위층', [22, 26], "upper tower interior wall of worn crayon stones with warm light patches from windows, every stone different"),
}
ROOM_BG = {  # 방마다 대표 배경 조각 (그 방에만 있음). TALL = 세로로 긴 방
    1: {1: "a sheet of blank paper peeling from the wall like a curtain, a single faint pencil line leading inward",
        2: "a narrow wind crack in the paper wall with paper scraps fluttering up toward a chimney",
        3: "crumpled paper slopes stacked behind like folded hills inside the burrow",
        4: "dangling pencil grass roots forming a dim curtain beside a tall narrow chimney",
        5: "a crossroads signpost in pencil-gray standing far back, three blank arrow boards",
        6: "sharpened pencil stubs stuck in the wall like little turret nests",
        7: "a mine entrance frame of wooden beams with a blank warning board",
        8: "a long receding row of timber supports along the tunnel",
        9: "a collapsed heap of broken beams and graphite chunks",
        10: "a miners' rest alcove with an unlit lantern, a bench and pinned-up child sketches",
        11: "a huge exposed graphite vein with pickaxe marks and a pile of eraser shavings",
        12: "storage crates stacked behind a curtain of graphite vines",
        13: "a large tree knot shaped like a closed eye in the wood",
        14: "a spiral of growth rings and step marks rising up the hollow trunk",
        15: "small pencil leaf shadows pressed against the bark of a branch tunnel",
        16: "a faded outline-only bird nest outline fading on the wood",
        17: "a woven bird nest with scattered pencil feathers",
        18: "long vine bridges sagging in the far darkness",
        19: "a fork of two root tunnels with a faint ink puddle gleam",
        20: "a trophy wall of scribble bones and tangled snares",
        21: "a quiet hollow with a soft paper glow, safe and still",
        22: "a giant knot of angry scribbles curling like a nest wall",
        23: "thick roots descending like stairs beside an old lever post",
        24: "a niche of tightly packed scribbles with one loose thread",
        25: "bookshelves of pencil-drawn books carved into the wall",
        26: "dripping wax honeycomb shapes stuck to the wood",
        27: "ink water lines staining the wall of a flooded tunnel end",
        28: "fluffy cotton-dust balls tangled in hanging roots",
        29: "a small den wall with a child's unfinished dog drawing pinned on it",
        30: "torn page edges peeling from the cave wall revealing faint ruled lines"},
    2: {1: "pencil lines on the wall being painted over by a thick smear of crayon",
        2: "melted crayon runs sliding down a far slope wall",
        3: "a wide crack glowing muted sunset orange across a big cavern",
        4: "three tall paint drips in muted red, blue and yellow running down a plaza wall",
        5: "a big brazier furnace with a chimney far behind",
        6: "a workshop door frame with a crayon-shaped blank sign",
        7: "tall shelves of wax blocks with sticky drips",
        8: "stacked faded outline-only canvases leaning against the wall",
        9: "a giant red paint bucket tipped over far behind",
        10: "a rounded den wall marked with a large shell-shaped scrape",
        11: "a ventilation shaft with a pinwheel fan turning high up",
        12: "a layered strata cliff with a faint winding crack",
        13: "paint jars hidden behind hanging vines",
        14: "a fallen color bar leaning diagonally across the far gorge",
        15: "paint-layered rocks and a toppled easel on the gorge floor",
        16: "a big target symbol painted on the gorge wall",
        17: "a melted crayon vein glowing dully through the rock",
        18: "a tall paint waterfall pouring from a crack high above",
        19: "a resting alcove with a drawn crayon campfire, unlit",
        20: "straw training dummies and banners in a tower hall",
        21: "crayon scratch marks in a narrow crevice",
        22: "battlements with wide window openings to a faint sunset (framed glimpse)",
        23: "a hidden crevice glowing faintly behind falling paint",
        24: "water rings on the wall of a damp puddle cave",
        25: "paper streamers blowing in a windy gorge passage",
        26: "a small attic wall with an uncolored sun drawing pinned on it"},
}
TALL = {(1, 4), (1, 14), (2, 18), (2, 11)}  # 대표 조각을 세로 1:2로 (좁고 높은 방)
ANCHOR_TXT = {   # 세트 조각이 공중에 떠 보이지 않게: 어디에 붙어 있는지
    'hang': "hanging from above: its top edge is cut off by the top of the image as if it continues past the ceiling",
    'rise': "rising from below: its base fades softly into the dark back wall, no visible bottom and no ground line",
    'span': "floor-to-ceiling: both the top and the bottom are cut off by the image edges, and the top edge continues exactly into the bottom edge so it can be stacked to any height without a seam",
    'wall': "drawn flat on the back wall itself, needs no ground or support",
}
ANCHOR_KO = {'hang': '⬇️ 매달림', 'rise': '⬆️ 솟음', 'span': '↕️ 바닥~천장', 'wall': '🧱 벽에 붙음'}
SET_ANCHOR = {  # 구역 공용 조각
    'A': dict(column='span', roots='hang', opening='wall', boulder='rise', support='span'),
    'B': dict(frame='span', lantern='hang', stalactites='hang', trestle='rise', ladder='hang'),
    'C': dict(root_arch='rise', fungus='wall', knot_hole='wall', moss='hang', branch_stub='wall'),
    'D': dict(vine_curtain='hang', root_pillar='span', thorn_knot='wall', nest='wall', root_bridge='hang'),
    'E': dict(stalagmite='rise', glow_crack='wall', drips='hang', broken_pillar='rise', ledge='rise'),
    'F': dict(vat='rise', rack='rise', flue='span', shelves='wall', pulley='hang'),
    'G': dict(paint_fall='span', arch='rise', paint_stalactites='hang', boulder='rise', strata_spire='rise'),
    'H': dict(banner='hang', window='wall', weapon_rack='rise', stair='rise', column='span'),
}
ROOM_ANCHOR = {  # 방 대표 조각
    1: {1: 'wall', 2: 'wall', 3: 'rise', 4: 'hang', 5: 'rise', 6: 'wall', 7: 'span', 8: 'span', 9: 'rise', 10: 'wall', 11: 'wall', 12: 'rise',
        13: 'wall', 14: 'span', 15: 'wall', 16: 'wall', 17: 'wall', 18: 'hang', 19: 'wall', 20: 'wall', 21: 'wall', 22: 'span', 23: 'rise',
        24: 'wall', 25: 'wall', 26: 'hang', 27: 'wall', 28: 'hang', 29: 'wall', 30: 'wall'},
    2: {1: 'wall', 2: 'wall', 3: 'wall', 4: 'wall', 5: 'rise', 6: 'wall', 7: 'wall', 8: 'rise', 9: 'rise', 10: 'wall', 11: 'span', 12: 'wall',
        13: 'hang', 14: 'rise', 15: 'rise', 16: 'wall', 17: 'wall', 18: 'span', 19: 'rise', 20: 'rise', 21: 'wall', 22: 'wall', 23: 'wall',
        24: 'wall', 25: 'hang', 26: 'wall'},
}
ROOM_BG_EXTRA = {  # 한 화면보다 높은 방: 높이마다 볼거리 (b = 중간·위, c = 맨 위) · 1장 천장 높이기(v34)로 큰 홀 5·7·10·11·18 추가
    (1, 4): [('b', 'hang', '1:1', "a narrow chimney mouth high up with paper scraps caught in it (upper part of the room)")],
    (1, 5): [('b', 'hang', '3:2', "tangled pencil roots and paper strips hanging from far above the crossroads, a faint signpost arrow caught in them (upper part of the hall)")],
    (1, 7): [('b', 'hang', '1:1', "an old mine hoist frame with a dangling empty bucket high above the entrance (upper part of the hall)")],
    (1, 9): [('b', 'wall', '3:2', "a broken pulley wheel and a snapped rope high on the wall (upper part of the room)")],
    (1, 10): [('b', 'wall', '1:1', "a high wooden shelf with rolled maps and a second unlit lantern (upper part of the room)")],
    (1, 11): [('b', 'hang', '3:2', "graphite stalactites and a hanging rail track high above the quarry (upper part of the hall)")],
    (1, 18): [('b', 'hang', '3:2', "more vine bridges crossing high in the dark, one snapped and dangling (upper part of the hall)")],
    (1, 14): [('b', 'wall', '1:1', "a row of small holes halfway up the trunk with faint light inside (middle of the tower)"),
              ('c', 'wall', '3:2', "branch gaps at the very top letting in soft paper light (top of the tower)")],
    (1, 22): [('b', 'hang', '1:1', "a scribble cocoon hanging high above the arena (upper part of the room)")],
    (2, 18): [('b', 'wall', '1:2', "paint drip streaks running down the wall halfway up (middle of the hall)"),
              ('c', 'wall', '3:2', "the crack high above where the paint waterfall begins (top of the hall)")],
    (2, 20): [('b', 'wall', '3:2', "high wall slits with faint warm light and a hanging chain (upper part of the hall)")],
}
TALL_WALLS = [  # 1.5화면 이상 높은 곳: 위아래로 바뀌는 세로 전용 뒷벽 한 장 (구획, 키, 이름, 비율, 아래→위 변화)
    ('C2', 'tower', '나무 속 탑 (방 14, 3.5화면)', '1:3', "inside a tall hollow trunk: bottom thick root bases and dark dense rings, middle long vertical grain and growth rings, top thinner wood with soft light patches from branch gaps"),
    ('A2', 'root_shaft', '일방통행 뿌리굴 (수직굴, 5.1화면)', '1:3', "a deep paper burrow shaft: bottom dense paper pulp with tangled grass roots, middle thinning roots, top torn paper layers with thin bright cracks"),
    ('C1', 'vine_shaft', '덩굴 수직굴 (수직굴, 2.4화면)', '1:3', "a vine shaft inside the tree: bottom root base hollow, middle hanging vines getting thicker, top knotty wood"),
    ('G3', 'hall', '물감 폭포 홀 (방 18, 1.7화면)', '1:2', "a paint waterfall cavern: bottom wet dark paint stains and mist marks, top drier layered strata near the crack where the waterfall begins"),
    ('G1', 'cliff_shaft', '협곡 낭떠러지 (수직굴, 2.5화면)', '1:3', "an underground gorge drop: bottom dark deep strata with dull melted crayon veins, middle banded strata, top lighter worn strata"),
    ('H1', 'tower_shaft', '크레파스 탑 (수직굴, 2.2화면)', '1:3', "inside the crayon tower shaft: bottom large heavy foundation stones, middle medium stones with banner hooks, top small worn stones with warm window light patches, every stone different"),
]
GLIMPSE = ('opening', 'window', 'knot_hole')
for aid, (z, kname, rooms, wall) in AREAS.items():
    ch, zname, _ = ZONES[z]
    for v in 'abc':
        add(f'B{ch}-{aid}-wall_{v}', '2 배경', ch, 'BG_WALL', f"{wall}, in the {zname}, {ZONE_TINT[z]}, variation {v.upper()}", f'ch{ch}/bg/ch{ch}_bg_{aid}_wall_{v}.png',
            ar='1:1', neg=NEG_ENV + NEG_BG, note=f'{z} 구역 · {kname} (방 {", ".join(map(str, rooms))}) · 뒷벽 변형 {v.upper()} · 4방향 이어짐 · ' + ('테두리 원본 (먼저 만들기)' if v == 'a' else 'A의 테두리 띠 그대로 + 안쪽만 새로 그리기'))
for z, (ch, zname, sets) in ZONES.items():
    for key, ar, subj in sets:
        an = SET_ANCHOR[z][key]
        add(f'B{ch}-{z}-set_{key}', '2 배경', ch, 'BG_SET', subj + f", in the {zname}, " + ANCHOR_TXT[an], f'ch{ch}/bg/ch{ch}_bg_{z}_set_{key}.png',
            ar=ar, neg=NEG_ENV + (NEG_BG_GLIMPSE if key in GLIMPSE else NEG_BG),
            note=f'{z} 구역 공용 세트 조각 · {ANCHOR_KO[an]}' + (' (세로로 긴 방·수직굴용)' if ar == '1:3' else ''))
for ch, rooms in ROOM_BG.items():
    for n, subj in rooms.items():
        aid = next(k for k, v in AREAS.items() if ZONES[v[0]][0] == ch and n in v[2])
        zname = ZONES[AREAS[aid][0]][1]
        an = ROOM_ANCHOR[ch][n]
        add(f'BR{ch}-{n:02d}', '2 배경', ch, 'BG_SET', f"room landmark: {subj}, in the {zname}, " + ANCHOR_TXT[an], f'ch{ch}/bg/rooms/ch{ch}_bg_room_{n:02d}.png',
            ar='1:2' if (ch, n) in TALL else '3:2', neg=NEG_ENV + (NEG_BG_GLIMPSE if 'glimpse' in subj else NEG_BG),
            note=f'{ch}장 {n}번 방 대표 배경 조각 (이 방에만) · 구획 {aid} · {ANCHOR_KO[an]}')
        for suf, an2, ar2, subj2 in ROOM_BG_EXTRA.get((ch, n), []):
            add(f'BR{ch}-{n:02d}{suf}', '2 배경', ch, 'BG_SET', f"room landmark: {subj2}, in the {zname}, " + ANCHOR_TXT[an2],
                f'ch{ch}/bg/rooms/ch{ch}_bg_room_{n:02d}{suf}.png', ar=ar2, neg=NEG_ENV + NEG_BG,
                note=f'{ch}장 {n}번 방 (한 화면보다 높음) 추가 대표 조각 {suf} · {ANCHOR_KO[an2]}')
for aid, key, kname, ar, subj in TALL_WALLS:
    z = AREAS[aid][0]; ch, zname, _ = ZONES[z]
    add(f'BT{ch}-{aid}-{key}', '2 배경', ch, 'BG_WALL_TALL', f"{subj}, in the {zname}", f'ch{ch}/bg/tall/ch{ch}_bg_{aid}_{key}.png',
        ar=ar, neg=NEG_ENV + NEG_BG, note=f'세로 전용 뒷벽 · {kname} · 아래→위로 바뀜 · 좌우 이어짐 + 위아래 띠 = 구획 뒷벽 A 테두리')
# Art/bg_areas.json 쓰고 → 맞닿은 구획 쌍 다시 계산 (seam_check.find_links) → 뒷벽 이음
json.dump({k: dict(zone=v[0], chapter=ZONES[v[0]][0], name=v[1], rooms=v[2]) for k, v in AREAS.items()},
          open(os.path.join(ROOT, 'bg_areas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seam_check
LINKS = seam_check.find_links(quiet=True)
for l in LINKS:
    a1, a2, d = l['first'], l['second'], l['dir']
    (z1, n1, _, w1), (z2, n2, _, w2) = AREAS[a1], AREAS[a2]
    c1, c2 = ZONES[z1][0], ZONES[z2][0]
    side = "left side matches {}, right side becomes {}" if d == 'h' else "top side matches {}, bottom side becomes {}"
    add(f'BL-{a1}{a2}-wall', '2 배경', c2, 'BG_WALL_LINK', "back wall transition: " + side.format(w1, w2)
        + (", " + MAT[c1] + " gradually becoming " + MAT[c2] if c1 != c2 else ''),
        f'link/bg_{a1}{a2}_wall.png', ref_extra=f'B{c1}-{a1}-wall_a B{c2}-{a2}-wall_a', ar='1:1', neg=NEG_ENV + NEG_BG,
        note=f"{n1} → {n2} 뒷벽 이음 ({'가로' if d == 'h' else '세로'}) · 검사: seam_check.py")
add('B1-fore', '2 배경', 1, 'BG_FORE', "hanging pencil roots and soft paper lumps framing the top and bottom screen edges", 'ch1/bg/ch1_bg_fore.png', ar='32:9', neg=NEG_ENV)
add('B2-fore', '2 배경', 2, 'BG_FORE', "hanging crayon wax drips and crayon rock lumps framing the top and bottom screen edges", 'ch2/bg/ch2_bg_fore.png', ar='32:9', neg=NEG_ENV)

# 3. 방 장식 (투명 배경 · 위치 참고 = Art/control/) — 실내 기준으로 다시 씀
ROOMS1 = {1: "start path in a paper burrow, a few pencil grass roots poking from the ceiling, gentle beginning",
    2: "windy paper crack passage, paper scraps and grass roots bending in the draft, a narrow chimney opening in the ceiling",
    3: "crumpled paper slope inside the burrow with folded paper ledges", 4: "shadowy grass-root cave, side exploration, narrow chimney in the ceiling",
    5: "crossroads hub cave, signpost in pencil-gray, many exits", 6: "loft above the crossroads with sharpened pencil turret nests and a target door",
    7: "mine entrance with wooden frame and minecart tracks", 8: "long graphite tunnel with ink pits",
    9: "collapsed mine shaft, cracked walls, ink pool at the bottom, crumbling planks", 10: "miners' rest area, lantern and bench, calm",
    11: "foreman's quarry, piles of graphite, eraser shavings everywhere", 12: "small storage behind vines with crates",
    13: "knot room inside the sketch tree", 14: "tall hollow tree tower with crumbling step planks",
    15: "branch tunnel inside the tree high up, bark walls", 16: "erased nest, faint faded outline-only bird nest", 17: "bird nest room with feathers",
    18: "vine bridge across a gap, hanging vines", 19: "upper fork with ink puddle to the east", 20: "hunter's den with tangled vines and bones of scribbles",
    21: "rest area before the boss nest, quiet", 22: "round boss arena nest made of tangled angry scribbles", 23: "root slope descending, roots in pencil-gray, a lever",
    24: "tiny secret room behind scribbles", 25: "small study behind a target door with books in pencil-gray", 26: "wax nest at the top of a wax shaft",
    27: "flooded tunnel end under ink water", 28: "cotton-dust shelf high in the root ceiling", 29: "hidden den with an unfinished dog drawing", 30: "sealed torn-page cave with a long ink pool"}
ROOMS2 = {1: "paint-smeared gate where pencil turns into crayon", 2: "first crayon slope cave with melting crayon platforms", 3: "large sunset-glow crayon cavern",
    4: "three-color plaza hub cavern with a big paint door", 5: "brazier workshop with melting platforms over swamp", 6: "workshop entrance",
    7: "wax storage with sticky wax puddles and shelves", 8: "erased painting storage behind scribbles", 9: "red paint bucket room",
    10: "turtle's den, rounded cave", 11: "pinwheel ventilation shaft high up with melting stepping platforms", 12: "underground gorge midway ledge",
    13: "paint storage behind vines", 14: "across the color bar bridge over a deep gorge", 15: "gorge floor with a villager covered in paint layers",
    16: "gorge target room", 17: "melted crayon vein over a swamp", 18: "tall paint waterfall hall with a wax east wall",
    19: "rest before the knight", 20: "knight's training hall arena with banners", 21: "small crevice beside the tower",
    22: "tower top room with wide windows and a path to the west", 23: "crevice behind the waterfall", 24: "flooded puddle cave", 25: "windy gorge passage beyond the swamp",
    26: "sun attic, small hidden room with an uncolored sun drawing"}
for n, s in ROOMS1.items():
    add(f'R1-{n:02d}', '3 방 장식', 1, 'DECOR', 'decorations for: ' + s, f'ch1/decor/ch1_decor_{n:02d}.png', control=f'Art/control/r1_{n:02d}.png', neg=NEG_ENV + NEG_DECOR, ar='16:9')
for n, s in ROOMS2.items():
    add(f'R2-{n:02d}', '3 방 장식', 2, 'DECOR', 'decorations for: ' + s, f'ch2/decor/ch2_decor_{n:02d}.png', control=f'Art/control/r2_{n:02d}.png', neg=NEG_ENV + NEG_DECOR, ar='16:9')
add('R-border-1-2', '3 방 장식', 2, 'DECOR', "decorations for the transition zone: pencil-gray scribble motifs on the left, crayon stripe motifs slowly painted over them toward the right",
    'border/border_1_2_decor.png', control='Art/control/border_1_2.png', neg=NEG_ENV + NEG_DECOR, ar='16:9', note='경계 장식')
# 구역 사이 지형 이음 (같은 지역 안 구역끼리 + 1↔2장 경계 D→E): 맞닿은 구획 쌍 중 구역이 다른 것
ZPAIRS = {}
for l in LINKS:
    z1, z2 = AREAS[l['first']][0], AREAS[l['second']][0]
    if z1 != z2 and (z1, z2) not in ZPAIRS and (z2, z1) not in ZPAIRS: ZPAIRS[(z1, z2)] = l['dir']
for (z1, z2), d in ZPAIRS.items():
    c1, k1 = ZONE_TERRAIN[z1][0], ZONE_TERRAIN[z1][1]; c2, k2 = ZONE_TERRAIN[z2][0], ZONE_TERRAIN[z2][1]
    mat = (", " + MAT[c1] + " gradually overpainted by " + MAT[c2]) if c1 != c2 else ''
    side = "left side = {}, right side = {}" if d == 'h' else "top side = {}, bottom side = {}"
    add(f'TL-{z1}{z2}-fill', '1 지형', c2, 'TERRAIN_LINK', "terrain fill transition: " + side.format(ZONE_TERRAIN[z1][2], ZONE_TERRAIN[z2][2]) + mat,
        f'ch{c2}/terrain/link/ch{c2}_{z1}{z2}_fill.png', ref_extra=f'TZ{c1}-{z1}-fill_a TZ{c2}-{z2}-fill_a', ar='1:1', neg=NEG_ENV + NEG_TERRAIN,
        note=f"{k1} → {k2} 지형 속 이음 · 경계: {'가로(왼→오)' if d == 'h' else '세로(위→아래)'}" + (' · ⭐ 장 경계' if c1 != c2 else ''))
    edges = (('edge_top', 3), ('edge_bottom', 4)) if d == 'h' else (('edge_side', 5),)
    for key, i in edges:
        add(f'TL-{z1}{z2}-{key}', '1 지형', c2, 'EDGE', f"{key.replace('_', ' ')} strip transition: the left end matches the {ZONE_EN[z1]} strip ({ZONE_TERRAIN[z1][i]}), "
            f"the right end matches the {ZONE_EN[z2]} strip ({ZONE_TERRAIN[z2][i]}), the details change gradually along the strip" + mat,
            f'ch{c2}/terrain/link/ch{c2}_{z1}{z2}_{key}.png', ref_extra=f'TZ{c1}-{z1}-{key} TZ{c2}-{z2}-{key}', ar='8:1', neg=NEG_ENV + NEG_TERRAIN,
            note=f'{k1} → {k2} 가장자리 이음 (양 끝이 각 구역 띠와 맞물림)' + (' · ⭐ 장 경계' if c1 != c2 else ''))

# 4. 소품 · 구조물
PROPS = [
    (1, 'save_statue', "a pencil-holder shaped stone statue holding three pencils, save point"),
    (2, 'save_statue', "a crayon-box shaped stone statue holding three crayons, save point"),
    (0, 'ribbon_end', "a standing bookmark ribbon stuck in the ground, muted red, notched top, gently curving"),
    (0, 'ribbon_secret', "a torn paper scrap with a slate-blue bookmark ribbon stuck in the ground"),
    (0, 'ribbon_knot', "a standing bookmark ribbon tied with a black knot in the middle, pale blue-gray"),
    (1, 'sketch_dog_unfinished', "a dog in a simple childlike style with clean lines, body and head finished, its four legs missing and shown only as faint dashed lines"),
    (1, 'sketch_dog_finished', "the same childlike dog now with four short legs drawn, happy, tail up"),
    (2, 'sketch_sun_unfinished', "a sun in a simple childlike style with clean lines, round face with dot eyes and a flat mouth, uncolored chalk-white, eight rays as faint dashed lines"),
    (2, 'sketch_sun_finished', "the same child-drawn sun colored with orange crayon, smiling, eight solid crayon rays"),
    (0, 'orb_heal', "small floating orb of muted rose paint in a tiny jar"),
    (0, 'orb_shield', "small folded paper shield charm"),
    (0, 'orb_paint', "small jar of muted blue paint droplets"),
    (2, 'healup_bottle', "a small bottle of rose healing paint with a cork"),
    (0, 'heart_piece', "half of a clean heart cut from paper, muted rose"),
    (0, 'graphite_shard', "a small shard of graphite, diamond shaped, dark gray"),
    (0, 'ability_orb', "a softly floating paper orb with a pencil swirl inside, ability pickup"),
    (1, 'lever', "a wooden lever in pencil-gray, standing on a base"),
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
    (1, 'foreman_eraser', "big rectangular eraser foreman with a worn corner and a helmet lamp in pencil-gray, charges forward", "one worn rounded corner"),
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
      (0, 'hit_spark', "short crossed clean pencil-line shapes marking an impact"), (0, 'smear_swing', "a curved streak of clean pencil-line shapes for a fast eraser swing"),
      (0, 'parry_flash', "a small golden-yellow star burst made of clean crayon-colored shapes"), (0, 'eraser_crumbs', "pinkish gray eraser crumbs scattering"),
      (0, 'erase_wipe', "a clean band wiping across like an eraser"), (0, 'redraw_line', "a single pencil line being drawn"),
      (0, 'dust_land', "small puff of paper dust on landing"), (1, 'graphite_dust', "graphite powder puff"), (1, 'ink_splash', "black-violet ink splash and drops"),
      (2, 'crayon_crumbs', "crumbs of orange and beige crayon"), (2, 'wax_drip', "drops of melted wax falling"), (2, 'crayon_flame', "small orange crayon-drawn flame"),
      (2, 'scribble_smoke', "a wisp of scribbled gray smoke")]
for ch, key, subj in FX:
    add(f'FX-{key}', '9 이펙트', ch or 1, 'FX', subj, f'{"common" if ch==0 else f"ch{ch}"}/fx/{key}.png', neg=NEG_ENV, ar='1:1')

# 8. UI
UI = [('heart_full', "a clean heart icon filled with muted rose"), ('heart_half', "a clean heart icon half filled with muted rose"),
      ('heart_empty', "an empty clean heart outline"), ('item_note', "a small paper note taped on the corner, for an item slot"),
      ('ability_wj', "small doodle icon of a hand gripping a wall"), ('ability_cut', "small doodle icon of a sharpened pencil"),
      ('ability_dash', "small doodle icon of an eraser with speed lines"), ('ability_parry', "small doodle icon of a deflected wax ball"),
      ('ability_climb', "small doodle icon of a crayon climbing a wall"), ('map_page', "a sketchbook page used as the map screen background"),
      ('title_underline', "a single clean crayon underline stroke"), ('pencil_hint', "a tiny pencil icon pointing down"),
      ('up_hint', "a tiny clean caret arrow pointing up")]
for key, subj in UI:
    add(f'UI-{key}', '10 UI', 1, 'UI', subj, f'common/ui/{key}.png', neg=NEG_ENV, ar='1:1', note='글자는 엔진에서 폰트로')

# 9. 키 아트
add('K-cover', '11 키 아트', 1, 'KEY', "the sketchbook cover with an empty title area, worn edges, a small chalk-white figure sitting on it", 'common/key/cover.png', ar='16:9', note='제목 Unfin은 손글씨로 합성')
add('K-ch1', '11 키 아트', 1, 'KEY', "chapter one title page: pencil forest drawn in graphite, a tiny chalk-white figure at the edge", 'ch1/key/ch1_title.png', ar='16:9')
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
    json.dump(dict(version='MSB v4 · UNIFY v4 · MAT v2 (부드러운 그래픽 디자인 · 선명한 선) · 지형 구조 v4 (구역별 지형 + 구역 이음, 3단 가장자리) · 배경 v6.2 실내형 · 4방향 이어짐 (구획 뒷벽 + 세로 뒷벽 + 공용 조각 + 방 대표 조각 · 붙는 곳) · 미드키 게임 그림체', blocks=dict(MSB=MSB, UNIFY=UNIFY, MAT=MAT, TYPE=TYPE, CHAR_LOCK=CHAR_LOCK,
              NEG_COMMON=NEG_COMMON, NEG_ENV=NEG_ENV, NEG_BG=NEG_BG, NEG_BG_GLIMPSE=NEG_BG_GLIMPSE, NEG_TERRAIN=NEG_TERRAIN, NEG_DECOR=NEG_DECOR, NEG_CHAR=NEG_CHAR, NEG_POSE=NEG_POSE), assets=A),
              open(os.path.join(ROOT, 'asset_catalog.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    with open(os.path.join(ROOT, 'prompts_full.csv'), 'w', encoding='utf-8-sig', newline='') as f:
        w = csv.writer(f); w.writerow(['ID', '분류', '장', '종류', '파일', '최종 비율', 'AI에 요청할 비율', 'AI가 보통 뽑는 크기', '최종 크기', '최종으로 만드는 법', '레퍼런스', '컨트롤 이미지', '메모', '프롬프트', '네거티브'])
        for r in rows: w.writerow([r['id'], r['cat'], r['ch'], r['type'], 'Art/out/' + r['file'], r['ar'], r['gen_ar'], r['gen_size'], r['final'], r['how'], r['ref'], r['control'], r['note'], r['prompt'], r['negative']])
    md = ['# Unfin 완성 프롬프트 (자동 생성 · 직접 고치지 말 것)', '', '> `python Tools/build_prompts.py`로 다시 만들어. 규칙은 Docs/19 문서.', '']
    cat = None
    for r in rows:
        if r['cat'] != cat:
            cat = r['cat']; md += ['', f'## {cat}', '']
        md += [f"### {r['id']} · `{r['file']}`", f"- 레퍼런스: {r['ref']}" + (f" · 컨트롤: `{r['control']}`" if r['control'] else '') + (f" · {r['note']}" if r['note'] else ''),
               f"- 📐 AI에 요청: **{r['gen_ar']}** (보통 {r['gen_size']}) → {r['how']} · 최종 **{r['final']}**",
               '```', r['prompt'], '```', '네거티브:', '```', r['negative'], '```', '']
    open(os.path.join(ROOT, 'ALL_PROMPTS.md'), 'w', encoding='utf-8').write('\n'.join(md))
    from collections import Counter
    c = Counter(r['cat'] for r in rows)
    print('에셋', len(rows), dict(c))
    import bg_manifest; bg_manifest.build()          # 방마다 깔 배경 → Art/bg_unity_manifest.json (유니티가 읽음)

if __name__ == '__main__': main()
