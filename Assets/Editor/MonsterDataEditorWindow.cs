using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class MonsterDataEditorWindow : EditorWindow
{
    [Serializable] sealed class Cell { public string key, value; }
    [SerializeField] List<Cell> savedDraft = new List<Cell>();
    Dictionary<string, string> draft;
    MonsterDataStore.Plan preview;
    Vector2 scroll;
    int selected;
    string message;
    bool showDifferences = true;
    [SerializeField] int tab;
    [SerializeField] MonsterPlacementPanel placement = new MonsterPlacementPanel();

    [MenuItem("Tools/MineDungeon/데이터 편집기")]
    public static void Open() => GetWindow<MonsterDataEditorWindow>("몬스터 데이터").Show();
    void OnEnable()
    {
        minSize = new Vector2(1030, 520);
        Undo.undoRedoPerformed += AfterUndo;
        placement.Enable(Repaint);
        try
        {
            draft = MonsterDataStore.Load();
            foreach (var cell in savedDraft) if (draft.ContainsKey(cell.key)) draft[cell.key] = cell.value;
        }
        catch (Exception e) { message = e.Message; }
    }
    void OnDisable() { Undo.undoRedoPerformed -= AfterUndo; placement.Disable(); StoreDraft(); }
    void StoreDraft()
    {
        if (draft != null) savedDraft = draft.Select(p => new Cell { key = p.Key, value = p.Value }).ToList();
    }
    void AfterUndo() { preview = null; message = "Undo/Redo가 반영되었습니다. 저장 수치를 다시 불러와 확인할 수 있습니다."; Repaint(); }
    void Run(Action action)
    {
        try { action(); StoreDraft(); }
        catch (Exception e) { message = e.Message; preview = null; }
    }
    void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, new[] { "몬스터 수치", "몬스터 배치" });
        if (tab == 1) { placement.Draw(); return; }
        DrawStats();
    }
    void DrawStats()
    {
        EditorGUILayout.HelpBox("공용 몬스터 5종 + Map_01~07의 같은 종류 전체를 입력값으로 맞춥니다. 맵의 기존 차이도 적용 대상입니다.\nCSV는 편집 초안만 변경합니다. 실제 게임 데이터는 기존 프리팹을 사용합니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("저장 수치 다시 불러오기", EditorStyles.toolbarButton)) Run(() =>
                {
                    if (draft != null && !EditorUtility.DisplayDialog("초안 초기화", "편집 중인 값을 버리고 저장된 공용 프리팹 수치를 불러올까요?", "불러오기", "취소")) return;
                    draft = MonsterDataStore.Load(); preview = null; message = "저장 수치를 불러왔습니다.";
                });
                if (GUILayout.Button("CSV 가져오기", EditorStyles.toolbarButton)) Run(() =>
                {
                    string path = EditorUtility.OpenFilePanel("몬스터 CSV 가져오기", "", "csv");
                    if (path.Length == 0) return;
                    draft = MonsterDataStore.ImportCsv(File.ReadAllText(path, Encoding.UTF8), draft);
                    preview = MonsterDataStore.BuildPlan(draft); message = "CSV 초안 로드 완료. 프리팹은 아직 변경하지 않았습니다.";
                });
                if (GUILayout.Button("CSV 내보내기", EditorStyles.toolbarButton)) Run(() =>
                {
                    string csv = MonsterDataStore.ExportCsv(draft);
                    string path = EditorUtility.SaveFilePanel("현재 표 수치 CSV 내보내기", "", "MonsterData.csv", "csv");
                    if (path.Length > 0) { File.WriteAllText(path, csv, new UTF8Encoding(true)); message = "현재 편집 초안을 내보냈습니다: " + path; }
                });
                if (GUILayout.Button("검증 / 적용 미리보기", EditorStyles.toolbarButton)) Run(() =>
                { preview = MonsterDataStore.BuildPlan(draft); message = $"검증 완료: 변경 {preview.Changes.Count}필드 / {preview.ChangedPaths.Length}개 프리팹"; });
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (draft != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("종류 / ID", GUILayout.Width(138));
                    foreach (var f in MonsterDataStore.Catalog[0].Fields.Take(8)) GUILayout.Label(f.Label, GUILayout.Width(96));
                }
                for (int i = 0; i < MonsterDataStore.Catalog.Length; i++)
                {
                    var s = MonsterDataStore.Catalog[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(selected == i, s.Label + " / " + s.Id, "Button", GUILayout.Width(138))) selected = i;
                        foreach (var f in s.Fields.Take(8)) DrawCell(s, f, 96);
                    }
                }
                var species = MonsterDataStore.Catalog[selected];
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(species.Label + " 추가 수치", EditorStyles.boldLabel);
                foreach (var f in species.Fields.Skip(8))
                    using (new EditorGUILayout.HorizontalScope())
                    { GUILayout.Label(f.Label + " (" + f.Key + ")", GUILayout.Width(300)); DrawCell(species, f, 180); }
            }
            if (preview != null)
            {
                EditorGUILayout.Space();
                showDifferences = EditorGUILayout.Foldout(showDifferences, $"공용 / 맵 개체의 현재 저장값 차이 ({preview.Differences.Count})", true);
                if (showDifferences) foreach (string difference in preview.Differences) EditorGUILayout.LabelField(difference, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"적용 예정: {preview.Changes.Count}필드 / {preview.ChangedPaths.Length}개 프리팹", EditorStyles.boldLabel);
                foreach (string path in preview.ChangedPaths)
                {
                    EditorGUILayout.LabelField(path, EditorStyles.boldLabel);
                    foreach (var c in preview.Changes.Where(c => c.Path == path)) EditorGUILayout.LabelField(c.Description, EditorStyles.wordWrappedMiniLabel);
                }
                using (new EditorGUI.DisabledScope(preview.Changes.Count == 0))
                    if (GUILayout.Button("위 변경사항 확인 후 적용", GUILayout.Height(30))) Run(() =>
                    {
                        if (!EditorUtility.DisplayDialog("몬스터 데이터 적용 확인", $"미리보기의 {preview.Changes.Count}개 수치를 {preview.ChangedPaths.Length}개 프리팹에 저장합니다.\n공용 수치와 다른 맵 개체도 표시된 입력값으로 변경됩니다.\n계속할까요? (Undo 지원)", "적용", "취소")) return;
                        int count = MonsterDataStore.Apply(preview);
                        preview = null; message = $"{count}개 프리팹 저장 완료. Ctrl+Z / Ctrl+Y로 Undo / Redo할 수 있습니다.";
                    });
            }
            EditorGUILayout.EndScrollView();
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
        EditorGUILayout.LabelField("CSV: monsterId,field,value · ID/필드명 대소문자 구분 · 소수점은 . · 일부 행만 가져오기 가능", EditorStyles.miniLabel);
    }
    void DrawCell(MonsterDataStore.Species s, MonsterDataStore.Field f, float width)
    {
        string key = MonsterDataStore.Key(s, f);
        string old = draft[key];
        bool valid = true;
        try { MonsterDataStore.Parse(f, old); } catch (FormatException) { valid = false; }
        Color previous = GUI.backgroundColor;
        if (!valid) GUI.backgroundColor = new Color(1, .5f, .5f);
        string value = EditorGUILayout.TextField(new GUIContent("", s.Id + "." + f.Key), old, GUILayout.Width(width));
        GUI.backgroundColor = previous;
        if (old != value) { draft[key] = value; preview = null; StoreDraft(); }
    }
}
