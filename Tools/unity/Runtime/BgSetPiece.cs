// Unfin 세트 조각 (L2) — 붙는 곳 표시 + '공중에 떠 보임' 검사 (Docs/20 §5-5)
//   Hang ⬇️ 천장에 매달림 : 그림 위 끝이 천장 지형 뒤로 들어가야 함
//   Rise ⬆️ 아래에서 솟음 : 밑동이 바닥 지형 뒤로 buryMin 이상 묻혀야 함 (카메라가 맨 아래일 때 기준으로 놓기)
//   Span ↕️ 바닥~천장     : 위아래 둘 다 지형 뒤로 (세로로 이어지는 그림이라 늘려 깔기 가능)
//   Wall 🧱 벽에 붙음      : 위아래 끝이 흐려지는 그림, 자유 배치
// 선택하면 빨간 선 = 지형이 최소 여기까지 가려 줘야 하는 높이
using UnityEngine;

namespace Unfin.Background
{
    public enum BgAnchor { Hang, Rise, Span, Wall }

    [ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
    public class BgSetPiece : MonoBehaviour
    {
        public string assetId;
        public BgAnchor anchor = BgAnchor.Rise;
        [Tooltip("묻는 최소 깊이(유닛) = min(화면 높이, 카메라 세로 이동) × (1 − 세로 배율) + 0.5. 바닥이 보이는 동안 조각이 바닥보다 덜/더 움직이는 양. BgRoomBuilder가 채움")]
        public float buryMin = 0.5f;

        void OnDrawGizmosSelected()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            var b = sr.bounds;
            Gizmos.color = new Color(0.9f, 0.25f, 0.2f, 0.9f);
            if (anchor == BgAnchor.Rise || anchor == BgAnchor.Span)
                Gizmos.DrawLine(new Vector3(b.min.x, b.min.y + buryMin), new Vector3(b.max.x, b.min.y + buryMin));
            if (anchor == BgAnchor.Hang || anchor == BgAnchor.Span)
                Gizmos.DrawLine(new Vector3(b.min.x, b.max.y - buryMin), new Vector3(b.max.x, b.max.y - buryMin));
        }
    }
}
