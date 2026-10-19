#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Editor-only native modal: blocks the editor player loop without modifying pause/UI ownership.
[InitializeOnLoad]
public static class PlayerProgressReset
{
    private const string Menu = "Tools/Mine Dungeon/개발용/플레이어 진행 데이터 초기화 (Play Mode)";
    private static bool showing, held;
    static PlayerProgressReset() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        var keyboard = Keyboard.current;
        bool down = Application.isPlaying && keyboard != null && keyboard.f9Key.isPressed;
        if (down && !held && keyboard.ctrlKey.isPressed && keyboard.shiftKey.isPressed) Show();
        held = down;
    }
    [MenuItem(Menu, false, 200)]
    public static void Show()
    {
        if (!Available() || showing) return;
        showing = true;
        try
        {
            // The second native button is Cancel, including the Escape key path.
            bool confirmed = EditorUtility.DisplayDialog("테스트 진행 데이터 초기화",
                "레벨, 경험치, 스탯, 골드, 아이템과 장착 상태를 초기화하고 메인 씬으로 돌아갑니다.\n게임 설정은 유지됩니다.",
                "초기화", "취소");
            if (confirmed) Confirm();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("초기화 실패", ex.Message, "확인");
        }
        finally
        {
            if (Keyboard.current != null) InputSystem.ResetDevice(Keyboard.current);
            if (Mouse.current != null) InputSystem.ResetDevice(Mouse.current);
            showing = false;
        }
    }
    private static void Confirm()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
        string e = null, r = null;
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var skills = root.GetComponentInChildren<PlayerSkills>(true);
                if (skills == null) continue;
                e = skills.Equipped(0) != null ? skills.Equipped(0).id : null;
                r = skills.Equipped(1) != null ? skills.Equipped(1).id : null;
                break;
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        string error;
        if (!CharacterSaveService.Current.EditorResetProgress(e, r, out error))
            EditorUtility.DisplayDialog("초기화 실패", error, "확인");
    }
    [MenuItem(Menu, true)]
    private static bool Available() => Application.isPlaying &&
        CharacterSaveService.Current != null && DungeonRunManager.Instance != null && !DungeonRunManager.Instance.IsRestarting;
}
#endif
