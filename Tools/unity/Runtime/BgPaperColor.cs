// Unfin L0 종이 — 방이 켜지면 카메라 배경색을 그 장의 종이 색으로 (뒷벽 틈으로 비치는 색, 명도 약 70%)
using UnityEngine;

namespace Unfin.Background
{
    public class BgPaperColor : MonoBehaviour
    {
        public Color color = new Color32(0xBD, 0xBC, 0xB5, 0xFF);
        public Camera cam;

        void OnEnable()
        {
            var c = cam != null ? cam : Camera.main;
            if (c == null) return;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = color;
        }
    }
}
