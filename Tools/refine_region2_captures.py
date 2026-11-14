from pathlib import Path
import os
p=Path(__file__).resolve().parents[1]/'Book/Assets/Unfin/Editor/Region2Integration.cs'
s=p.read_text(encoding='utf-8')
s=s.replace('w.Progression.Toast.Contains("ユチ")||','')
old='camera.transform.position=new Vector3(spawn.x+.5f,-spawn.y+3.2f,-10);Capture(camera,"Validation/region2-rooms/"+r.n.ToString("D2")+"-play.png");'
new='float groundPx=360+(camera.transform.position.y+spawn.y)*360/camera.orthographicSize;Capture(camera,"Validation/region2-rooms/"+r.n.ToString("D2")+"-play.png");if(new[]{3,5,18,20}.Contains(r.n)){var actors=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(a=>a.GetComponentInParent<MapMarker>()!=null&&a.GetComponentInParent<MapMarker>().enemy).ToArray();var enabled=actors.Select(a=>a.enabled).ToArray();foreach(var a in actors)a.enabled=false;Capture(camera,"Validation/region2-rooms/"+r.z+"-clean.png");for(int i=0;i<actors.Length;i++)actors[i].enabled=enabled[i];}'
if old in s:s=s.replace(old,new)
old='\\"spawn\\":\\""+spawn.x'
new='\\"groundPx\\":"+groundPx.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"orthographicSize\\":"+w.Data.orthographicSize.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\\"spawn\\":\\""+spawn.x'
if old in s:s=s.replace(old,new)
old='w.Progression.DefeatedEncounters.Add(91);w.Respawn(465,124);'
new='w.worldCamera.orthographicSize=w.Data.orthographicSize;w.Respawn(465,124);player.Tick(.001f);foreach(var d in UnityEngine.Object.FindObjectsByType<PaperDeviceVisual>(FindObjectsSortMode.None))d.Tick(0);Capture(w.worldCamera,"Validation/region2-rooms/resident-before.png");w.Progression.DefeatedEncounters.Add(91);w.Respawn(465,124);'
if old in s:s=s.replace(old,new)
s=s.replace('w.worldCamera.transform.position=new Vector3(466,-120.8f,-10);','')
t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
print('REGION2_CAPTURE_FRAMING_READY')
