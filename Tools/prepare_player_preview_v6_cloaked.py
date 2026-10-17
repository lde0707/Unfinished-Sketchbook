from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
base=ROOT/'Art/animations/player-c-v6'
out=ROOT/'Art/animations/player-c-v6-cloaked'
s=(base/'preview.html').read_text(encoding='utf-8')
s=s.replace('종이 여행자 · 다리 교대 8프레임','종이 여행자 · 망토 속 양팔')
s=s.replace('PLAYER C / V6','PLAYER C / V6 · CLOAKED')
s=s.replace('다리가 교대하는 8장 달리기','양팔을 망토 안으로')
s=s.replace('1~3번은 가까운 다리가 앞으로, 5~7번은 먼 다리가 앞으로 갑니다.<br>1·5번은 든 다리를 깊게 접고, 반대쪽 발바닥으로 바닥을 짚습니다.','v6의 양팔과 손을 망토 안에 감춘 수정안입니다.<br>기존 8장 순서와 0.8초 달리기 주기를 유지합니다.')
s=s.replace('Warrior 원본','수정 전 v6').replace('수정한 플레이어','양팔을 감춘 v6')
s=s.replace('전신 그림 8장을 새로 제작했습니다. 머리 크기를 통일하고, 이륙·착지에 따라 몸 전체 높이를 바꿨습니다. 부품 회전·자동 보간은 사용하지 않았습니다.','v6 전신 원화를 참조해 양팔을 숨긴 8장을 수정했습니다. 원본과 픽셀 단위로 같지는 않습니다. 부품 회전·자동 보간은 사용하지 않았습니다.')
s=s.replace(' · <a href="https://clembod.itch.io/warrior-free-animation-set">Warrior 원작자</a>','')
s=s.replace('c===oc?211:218','211')
s=s.replace('c.imageSmoothingEnabled=!isRaw;if(isRaw)c.drawImage(im,-14,18,640,440);else c.drawImage(im,0,0);','c.imageSmoothingEnabled=true;c.drawImage(im,0,0);')
s=s.replace('img(`reference/${','img(`../player-c-v6/frames/${')
(out/'preview.html').write_text(s,encoding='utf-8')
