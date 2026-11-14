using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unfin
{
    [Serializable] public sealed class StoryFrame
    {
        public string file;
        public int[] rect;
        public float anchorX, anchorY, scale;
    }
    [Serializable] public sealed class StoryClip
    {
        public string name;
        public float duration;
        public StoryFrame[] frames;
    }
    [Serializable] public sealed class StoryArt
    {
        public StoryClip[] clips;
        public StoryFrame monsterIdle;
    }

    // Prologue apparitions only. Does not alter combat enemies or player animation.
    public sealed class ProximityStory : MonoBehaviour
    {
        public enum Phase { Present, Dissolving, Gone }
        public sealed class Actor
        {
            public SpriteRenderer visual;
            public Phase phase;
            public float age, radius;
            public int frame, starts;
            internal StoryClip clip;
            internal Sprite[] frames;
            internal Sprite idle;
        }
        public readonly List<Actor> Actors = new List<Actor>();
        public int GoneMask { get; private set; }
        WorldPrototype world;
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static ProximityStory Create(WorldPrototype w)
        {
            var existing = w.GetComponent<ProximityStory>();
            if (existing != null) return existing;
            var story = w.gameObject.AddComponent<ProximityStory>();
            story.Initialize(w);
            return story;
        }
        void Initialize(WorldPrototype w)
        {
            world = w;
            var source = Resources.Load<TextAsset>("ProximityStory/manifest");
            if (source == null) { Debug.LogError("Missing opening proximity story art"); return; }
            var art = JsonUtility.FromJson<StoryArt>(source.text);
            // Authored positions in the separate opening map.
            Add("Paper tree", art.clips[0], art.clips[0].frames[0], new Vector2(25, -16), 175f / 128);
            Add("Paper creature near start", art.clips[1], art.monsterIdle, new Vector2(12, -16), 195f / 128);
            Add("Paper creature by west exit", art.clips[1], art.monsterIdle, new Vector2(37, -16), 195f / 128);
        }
        Sprite LoadFrame(StoryFrame frame)
        {
            string key = frame.file + ":" + frame.rect[0] + ":" + frame.rect[1];
            if (sprites.TryGetValue(key, out var found)) return found;
            var texture = Resources.Load<Texture2D>("ProximityStory/" + System.IO.Path.GetFileNameWithoutExtension(frame.file));
            var r = frame.rect;
            var sprite = Sprite.Create(texture, new Rect(r[0], texture.height-r[1]-r[3], r[2], r[3]),
                new Vector2(frame.anchorX/r[2], 1-frame.anchorY/r[3]), 128/frame.scale, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            sprites.Add(key, sprite);
            return sprite;
        }
        void Add(string label, StoryClip clip, StoryFrame idle, Vector2 feet, float radius)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform, false);
            obj.transform.position = feet;
            var visual = obj.AddComponent<SpriteRenderer>();
            visual.sortingOrder = -2; // Behind the terrain edge; the player remains in front.
            var actor = new Actor { visual=visual, clip=clip, radius=radius, idle=LoadFrame(idle), frames=new Sprite[clip.frames.Length] };
            for (int i=0;i<actor.frames.Length;i++) actor.frames[i]=LoadFrame(clip.frames[i]);
            visual.sprite=actor.idle;
            Actors.Add(actor);
        }
        void Update()
        {
            if (world==null || world.MapOpen || world.HazardInputBlocked || Time.timeScale==0) return;
            Tick(Time.deltaTime);
        }
        public void Tick(float dt)
        {
            if (dt<=0 || world==null || world.Motor==null) return;
            var playerBounds=world.Motor.WorldBounds;
            for (int i=0;i<Actors.Count;i++)
            {
                var actor=Actors[i];
                if (actor.phase==Phase.Gone) continue;
                if (actor.phase==Phase.Present)
                {
                    Vector2 feet=actor.visual.transform.position;
                    Vector2 target=feet+Vector2.up*.6f;
                    if (Mathf.Abs(playerBounds.center.x-feet.x)>actor.radius ||
                        Mathf.Abs(playerBounds.min.y-feet.y)>1.1f ||
                        !InteractionProximity.Visible(world,playerBounds.center,target)) continue;
                    actor.phase=Phase.Dissolving;
                    actor.starts++;
                    GoneMask|=1<<i; // Saving mid-effect must not replay it on Continue.
                }
                actor.age=Mathf.Min(actor.clip.duration,actor.age+dt);
                actor.frame=Mathf.Min(31,Mathf.FloorToInt(actor.age/actor.clip.duration*32));
                actor.visual.sprite=actor.frames[actor.frame];
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,actor.age/actor.clip.duration));
                actor.visual.color=new Color(1,1,1,fade);
                if (actor.age>=actor.clip.duration) { actor.phase=Phase.Gone; actor.visual.enabled=false; }
            }
        }
        public void Restore(int mask)
        {
            GoneMask=mask & ((1<<Actors.Count)-1);
            for(int i=0;i<Actors.Count;i++)
            {
                var actor=Actors[i];
                actor.phase=(GoneMask&(1<<i))!=0?Phase.Gone:Phase.Present;
                actor.age=0;actor.frame=0;actor.starts=0;
                actor.visual.enabled=actor.phase==Phase.Present;
                actor.visual.sprite=actor.idle;actor.visual.color=Color.white;
            }
        }
        void OnDestroy()
        {
            foreach(var sprite in sprites.Values)
                if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);
        }
    }
}
