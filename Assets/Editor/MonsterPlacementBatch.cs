using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class MonsterPlacementBatch
{
    [Serializable] internal sealed class ClipEntry { public int kind; public Vector3 offset; }
    [Serializable] internal sealed class Clipboard { public ClipEntry[] entries; }
    const string ClipboardKey = "MineDungeon.PlacementClipboard";
    internal static Clipboard ReadClipboard() => JsonUtility.FromJson<Clipboard>(SessionState.GetString(ClipboardKey, "{}"));
    internal static EnemyHealth[] Selected(GameObject root) => Selection.gameObjects
        .Select(go => go.GetComponentInParent<EnemyHealth>()).Where(h => h && h.transform.IsChildOf(root.transform)).Distinct().ToArray();
    static void Check(GameObject root, EnemyHealth[] selected)
    {
        if (selected == null || selected.Length == 0 || selected.Distinct().Count() != selected.Length ||
            selected.Any(h => !h || h.transform.parent != root.transform.Find("Monsters") || MonsterPlacementStore.Species(h) == null))
            throw new InvalidOperationException("현재 맵 Monsters 아래의 일반 몬스터를 선택하세요.");
    }
    internal static void Copy(GameObject root, EnemyHealth[] selected)
    {
        Check(root, selected); Vector3 anchor = selected[0].transform.localPosition;
        var clip = new Clipboard { entries = selected.Select(h => new ClipEntry { kind = (int)MonsterProfileRuntime.Kind(h), offset = h.transform.localPosition - anchor }).ToArray() };
        SessionState.SetString(ClipboardKey, JsonUtility.ToJson(clip));
    }
    internal static void Move(GameObject root, EnemyHealth[] selected, Vector3 delta)
    {
        Check(root, selected);
        if (!MonsterPlacementStore.Finite(delta)) throw new InvalidOperationException("이동량은 유한한 숫자여야 합니다.");
        MonsterPlacementStore.Edit(root, "선택 몬스터 일괄 이동", () =>
        {
            foreach (var h in selected) MonsterPlacementStore.Move(root, h, h.transform.localPosition + delta);
        });
    }
    internal static void Delete(GameObject root, EnemyHealth[] selected)
    {
        Check(root, selected);
        MonsterPlacementStore.Edit(root, "선택 몬스터 일괄 삭제", () => { foreach (var h in selected) MonsterPlacementStore.Delete(root, h); });
    }
    internal static EnemyHealth[] Add(GameObject root, ClipEntry[] entries, Vector3 anchor, Action<EnemyHealth, int> initialize = null)
    {
        if (entries == null || entries.Length == 0 || entries.Length > 1000 || !MonsterPlacementStore.Finite(anchor) ||
            entries.Any(e => e == null || e.kind < 0 || e.kind >= 5 || !MonsterPlacementStore.Finite(e.offset)))
            throw new InvalidOperationException("추가 종류·좌표·수량(1~1000)을 확인하세요.");
        var result = new EnemyHealth[entries.Length];
        MonsterPlacementStore.Edit(root, "몬스터 그룹 추가", () =>
        {
            for (int i = 0; i < entries.Length; i++)
            {
                int kind = entries[i].kind;
                result[i] = MonsterPlacementStore.Add(root, kind, anchor + entries[i].offset, h => initialize?.Invoke(h, kind));
            }
            var report = MonsterPlacementStore.Validate(root);
            if (report.Errors.Count > 0) throw new InvalidOperationException("추가를 취소했습니다. 기준 위치/간격을 조정하세요.\n" + string.Join("\n", report.Errors));
        });
        Selection.objects = result.Select(h => (UnityEngine.Object)h.gameObject).ToArray();
        return result;
    }
    internal static ClipEntry[] Grid(int kind, int count, int columns, Vector2 spacing)
    {
        if (count < 1 || count > 1000 || columns < 1 || columns > 1000 || !MonsterPlacementStore.Finite(spacing))
            throw new InvalidOperationException("수량/열 수(1~1000)와 간격을 확인하세요.");
        return Enumerable.Range(0, count).Select(i => new ClipEntry { kind = kind, offset = new Vector3(i % columns * spacing.x, i / columns * spacing.y) }).ToArray();
    }
    internal static MonsterMapProfile CopyProfile(MonsterMapProfile source, MonsterMapProfile target)
    {
        if (!source || !target || source == target) throw new InvalidOperationException("서로 다른 원본/대상 독립 프로필이 필요합니다. 미생성 대상은 먼저 빈 프로필을 만드세요.");
        if (!source.configured) throw new InvalidOperationException("원본 배치가 미설정입니다. 의도한 0마리는 원본의 설정 완료 상태를 먼저 확인하세요.");
        var draft = MonsterProfileEditorStore.Draft(target);
        try
        {
            draft.entries = source.entries.Select(e =>
            {
                if (e == null || !e.template) throw new InvalidOperationException("원본 몬스터 참조 누락");
                if (e.reservedHpAdjustment != 0 || e.reservedAttackAdjustment != 0) throw new InvalidOperationException("미지원 개체별 보정이 있는 배치는 복사할 수 없습니다.");
                int kind = (int)MonsterProfileRuntime.Kind(e.template);
                return new MonsterMapProfile.Entry { id = Guid.NewGuid().ToString("N"),
                    template = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterDataStore.Catalog[kind].Path).GetComponent<EnemyHealth>(),
                    embeddedPath = "", localPosition = e.localPosition, localEuler = e.localEuler, localScale = e.localScale };
            }).ToArray();
            return draft;
        }
        catch { UnityEngine.Object.DestroyImmediate(draft); throw; }
    }
}
