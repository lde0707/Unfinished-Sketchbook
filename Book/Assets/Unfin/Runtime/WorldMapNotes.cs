using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Unfin
{
    [Serializable] public sealed class MapNote {public float x,y;public string text;}
    [Serializable] sealed class MapNoteFile {public List<MapNote> notes=new List<MapNote>();}
    public sealed partial class WorldProgression
    {
        readonly List<MapNote> notes=new List<MapNote>();
        int selectedNote=-1;Vector2? notePosition;string noteDraft="";Vector2 noteScroll,noteReadScroll;
        public bool NotesEditing {get;private set;}
        public int NoteCount=>notes.Count;
        public int SelectedNoteIndex=>selectedNote;
        public string SelectedNoteText=>noteDraft;
        public string NoteStorageKey=>"stilldrawn.notes."+world.Data.gridSha256;
        GUIStyle noteStyle;bool clearNoteFocus;
        [DllImport("__Internal")] static extern void Stilldrawn_EditNote(string value);
        [DllImport("__Internal")] static extern void Stilldrawn_CloseNote();
        public void LoadNotes(MapNote[] fallback=null)
        {
            notes.Clear();selectedNote=-1;notePosition=null;noteDraft="";
            try{
                var data=PlayerPrefs.HasKey(NoteStorageKey)?JsonUtility.FromJson<MapNoteFile>(PlayerPrefs.GetString(NoteStorageKey))?.notes:fallback==null?null:new List<MapNote>(fallback);
                if(data!=null)foreach(var n in data){if(notes.Count>=48)break;if(n==null||!float.IsFinite(n.x)||!float.IsFinite(n.y)||n.x<0||n.y<0||n.x>=world.Data.width||n.y>=world.Data.height||string.IsNullOrWhiteSpace(n.text))continue;notes.Add(new MapNote{x=n.x,y=n.y,text=n.text.Substring(0,Mathf.Min(160,n.text.Length))});}
            }catch{/* Ignore malformed notes without invalidating the main save. */}
        }
        void PersistNotes(){PlayerPrefs.SetString(NoteStorageKey,JsonUtility.ToJson(new MapNoteFile{notes=notes}));PlayerPrefs.Save();}
        public void SelectNote(float x,float y,int index=-1)
        {
            SaveNote();FinishNoteEditing();
            selectedNote=index>=0&&index<notes.Count?index:-1;
            notePosition=selectedNote>=0?new Vector2(notes[index].x,notes[index].y):new Vector2(Mathf.Clamp(x,0,world.Data.width-.01f),Mathf.Clamp(y,0,world.Data.height-.01f));
            noteDraft=selectedNote>=0?notes[index].text:"";
            noteReadScroll=Vector2.zero;
        }
        public void SetNoteDraft(string value){noteDraft=(value??"").Substring(0,Mathf.Min(160,(value??"").Length));}
        public bool SaveNote()
        {
            if(!notePosition.HasValue||string.IsNullOrWhiteSpace(noteDraft))return false;
            if(selectedNote<0){if(notes.Count>=48)return false;selectedNote=notes.Count;notes.Add(new MapNote{x=notePosition.Value.x,y=notePosition.Value.y});}
            notes[selectedNote].text=noteDraft.Trim();PersistNotes();return true;
        }
        public void DeleteNote(){FinishNoteEditing();if(selectedNote>=0&&selectedNote<notes.Count)notes.RemoveAt(selectedNote);selectedNote=-1;notePosition=null;noteDraft="";PersistNotes();}
        public void CloseNotes(){mapPointerHeld=false;SaveNote();FinishNoteEditing();}
        public void BeginNoteEditing()
        {
            if(!notePosition.HasValue)return;NotesEditing=true;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=false;Stilldrawn_EditNote(noteDraft);
#endif
        }
        public void FinishNoteEditing()
        {
            NotesEditing=false;clearNoteFocus=true;
#if UNITY_WEBGL && !UNITY_EDITOR
            Stilldrawn_CloseNote();WebGLInput.captureAllKeyboardInput=true;
#endif
        }
        int HitNote(Rect rect,Rect view,Vector2 point)
        {
            int hit=-1;float distance=18;
            for(int i=0;i<notes.Count;i++){
                var n=notes[i];if(!view.Contains(new Vector2(n.x,n.y)))continue;
                float d=Vector2.Distance(MapPoint(rect,view,n.x,n.y),point);
                if(d<distance){hit=i;distance=d;}
            }
            return hit;
        }
        void DrawNotes(Rect rect,Rect view,bool detailed)
        {
            for(int i=0;i<notes.Count;i++){
                var n=notes[i];if(!view.Contains(new Vector2(n.x,n.y)))continue;
                var p=MapPoint(rect,view,n.x,n.y);
                if(i==selectedNote&&detailed){float pulse=22+2*Mathf.Sin(Time.unscaledTime*3);HudDiamond(p.x,p.y,pulse,hudGold);HudDiamond(p.x,p.y,pulse-4,new Color(.15f,.13f,.10f));}
                PaintedHudArt.Draw("key",new Rect(p.x-11,p.y-12,22,24),i==selectedNote?Color.white:new Color(.82f,.75f,.56f));
                HudText(new Rect(p.x-7,p.y-9,20,20),(i+1).ToString(),new Color(.2f,.17f,.12f),14);
            }
            if(notePosition.HasValue&&selectedNote<0)Marker(rect,view,notePosition.Value.x,notePosition.Value.y,hudGold,10);
        }
        void DrawNotePage()
        {
            if(clearNoteFocus){GUI.FocusControl(null);clearNoteFocus=false;}
            HudText(new Rect(1260,180,400,45),"메모",hudInk,32,true);
            if(noteStyle==null)noteStyle=new GUIStyle(hudBody){wordWrap=true,fontSize=24,padding=new RectOffset(10,10,10,10),normal={textColor=hudInk,background=null},focused={textColor=hudInk,background=null}};
            var edit=new Rect(1270,345,370,220);
            if(notePosition.HasValue){
                HudText(new Rect(1260,300,400,35),(selectedNote>=0?"# "+(selectedNote+1):"새 메모")+(NotesEditing&&noteDraft.Length>=140?"  ·  "+noteDraft.Length+" / 160":""),hudGold,18);
#if UNITY_WEBGL && !UNITY_EDITOR
                if(!NotesEditing){
                    string content=string.IsNullOrEmpty(noteDraft)?"메모 입력…":noteDraft;
                    float height=Mathf.Max(edit.height,noteStyle.CalcHeight(new GUIContent(content),edit.width-22));
                    noteReadScroll=GUI.BeginScrollView(edit,noteReadScroll,new Rect(0,0,edit.width-22,height));
                    GUI.Label(new Rect(0,0,edit.width-22,height),content,noteStyle);GUI.EndScrollView();
                }
                var e=Event.current;if(e.type==EventType.MouseDown&&edit.Contains(e.mousePosition)){BeginNoteEditing();e.Use();}
#else
                GUI.SetNextControlName("map-note");noteDraft=GUI.TextArea(edit,noteDraft,160,noteStyle);
                if(GUI.GetNameOfFocusedControl()=="map-note")NotesEditing=true;
#endif
                if(PaintedButton(new Rect(1260,585,174,50),"저장")){SaveNote();FinishNoteEditing();}
                if(PaintedButton(new Rect(1455,585,174,50),"삭제"))DeleteNote();
            }else{GUI.Label(edit,"지도에서 위치를 선택하세요.",noteStyle);}
            if(notes.Count>0)HudText(new Rect(1260,675,380,35),"메모 목록",hudGold,20,true);
            noteScroll=GUI.BeginScrollView(new Rect(1250,720,410,135),noteScroll,new Rect(0,0,370,Mathf.Max(130,notes.Count*43)),false,false);
            for(int i=0;i<notes.Count;i++)if(HudButton(new Rect(0,i*43,370,39),(i+1)+". "+notes[i].text.Replace('\n',' ').Substring(0,Mathf.Min(14,notes[i].text.Length)),i==selectedNote)){FocusNote(i);}
            GUI.EndScrollView();
        }
    }
    public sealed partial class WorldPrototype
    {
        public void SetMapNoteDraft(string value){if(showMap)Progression.SetNoteDraft(value);}
        public void SaveMapNote(string unused){if(showMap){Progression.SaveNote();Progression.FinishNoteEditing();}}
        public void FinishMapNoteEditing(string unused){Progression.FinishNoteEditing();}
    }
}
