// Unfin 배경 패럴랙스 — 방 가운데 기준 (Docs/20 §5-3)
// 층 위치 = 방 가운데 + (카메라 − 방 가운데) × (1 − f)
//   f = 플레이 층이 1 움직일 때 이 층이 화면에서 움직이는 양
//   뒷벽 (0.2, 0.6) · 세트 조각 (0.5, 0.8) · 플레이 (1, 1) · 전경 (1.3, 0 = 세로는 화면 고정)
// 카메라가 방 가운데에 있을 때 = 씬 뷰에서 놓은 자리 그대로 보임 → 배치는 '카메라가 방 가운데'라고 생각하고 하면 됨
using UnityEngine;

namespace Unfin.Background
{
    [ExecuteAlways]
    public class BgParallax : MonoBehaviour
    {
        [Tooltip("x = 가로, y = 세로. 1 = 플레이 층과 같이 움직임, 0 = 화면에 붙어 있음")]
        public Vector2 factor = new Vector2(0.2f, 0.6f);
        [Tooltip("방 가운데 (월드 좌표). BgRoomBuilder가 채움")]
        public Vector2 roomCenter;
        [Tooltip("비우면 Camera.main")]
        public Camera cam;
        [Tooltip("에디터에서 멈춰 있을 때는 방 가운데 기준 자리에 둠 (배치용)")]
        public bool previewInEditor = false;

        void LateUpdate()
        {
            var c = cam != null ? cam : Camera.main;
            if (c == null) return;
            Vector2 camPos = c.transform.position;
            if (!Application.isPlaying && !previewInEditor) camPos = roomCenter;
            Vector2 p = roomCenter + Vector2.Scale(camPos - roomCenter, Vector2.one - factor);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        // 이 층에서 화면에 보일 수 있는 범위 (방 가운데 기준 한쪽 거리) — bg_manifest.py extent()와 같은 식
        public static float Extent(float roomSize, float screenSize, float f, float margin = 1f)
            => screenSize * 0.5f + Mathf.Max(0f, roomSize * 0.5f - screenSize * 0.5f) * f + margin;
    }
}
