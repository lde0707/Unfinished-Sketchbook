// Unfin 방 배경 자동 배치 — Art/bg_unity_manifest.json 을 읽어서 방마다 L1 뒷벽·L2 세트 조각·L4 전경을 깔아 줌 (Docs/20 §5)
// 메뉴: Unfin ▸ 배경 ▸ 방 배경 만들기 / 정렬 레이어 만들기
// 전제: 그림은 Assets/Art/<목록의 파일 경로> 에 (Art/out 폴더 내용을 그대로 복사), 매니페스트는 Assets/Art/bg_unity_manifest.json
// 뒷벽 칸 고르기(변형 섞기·이음 자리·세로 뒷벽)는 bg_manifest.py가 이미 정함 → 여기서는 그대로 깔기만 (파이썬 미리보기와 똑같이 나옴)
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unfin.Background.EditorTools
{
    [Serializable] public class BgManifest { public BgAsset[] asset_list; public BgRoom[] rooms; }
    [Serializable] public class BgAsset { public string id, file, type, final; }
    [Serializable] public class BgRoom
    {
        public string id, kind, zone, area, area_name, name; public int chapter;
        public BgRect unity; public float[] screens; public BgLayers layers; public BgLink[] links; public string[] warnings;
    }
    [Serializable] public class BgRect { public float x, y, w, h; public float[] center; }
    [Serializable] public class BgLayers { public BgPaper paper; public BgWall wall; public BgSet set; public BgFore fore; }
    [Serializable] public class BgPaper { public float[] parallax; public string sorting, color; }
    [Serializable] public class BgWall { public float[] parallax; public string sorting, tall; public float tile; public float[] cover, offset; public BgTile[] tiles; }
    [Serializable] public class BgTile { public int x, y, rows; public string id, role; }
    [Serializable] public class BgSet { public float[] parallax; public string sorting; public float bury_min; public BgPiece[] landmarks, shared; }
    [Serializable] public class BgPiece { public string id, anchor; }
    [Serializable] public class BgFore { public float[] parallax; public string sorting, id; public float alpha; }
    [Serializable] public class BgLink { public string side, to_area, dir, wall_link; public string[] terrain_links; public bool chapter_border; }

    public class BgRoomBuilder : EditorWindow
    {
        public const string ManifestPath = "Assets/Art/bg_unity_manifest.json";
        public static readonly string[] SortingLayers = { "BG_Paper", "BG_Wall", "BG_Set", "Terrain", "Decor", "Actors", "Fx", "Fore" };
        const float ScreenW = 30f, ScreenH = 16.875f;

        string roomId = "ch1-r01";
        static bool placeShared = false;

        [MenuItem("Unfin/배경/방 배경 만들기")]
        static void Open() => GetWindow<BgRoomBuilder>("Unfin 방 배경");

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Art/out 그림을 Assets/Art/ 로, bg_unity_manifest.json 을 Assets/Art/ 로 복사한 뒤 사용.\n" +
                                    "방 ID 예: ch1-r09 (1장 9번 방), ch1-s06 (1장 수직굴 6)", MessageType.Info);
            roomId = EditorGUILayout.TextField("방 ID", roomId);
            placeShared = EditorGUILayout.Toggle("공용 조각도 1개씩 놓기", placeShared);
            if (GUILayout.Button("이 방 만들기 / 다시 만들기")) Build(roomId);
            if (GUILayout.Button("모든 방 만들기")) Build(null);
            if (GUILayout.Button("정렬 레이어 만들기")) EnsureSortingLayers();
        }

        static BgManifest Load()
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            if (ta == null) { Debug.LogError($"[Unfin 배경] {ManifestPath} 없음 → python Tools/bg_manifest.py build 후 복사"); return null; }
            return JsonUtility.FromJson<BgManifest>(ta.text);
        }

        static void Build(string only)
        {
            var m = Load(); if (m == null) return;
            var files = new Dictionary<string, string>();
            foreach (var a in m.asset_list) files[a.id] = "Assets/Art/" + a.file;
            int n = 0, missing = 0;
            foreach (var r in m.rooms)
            {
                if (only != null && r.id != only) continue;
                missing += BuildRoom(r, files); n++;
            }
            Debug.Log($"[Unfin 배경] 방 {n}개 만듦 · 빠진 그림 {missing}개 (빠진 칸은 비워 둠 → bg_manifest.py check 로 확인)");
        }

        static Sprite Spr(Dictionary<string, string> files, string id, ref int missing)
        {
            if (string.IsNullOrEmpty(id) || !files.TryGetValue(id, out var path)) { missing++; return null; }
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) { missing++; Debug.LogWarning($"[Unfin 배경] 그림 없음: {id} ({path})"); }
            return s;
        }

        static GameObject Layer(Transform parent, string name, float[] f, Vector2 center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0);
            var p = go.AddComponent<BgParallax>();
            p.factor = new Vector2(f[0], f[1]); p.roomCenter = center;
            return go;
        }

        static SpriteRenderer Put(Transform parent, string name, Sprite s, Vector2 local, string sorting, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingLayerName = sorting; sr.sortingOrder = order;
            return sr;
        }

        static int BuildRoom(BgRoom r, Dictionary<string, string> files)
        {
            int missing = 0;
            string rootName = $"BG_{r.id}";
            var old = GameObject.Find(rootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Unfin 방 배경");
            var c = new Vector2(r.unity.center[0], r.unity.center[1]);

            // L0 종이 = 카메라 배경색 (방에 들어올 때 BgPaperColor가 바꿔 줌)
            var paper = root.AddComponent<BgPaperColor>();
            ColorUtility.TryParseHtmlString(r.layers.paper.color, out paper.color);

            // L1 뒷벽: 칸 목록 그대로. 칸 가운데 = 오프셋 + (x + 0.5, y + 칸높이/2) × 16
            var W = r.layers.wall; float T = W.tile;
            var wall = Layer(root.transform, "L1_Wall", W.parallax, c);
            foreach (var t in W.tiles)
            {
                var pos = new Vector2(W.offset[0] + (t.x + 0.5f) * T, W.offset[1] + (t.y + t.rows * 0.5f) * T);
                Put(wall.transform, $"{t.role}_{t.x}_{t.y}_{t.id}", Spr(files, t.id, ref missing), pos, W.sorting, t.role == "link" ? 1 : 0);
            }

            // L2 세트 조각: 대표 조각은 붙는 곳에 맞는 기본 자리에 놓음 → 사람이 옮겨서 다듬기
            var S = r.layers.set;
            var set = Layer(root.transform, "L2_Set", S.parallax, c);
            // 바닥·천장이 보일 때(카메라가 맨 아래/맨 위) 기준으로 놓음: 층 좌표 = ∓방 높이/2 ± 카메라 이동 × (1 − 세로 배율)
            float hw = Mathf.Min(r.unity.w, ScreenW) * 0.4f;
            float travel = Mathf.Max(0f, (r.unity.h - ScreenH) * 0.5f);
            float floorY = -r.unity.h * 0.5f + travel * (1f - S.parallax[1]), ceilY = -floorY;
            var pieces = new List<BgPiece>(S.landmarks);
            if (placeShared && S.shared.Length > 0) pieces.Add(S.shared[(r.id.GetHashCode() & 0x7FFFFFFF) % S.shared.Length]);
            for (int i = 0; i < pieces.Count; i++)
            {
                var pc = pieces[i];
                var s = Spr(files, pc.id, ref missing);
                float x = (i - (pieces.Count - 1) * 0.5f) * hw;
                float ext = s != null ? s.bounds.extents.y : 4f;
                var a = pc.anchor == "hang" ? BgAnchor.Hang : pc.anchor == "rise" ? BgAnchor.Rise : pc.anchor == "span" ? BgAnchor.Span : BgAnchor.Wall;
                float y = a == BgAnchor.Hang ? ceilY - ext + S.bury_min : a == BgAnchor.Rise ? floorY + ext - S.bury_min : 0f;
                var sr = Put(set.transform, pc.id, s, new Vector2(x, y), S.sorting, i);
                var sp = sr.gameObject.AddComponent<BgSetPiece>();
                sp.assetId = pc.id; sp.anchor = a; sp.buryMin = S.bury_min;
                if (a == BgAnchor.Span && s != null)   // 바닥~천장: 세로로 이어지는 그림 → 방 높이만큼 늘려 깔기
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(s.bounds.size.x, BgParallax.Extent(r.unity.h, ScreenH, S.parallax[1]) * 2f);
                }
            }

            // L4 전경: 가로로 이어지는 띠, 세로는 화면 높이에 맞춤 (세로 배율 0 = 화면 고정)
            var F = r.layers.fore;
            var fore = Layer(root.transform, "L4_Fore", F.parallax, c);
            var fs = Spr(files, F.id, ref missing);
            if (fs != null)
            {
                var sr = Put(fore.transform, F.id, fs, Vector2.zero, F.sorting, 0);
                float k = ScreenH / fs.bounds.size.y;
                sr.transform.localScale = new Vector3(k, k, 1);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(BgParallax.Extent(r.unity.w, ScreenW, F.parallax[0]) * 2f / k, fs.bounds.size.y);
                sr.color = new Color(1, 1, 1, F.alpha);
            }

            foreach (var w in r.warnings) Debug.LogWarning($"[Unfin 배경] {r.id} {r.name}: {w}");
            return missing;
        }

        [MenuItem("Unfin/배경/정렬 레이어 만들기")]
        public static void EnsureSortingLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("m_SortingLayers");
            var have = new HashSet<string>();
            for (int i = 0; i < layers.arraySize; i++) have.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            foreach (var name in SortingLayers)
            {
                if (have.Contains(name)) continue;
                layers.InsertArrayElementAtIndex(layers.arraySize);
                var e = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                e.FindPropertyRelative("name").stringValue = name;
                e.FindPropertyRelative("uniqueID").intValue = (int)((uint)name.GetHashCode() & 0x7FFFFFFF);
            }
            tm.ApplyModifiedProperties();
            Debug.Log("[Unfin 배경] 정렬 레이어: " + string.Join(" < ", SortingLayers) + " (Default는 맨 뒤에 그대로, 순서는 Project Settings ▸ Tags and Layers에서 확인)");
        }
    }
}
