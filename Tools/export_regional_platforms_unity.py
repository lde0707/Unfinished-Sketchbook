"""Copy selected generated platform sprites unchanged into Unity's source tree."""
from pathlib import Path
import shutil
src=Path('Art/samples/regional-platforms-v1')
out=Path('Book/Assets/Unfin/Art/RegionalPlatforms')
for region in sorted(src.glob('0*')):
    if not region.is_dir():continue
    (out/region.name).mkdir(parents=True,exist_ok=True)
    for image in region.glob('*-128.png'):
        shutil.copyfile(image,out/region.name/image.name)
shutil.copyfile(src/'design.json',out/'design.json')
print('15 platform sprites copied unchanged')
