using UnityEngine;
namespace Unfin
{
    public sealed class MiniMapTracking
    {
        Vector2 center,velocity;
        bool initialized;
        public Rect View {get;private set;}
        public Rect Follow(Vector2 player,Vector2 mapSize,float dt)
        {
            var size=new Vector2(Mathf.Min(30f*314/165,mapSize.x),Mathf.Min(30,mapSize.y));
            var target=new Vector2(Mathf.Clamp(player.x,size.x*.5f,mapSize.x-size.x*.5f),Mathf.Clamp(player.y,size.y*.5f,mapSize.y-size.y*.5f));
            if(!initialized||Vector2.Distance(center,target)>size.x*.5f){center=target;velocity=Vector2.zero;initialized=true;}
            else center=Vector2.SmoothDamp(center,target,ref velocity,.18f,Mathf.Infinity,dt);
            View=new Rect(center-size*.5f,size);return View;
        }
    }
    // World-space viewport: zooming never changes saved note coordinates or discovery.
    public sealed class JournalMapView
    {
        public const float MaxZoom=6;
        public Rect Bounds {get;private set;}
        public Rect View {get;private set;}
        public float Zoom {get;private set;}=1;
        public void Reset(Rect bounds){Bounds=bounds;Zoom=1;View=bounds;}
        void Place(Vector2 center)
        {
            Vector2 size=Bounds.size/Zoom;
            View=new Rect(Mathf.Clamp(center.x-size.x*.5f,Bounds.xMin,Bounds.xMax-size.x),Mathf.Clamp(center.y-size.y*.5f,Bounds.yMin,Bounds.yMax-size.y),size.x,size.y);
        }
        public Vector2 WorldAt(Vector2 normalized)=>View.position+Vector2.Scale(View.size,normalized);
        public void ZoomAt(float multiplier,Vector2 normalized)
        {
            normalized=new Vector2(Mathf.Clamp01(normalized.x),Mathf.Clamp01(normalized.y));
            Vector2 anchor=WorldAt(normalized);Zoom=Mathf.Clamp(Zoom*multiplier,1,MaxZoom);
            Place(anchor+Vector2.Scale(Bounds.size/Zoom,new Vector2(.5f,.5f)-normalized));
        }
        public void PanFrom(Rect start,Vector2 delta,Vector2 screenSize)
        {Place(start.center-new Vector2(delta.x*start.width/screenSize.x,delta.y*start.height/screenSize.y));}
        public void Focus(Vector2 point){Zoom=Mathf.Max(3,Zoom);Place(point);}
    }
    public sealed partial class WorldProgression
    {
        readonly JournalMapView journalView=new JournalMapView();
        int journalRegion=-1;
        bool showMapLegend;
        bool mapPointerHeld,mapDragging;Vector2 mapPointerDown;Rect mapPointerView;
        public Rect JournalView=>journalView.View;
        public float JournalZoom=>journalView.Zoom;
        public void FocusNote(int index)
        {
            if(index<0||index>=notes.Count)return;
            SelectNote(notes[index].x,notes[index].y,index);
            mapRegion=notes[index].x<world.Data.regionX?1:2;SyncJournalRegion();
            journalView.Focus(new Vector2(notes[index].x,notes[index].y));
        }
        Rect JournalRegionBounds()=>new Rect(mapRegion==2?world.Data.regionX:0,0,mapRegion==0?world.Data.width:mapRegion==2?world.Data.width-world.Data.regionX:world.Data.regionX,world.Data.height);
        void SyncJournalRegion(){if(journalRegion==mapRegion)return;journalRegion=mapRegion;journalView.Reset(JournalRegionBounds());mapPointerHeld=false;}
        void DrawMapNavigation()
        {
            SyncJournalRegion();
            if(PaintedButton(new Rect(225,322,62,42),"−"))journalView.ZoomAt(1/1.5f,new Vector2(.5f,.5f));
            if(PaintedButton(new Rect(296,322,62,42),"+"))journalView.ZoomAt(1.5f,new Vector2(.5f,.5f));
            if(PaintedButton(new Rect(500,322,150,42),"전체 보기"))journalView.Reset(JournalRegionBounds());
            if(PaintedButton(new Rect(670,322,150,42),"내 위치")){
                var point=new Vector2((world.Motor.x+6)/16,(world.Motor.y+11)/16);
                if(mapRegion!=0){mapRegion=point.x<world.Data.regionX?1:2;SyncJournalRegion();}
                journalView.Focus(point);
            }
            if(PaintedButton(new Rect(845,322,190,42),showMapLegend?"범례 접기":"범례"))showMapLegend=!showMapLegend;
        }
        void HandleMapPointer(Rect rect)
        {
            var e=Event.current;var point=e.mousePosition;
            if(e.type==EventType.ScrollWheel&&rect.Contains(point)){
                mapPointerHeld=false;journalView.ZoomAt(Mathf.Exp(-e.delta.y*.12f),new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height));e.Use();
            }
            if(e.type==EventType.MouseDown&&e.button==0&&rect.Contains(point)){
                FinishNoteEditing();mapPointerHeld=true;mapDragging=false;mapPointerDown=point;mapPointerView=journalView.View;e.Use();
            }
            if(!mapPointerHeld)return;
            if(e.type==EventType.MouseDrag&&e.button==0){
                if(Vector2.Distance(mapPointerDown,point)>6)mapDragging=true;
                if(mapDragging)journalView.PanFrom(mapPointerView,point-mapPointerDown,rect.size);e.Use();
            }
            if(e.type==EventType.MouseUp&&e.button==0){
                mapPointerHeld=false;
                if(!mapDragging&&Vector2.Distance(mapPointerDown,point)<=6&&rect.Contains(point)){
                    var view=journalView.View;int existing=HitNote(rect,view,point);
                    var target=journalView.WorldAt(new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height));SelectNote(target.x,target.y,existing);
                }
                e.Use();
            }
        }
    }
    public sealed partial class WorldPrototype
    {
        // Read-only QA snapshot; no game state or save mutation.
        public void ReportJournalMap(){var v=Progression.JournalView;var mini=Progression.MiniView;Debug.Log("JOURNAL_MAP "+JsonUtility.ToJson(new JournalMapSnapshot{zoom=Progression.JournalZoom,x=v.x,y=v.y,width=v.width,height=v.height,notes=Progression.NoteCount,selected=Progression.SelectedNoteIndex,text=Progression.SelectedNoteText,miniX=mini.center.x,miniY=mini.center.y,playerX=(Motor.x+6)/16,playerY=(Motor.y+11)/16,paused=Time.timeScale==0}));}
        [System.Serializable] sealed class JournalMapSnapshot {public float zoom,x,y,width,height,miniX,miniY,playerX,playerY;public int notes,selected;public string text;public bool paused;}
    }
}
