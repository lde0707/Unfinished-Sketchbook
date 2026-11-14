using UnityEngine;

namespace Unfin
{
    // Grade the world render before OnGUI/HUD, without changing any source art.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class WorldCameraGrade : MonoBehaviour
    {
        // World art direction, separate from the player's saved brightness adjustment.
        public const float BaseExposure = .8f;
        // Raise the previous 1.10x center exposure by 20%: 1.10 * 1.20 = 1.32.
        public const float PlayerExposureBoost = .32f;
        public const float PlayerLightRadius = 3f;
        [Range(.5f, 1f)] public float brightness = .9f;
        Material material;
        Camera view;
        WorldPrototype world;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                var shader = Resources.Load<Shader>("WorldCameraGrade");
                if (shader == null || !shader.isSupported) { Graphics.Blit(source, destination); return; }
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            material.SetFloat("_Brightness", brightness * BaseExposure);
            material.SetFloat("_Antialiasing", GraphicsPreferences.Antialiasing);
            if (view == null) view = GetComponent<Camera>();
            if (world == null) world = FindFirstObjectByType<WorldPrototype>();
            var light = Vector4.zero;
            if (world != null && world.player != null && world.worldCamera == view)
            {
                var center = view.WorldToViewportPoint(world.player.position);
                light = new Vector4(center.x, center.y, PlayerLightRadius / (2f * view.orthographicSize),
                    center.z > 0 ? PlayerExposureBoost : 0);
            }
            material.SetVector("_PlayerLight", light);
            material.SetFloat("_ViewAspect", (float)source.width / source.height);
            Graphics.Blit(source, destination, material);
        }
        void OnDisable()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            material = null;
        }
    }
}
