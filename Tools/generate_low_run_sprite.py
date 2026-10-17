"""Generate a NEW complete low-running sprite with SDXL + IP-Adapter.

No inpainting mask, no reference-pixel compositing, no skeletal transforms.
Pillow formats the whole reference. Rembg segments the generated background.
"""
import argparse
import json
import os
import shutil
import urllib.request
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'Art/inbox/open-sprite-runtime/ComfyUI'
OUT = ROOT / 'Art/animations/open-source-low-run'
URL = 'http://127.0.0.1:8189'


def api(path, data=None):
    req = urllib.request.Request(URL + path, data=None if data is None else json.dumps(data).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response:
        return json.load(response)


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    character = Image.open(ROOT / 'Art/animations/player-c-v6/frames/00.png').convert('RGBA')
    # Whole-reference orientation only, not articulated parts or an output edit.
    character = character.rotate(-18, resample=Image.Resampling.BICUBIC, expand=True)
    ref = Image.new('RGBA', character.size, (37, 39, 43, 255))
    ref.alpha_composite(character)
    ref = ref.convert('RGB').resize((768, 768), Image.Resampling.LANCZOS)
    ref.save(OUT / 'reference-running.png')
    ref.save(RUNTIME / 'input/paper-traveler-identity.png')
    prompt = ('Strict right-facing side profile of one tiny ivory paper spirit, (only ONE black oval eye visible:1.4), '
        'huge spherical head, very short stubby limbs. (Low forward sprint:1.6), (torso nearly horizontal:1.5), '
        'head far ahead of hips, hips low, knees bent, back foot pushing ground, front knee tucked toward chest. '
        'Short rounded mitten hands swinging in opposition. Very long soft layered paper cape streams left '
        'in two broad flowing tails. Plain shorts and cape slightly darker warm ivory than body. '
        'Soft fine paper grain and gentle illustrated shading, rounded contours, full-body 2D game sprite, '
        'plain charcoal background, entire character visible with margins.')
    negative = ('two eyes, front view, three-quarter face, looking at viewer, standing, upright torso, walking, jumping upwards, floating, sitting, kneeling, tall human proportions, '
        'long legs, long arms, muscular, realistic human, photorealistic, hard plastic, metal armor, helmet, '
        'horns, sword, weapon, nose, mouth, teeth, hair, detailed fingers, extra limbs, missing limbs, '
        'multiple characters, character sheet, text, watermark, scenery, pixel art, jagged edges, shorts seams')
    graph = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': 'sd_xl_base_1.0.safetensors'}},
        '2': {'class_type': 'LoadImage', 'inputs': {'image': 'paper-traveler-identity.png'}},
        '3': {'class_type': 'CLIPVisionLoader', 'inputs': {'clip_name': 'CLIP-ViT-H-14-laion2B-s32B-b79K.safetensors'}},
        '4': {'class_type': 'IPAdapterModelLoader', 'inputs': {'ipadapter_file': 'ip-adapter-plus_sdxl_vit-h.safetensors'}},
        '5': {'class_type': 'IPAdapterAdvanced', 'inputs': {'model': ['1', 0], 'ipadapter': ['4', 0],
            'clip_vision': ['3', 0], 'image': ['2', 0], 'weight': 0.8, 'weight_type': 'linear',
            'combine_embeds': 'concat', 'start_at': 0.0, 'end_at': 0.8, 'embeds_scaling': 'V only'}},
        '6': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': prompt}},
        '7': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['1', 1], 'text': negative}},
        '8': {'class_type': 'EmptyLatentImage', 'inputs': {'width': 1216, 'height': 832, 'batch_size': 1}},
        '9': {'class_type': 'KSampler', 'inputs': {'model': ['5', 0], 'positive': ['6', 0], 'negative': ['7', 0],
            'latent_image': ['8', 0], 'seed': 171020263, 'steps': 28, 'cfg': 7.5,
            'sampler_name': 'euler', 'scheduler': 'normal', 'denoise': 1.0}},
        '10': {'class_type': 'VAEDecode', 'inputs': {'samples': ['9', 0], 'vae': ['1', 2]}},
        '11': {'class_type': 'SaveImage', 'inputs': {'images': ['10', 0], 'filename_prefix': 'paper-traveler-low-run'}}
    }
    (OUT / 'workflow-api.json').write_text(json.dumps(graph, indent=2), encoding='utf-8')
    print('Prepared NEW full-sprite generation: reference conditioning + empty latent, no edit mask.')


def submit():
    graph = json.loads((OUT / 'workflow-api.json').read_text(encoding='utf-8'))
    result = api('/prompt', {'prompt': graph, 'client_id': 'unfinished-sketchbook-low-run'})
    (OUT / 'job.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))


def collect():
    job = json.loads((OUT / 'job.json').read_text())['prompt_id']
    history = api('/history/' + job)
    if job not in history:
        print('Generation still pending or running.')
        return
    record = history[job]
    (OUT / 'history.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    if not record.get('outputs', {}).get('11'):
        print(json.dumps(record.get('status', {})))
        return
    result = record['outputs']['11']['images'][0]
    path = RUNTIME / 'output' / result.get('subfolder', '') / result['filename']
    shutil.copy2(path, OUT / 'low-run-generated.png')
    print('Saved complete newly generated sprite candidate:', OUT / 'low-run-generated.png')


def cutout():
    os.environ['U2NET_HOME'] = str(RUNTIME.parent / 'saliency-models')
    from rembg import new_session, remove
    session = new_session('u2net', providers=['CPUExecutionProvider'])
    source = Image.open(OUT / 'low-run-generated.png').convert('RGB')
    sprite = remove(source, session=session).convert('RGBA')
    sprite.save(OUT / 'low-run-sprite.png')
    ImageOps.contain(sprite, (512, 512), Image.Resampling.LANCZOS).save(OUT / 'low-run-sprite-512.png')
    preview = Image.new('RGBA', sprite.size, (32, 34, 40, 255))
    preview.alpha_composite(sprite)
    preview.convert('RGB').save(OUT / 'low-run-preview.png')
    report = {'size': list(sprite.size), 'mode': sprite.mode,
              'alphaExtrema': list(sprite.getchannel('A').getextrema()),
              'alphaBounds': sprite.getchannel('A').getbbox(),
              'entireCharacterNewlyGenerated': True, 'inpainting': False,
              'referencePixelsComposited': False,
              'backgroundRemoval': 'rembg 2.0.85 + U2NET (CPU)',
              'visualReview': 'Pending'}
    (OUT / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'submit', 'collect', 'cutout'])
    globals()[parser.parse_args().action]()
