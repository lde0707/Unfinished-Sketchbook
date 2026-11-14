from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]/'Book/Assets/Unfin/Runtime'
changes={
'Region2Presentation.cs':[('if(cache.TryGetValue(path,out var s))return s;','if(cache.TryGetValue(path,out var s)&&s!=null)return s;')],
'PaperRegion2Encounter.cs':[('if(drawings.TryGetValue(a.id,out var s))return s;','if(drawings.TryGetValue(a.id,out var s)&&s.Length>0&&s[0]!=null)return s;')],
'PaperBossRegion1.cs':[('if(drawings.TryGetValue(name,out var cached))return cached;','if(drawings.TryGetValue(name,out var cached)&&cached.Length>0&&cached[0]!=null)return cached;')],
'PaperEnemyAnimation.cs':[('if(!sheets.TryGetValue(data.id,out frames))','if(!sheets.TryGetValue(data.id,out frames)||frames.Length==0||frames[0]==null)')],
'PaperPickupVisual.cs':[('if(sprites.TryGetValue(name,out var cached))return cached;','if(sprites.TryGetValue(name,out var cached)&&cached!=null)return cached;')],
'PaperDeviceVisual.cs':[('if(cache.TryGetValue(name,out var s))return s;','if(cache.TryGetValue(name,out var s)&&s!=null)return s;')],
'PaperFriendAnimation.cs':[('if(cache.TryGetValue(name,out var a))return a;','if(cache.TryGetValue(name,out var a)&&a.Length>0&&a[0]!=null)return a;')]
}
for name,replacements in changes.items():
    p=root/name;s=p.read_text(encoding='utf-8-sig')
    for old,new in replacements:
        if new in s:continue
        if old not in s:raise RuntimeError(name+' anchor missing')
        s=s.replace(old,new,1)
    t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
print('SPRITE_CACHE_LIFETIME_FIXED')
