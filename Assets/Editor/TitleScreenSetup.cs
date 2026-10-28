using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit, one-off authoring for the title trial; never runs automatically or modifies Main.
public static class TitleScreenSetup
{
    private const string Art = "Assets/Art/Title/OREPLUME_v1/";
    public const string ScenePath = "Assets/Scenes/Title.unity";

    private static Sprite Import(string source, string relative, Vector2 pivot, Vector4 border = default)
    {
        if (!File.Exists(source)) throw new FileNotFoundException("Required title art missing", source);
        string path = Art + relative;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (File.Exists(path)) throw new IOException("Preserve existing title art: " + path);
        File.Copy(source, path);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteBorder = border;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = size; r.anchoredPosition = position; return r;
    }
    private static Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = Color.white; image.raycastTarget = false;
        return image;
    }
    private static Vector2 Source(float x, float y) => new Vector2(x - 1376, 768 - y);
    private static void Ref(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
    private static void Sprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
    private static void Navigate(Button from, Button up, Button down)
    {
        from.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
    }
    private static Button MenuButton(string name, string label, Transform parent, float y, Sprite[] art, TMP_FontAsset font)
    {
        var image = Image(name, parent, art[0], new Vector2(360, 80), new Vector2(-480, y));
        image.raycastTarget = true; image.type = UnityEngine.UI.Image.Type.Sliced;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { highlightedSprite = art[1], selectedSprite = art[1], pressedSprite = art[2], disabledSprite = art[0] };
        var text = Rect("Label", image.transform, new Vector2(300, 40), Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = label; text.fontSize = 28; text.fontStyle = FontStyles.Bold;
        text.color = new Color32(228, 221, 200, 255); text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return button;
    }
    public static string BuildTrial()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit-mode scene required; preserve all existing edits.");
        if (File.Exists(ScenePath)) throw new IOException("Title scene already exists; do not overwrite it.");
        var original = SceneManager.GetActiveScene();
        var pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        var input = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (pause == null || input == null) throw new InvalidOperationException("Existing Main UI required for safe view/input reuse.");
        var pauseSO = new SerializedObject(pause);
        var sourcePanel = (GameObject)pauseSO.FindProperty("pausePanel").objectReferenceValue;
        var sourceConfirm = (GameObject)pauseSO.FindProperty("quitPage").objectReferenceValue;
        var sourceMessage = (TMP_Text)pauseSO.FindProperty("confirmationText").objectReferenceValue;
        var sourceYes = (Button)pauseSO.FindProperty("confirmButton").objectReferenceValue;
        var sourceNo = (Button)pauseSO.FindProperty("cancelButton").objectReferenceValue;
        var font = sourceMessage.font;

        string pack = "Art Resource/";
        var background = Import("C:/Users/User/Downloads/TitleScene_background.png", "Background.png", new Vector2(.5f,.5f));
        var logo = Import(pack + "TitleMenuUI_v2/Logos/OREPLUME_Logo_A_720x240_v2.png", "LogoA.png", new Vector2(.5f,.5f));
        var buttonArt = new[] { "Normal", "Hover", "Pressed" }.Select(s => Import(pack + "TitleMenuUI_v1/Buttons/TitleMenu_Button_" + s + "_360x80_v1.png", "Buttons/" + s + ".png", new Vector2(.5f,.5f), new Vector4(28,20,28,20))).ToArray();
        var idle = Enumerable.Range(1,6).Select(i => Import(pack + "TitleMinerSlime_v1/Frames/TitleMinerSlime_Idle_F" + i.ToString("00") + ".png", "Slime/Idle" + i + ".png", new Vector2(.46875f,.28125f))).ToArray();
        var shadow = Import(pack + "TitleMinerSlime_v1/Layers/TitleMinerSlime_ContactShadow_64x16_v1.png", "Slime/Shadow.png", new Vector2(.5f,.5f));
        var halo = Import(pack + "TitleLantern_v1/Glow/TitleLantern_WarmHalo_384x384_v1.png", "Lanterns/Halo.png", new Vector2(.5f,.5f));
        var flames = new Sprite[2][]; var glass = new Sprite[2]; var surface = new Sprite[2][];
        for (int l = 0; l < 2; l++)
        {
            string type = l == 0 ? "Hanging" : "Barrel";
            flames[l] = Enumerable.Range(1,6).Select(i => Import(pack + "TitleLantern_v1/Flames/Frames_" + type + "/Flame_F" + i.ToString("00") + ".png", "Lanterns/" + type + "/Flame" + i + ".png", new Vector2(.5f, l == 0 ? .125f : .15625f))).ToArray();
            glass[l] = Import(pack + "TitleLantern_v1/Glass/TitleLantern_" + type + "_GlassLight_v1.png", "Lanterns/" + type + "/Glass.png", new Vector2(.5f,.5f));
            surface[l] = new[] { "Wood", "Rock", "Floor" }.Select(s => Import(pack + "TitleLantern_v1/SurfaceLighting/TitleLantern_" + type + "_" + s + "_Light_v1.png", "Lanterns/" + type + "/" + s + ".png", new Vector2(.5f,.5f))).ToArray();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var camera = new GameObject("Title Camera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f,.025f,.03f); camera.cullingMask = 0; camera.orthographic = true;
        var canvas = new GameObject("Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        var composition = Rect("ReferenceComposition1920x1080", canvas.transform, new Vector2(1920,1080), Vector2.zero);
        var backdrop = Rect("BackgroundSpace2752x1536", composition, new Vector2(2752,1536), Vector2.zero);
        backdrop.localScale = Vector3.one * (1920f / 2752);
        Image("MineEntranceBackground", backdrop, background, new Vector2(2752,1536), Vector2.zero);
        var lanternFlames = new Image[2]; var lanternLights = new Image[2][];
        for (int l = 0; l < 2; l++)
        {
            var layer = Rect(l == 0 ? "HangingLantern" : "BarrelLantern", backdrop, new Vector2(2752,1536), Vector2.zero);
            var lights = new Image[5];
            for (int i = 0; i < 3; i++) lights[i] = Image(new[] { "WoodLight", "RockLight", "FloorLight" }[i], layer, surface[l][i], new Vector2(1024,1024), Source(2240,1024));
            Vector2 center = l == 0 ? Source(2344,800) : Source(2476,1078);
            lights[3] = Image("Halo", layer, halo, Vector2.one * (l == 0 ? 384 : 256), center);
            lights[4] = Image("GlassLight", layer, glass[l], l == 0 ? new Vector2(64,80) : new Vector2(40,48), center);
            var flame = Image("Flame", layer, flames[l][0], l == 0 ? new Vector2(32,48) : new Vector2(24,32), l == 0 ? Source(2338,820) : Source(2476,1090));
            flame.rectTransform.pivot = new Vector2(.5f, l == 0 ? .125f : .15625f);
            lanternFlames[l] = flame; lanternLights[l] = lights;
        }
        Image("SlimeContactShadow", backdrop, shadow, new Vector2(192,48), Source(2180,1319));
        var slime = Image("MinerSlimePlacementA", backdrop, idle[0], new Vector2(384,384), Source(2180,1310));
        slime.rectTransform.pivot = new Vector2(.46875f,.28125f);
        var logoImage = Image("OREPLUME Logo A", composition, logo, new Vector2(720,240), new Vector2(-480,300));
        logoImage.preserveAspect = true;
        var start = MenuButton("StartGame", "게임 시작", composition, 0, buttonArt, font);
        var settingsButton = MenuButton("SettingsPending", "설정", composition, -104, buttonArt, font); settingsButton.interactable = false;
        var quit = MenuButton("QuitGame", "게임 종료", composition, -208, buttonArt, font);
        Navigate(start, quit, quit); Navigate(quit, start, start);

        // Reuse the existing confirm/cancel view, without bringing PauseMenu or its gameplay dependencies.
        var overlay = Rect("ExistingQuitConfirmation", canvas.transform, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        var dim = overlay.gameObject.AddComponent<Image>(); dim.color = sourcePanel.GetComponent<Image>().color; dim.raycastTarget = true;
        var sourceCard = (RectTransform)sourceConfirm.transform.parent;
        var card = Rect("ExistingPauseCard", overlay, sourceCard.sizeDelta, Vector2.zero);
        var cardImage = card.gameObject.AddComponent<Image>(); cardImage.color = sourceCard.GetComponent<Image>().color; cardImage.raycastTarget = true;
        var confirmPage = Object.Instantiate(sourceConfirm, card, false); confirmPage.name = sourceConfirm.name;
        var message = confirmPage.transform.Find(AnimationUtility.CalculateTransformPath(sourceMessage.transform, sourceConfirm.transform)).GetComponent<TMP_Text>();
        var yes = confirmPage.transform.Find(AnimationUtility.CalculateTransformPath(sourceYes.transform, sourceConfirm.transform)).GetComponent<Button>();
        var no = confirmPage.transform.Find(AnimationUtility.CalculateTransformPath(sourceNo.transform, sourceConfirm.transform)).GetComponent<Button>();
        yes.onClick = new Button.ButtonClickedEvent(); no.onClick = new Button.ButtonClickedEvent();
        confirmPage.SetActive(true); overlay.gameObject.SetActive(false);
        Navigate(yes, no, no); Navigate(no, yes, yes);
        var eventSystem = Object.Instantiate(input.gameObject); eventSystem.name = "EventSystem"; SceneManager.MoveGameObjectToScene(eventSystem, scene);
        var controller = canvas.gameObject.AddComponent<TitleScreen>(); var so = new SerializedObject(controller);
        Ref(so,"viewport",canvas.transform); Ref(so,"composition",composition); Ref(so,"slime",slime); Sprites(so.FindProperty("slimeFrames"), idle);
        Ref(so,"startButton",start); Ref(so,"settingsButton",settingsButton); Ref(so,"quitButton",quit); Ref(so,"quitOverlay",overlay.gameObject);
        Ref(so,"quitMessage",message); Ref(so,"confirmQuitButton",yes); Ref(so,"cancelQuitButton",no);
        var lanterns = so.FindProperty("lanterns"); lanterns.arraySize = 2;
        for (int l = 0; l < 2; l++)
        {
            var p = lanterns.GetArrayElementAtIndex(l); p.FindPropertyRelative("flame").objectReferenceValue = lanternFlames[l];
            Sprites(p.FindPropertyRelative("frames"), flames[l]); var lights = p.FindPropertyRelative("lights"); lights.arraySize = 5;
            for (int i = 0; i < 5; i++) lights.GetArrayElementAtIndex(i).objectReferenceValue = lanternLights[l][i];
            p.FindPropertyRelative("frameSeconds").floatValue = l == 0 ? .14f : .16f;
            p.FindPropertyRelative("frameOffset").floatValue = l == 0 ? 0 : .32f;
            p.FindPropertyRelative("lightSeconds").floatValue = l == 0 ? 2.6f : 2.9f;
            p.FindPropertyRelative("lightPhase").floatValue = l == 0 ? 0 : 1.1f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(original, true);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes).ToArray();
        AssetDatabase.SaveAssets();
        return "Title created, 33 art PNGs imported; existing Main and saves untouched. Settings intentionally pending.";
    }
}
