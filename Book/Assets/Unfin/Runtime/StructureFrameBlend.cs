using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Interpolate registered whole drawings at render cadence. No extra textures,
    // duplicated renderers, or changes to the interaction/collision clock.
    public sealed class StructureFrameBlend : MonoBehaviour
    {
        static Material material;
        static readonly Dictionary<string, Sprite[]> clips = new Dictionary<string, Sprite[]>();
        static readonly int NextTex = Shader.PropertyToID("_BlendNextTex"), NextRect = Shader.PropertyToID("_BlendNextRect"), BaseRect = Shader.PropertyToID("_BlendBaseRect"), Amount = Shader.PropertyToID("_FrameBlend");
        SpriteRenderer body;
        MaterialPropertyBlock properties;
        string clipName; int clipCount, clipStart; Sprite[] sequence;
        public Sprite Next { get; private set; }
        public float Blend { get; private set; }
        public static StructureFrameBlend For(SpriteRenderer renderer)
        {
            var result = renderer.GetComponent<StructureFrameBlend>();
            if (result == null) result = renderer.gameObject.AddComponent<StructureFrameBlend>();
            if (result.body == null) {
                result.body = renderer; result.properties = new MaterialPropertyBlock();
                if (material == null) material = new Material(Resources.Load<Shader>("StructureFrameBlend"));
                renderer.sharedMaterial = material;
            }
            return result;
        }
        static Vector4 UV(Sprite s) { var r = s.rect; return new Vector4(r.x / s.texture.width, r.y / s.texture.height, r.width / s.texture.width, r.height / s.texture.height); }
        public void CopyTo(MaterialPropertyBlock block)
        {
            block.SetFloat(Amount, Blend);
            block.SetTexture(NextTex, Next.texture);
            block.SetVector(NextRect, UV(Next));
            block.SetVector(BaseRect, UV(body.sprite));
        }
        public void Set(Sprite from, Sprite to, float amount)
        {
            if (from == null) return;
            body.sprite = from; Next = to != null ? to : from; Blend = Mathf.Clamp01(amount);
            body.GetPropertyBlock(properties); CopyTo(properties); body.SetPropertyBlock(properties);
        }
        public void Sample(Sprite[] frames, float position, bool loop)
        {
            position = loop ? Mathf.Repeat(position, frames.Length) : Mathf.Clamp(position, 0, frames.Length - 1);
            int i = Mathf.FloorToInt(position), j = loop ? (i + 1) % frames.Length : Mathf.Min(i + 1, frames.Length - 1);
            // Linear sampling keeps the same motion speed across the loop seam.
            Set(frames[i], frames[j], position - i);
        }
        public static void Device(SpriteRenderer body, string name, float position, int count = 4, bool loop = true, int start = 0)
        {
            var animator = For(body);
            if (animator.clipName == name && animator.clipCount == count && animator.clipStart == start && animator.sequence != null) {
                animator.Sample(animator.sequence, position, loop); return;
            }
            string key = name + ":" + start + ":" + count;
            if (!clips.TryGetValue(key, out var frames) || frames[0] == null) {
                frames = new Sprite[count];
                for (int i = 0; i < count; i++) frames[i] = Region2Presentation.DeviceFrame(name, start + i);
                if (frames[0] == null) return;
                clips[key] = frames;
            }
            animator.clipName = name; animator.clipCount = count; animator.clipStart = start; animator.sequence = frames;
            animator.Sample(frames, position, loop);
        }
    }
}
