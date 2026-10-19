using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterSaveSetup
{
    static TMP_FontAsset font;
    static RectTransform Rect(string name, Transform parent, float x,float y,float xx,float yy)
    { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;return r; }
    static TMP_Text Text(string name,Transform parent,string value,int size,float x,float y,float xx,float yy)
    {var t=Rect(name,parent,x,y,xx,yy).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.text=value;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static Image Image(RectTransform r,Color c){var i=r.gameObject.AddComponent<Image>();i.color=c;return i;}
    static Button Button(string n,Transform p,string label,float y)
    {var r=Rect(n,p,.12f,y,.88f,y+.12f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=Image(r,new Color(.12f,.22f,.25f));Text("Label",r,label,28,0,0,1,1);return b;}
    static void Ref(SerializedObject o,string n,Object v){o.FindProperty(n).objectReferenceValue=v;}
    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode required");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Mine Entrance Korean.asset");
        var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="C:/Windows/Fonts/NotoSansKR-VF.ttf";fs.ApplyModifiedPropertiesWithoutUndo();font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        string chars="일시정지 계속하기 설정 광산 밖으로 귀환 게임 종료 카메라 흔들림 끄기 기본 강하게 확인 취소 돌아가기";
        foreach(var path in new[]{"Assets/Scripts/CharacterSaveService.cs","Assets/Scripts/PauseMenu.cs"})chars+=System.IO.File.ReadAllText(path);
        string missing;font.TryAddCharacters(chars,out missing);font.atlasPopulationMode=AtlasPopulationMode.Static;fs.Update();fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(font);
        foreach(var path in new[]{"Assets/Scenes/TestRoom.unity","Assets/Scenes/Main.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);var run=Object.FindFirstObjectByType<DungeonRunManager>();
            var service=run.GetComponent<CharacterSaveService>();if(service==null)service=run.gameObject.AddComponent<CharacterSaveService>();
            var ss=new SerializedObject(service);var skills=AssetDatabase.FindAssets("t:SkillData").Select(g=>AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();var mats=AssetDatabase.FindAssets("t:MaterialData").Select(g=>AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var sa=ss.FindProperty("skillCatalog");sa.arraySize=skills.Length;for(int i=0;i<skills.Length;i++)sa.GetArrayElementAtIndex(i).objectReferenceValue=skills[i];var ma=ss.FindProperty("materialCatalog");ma.arraySize=mats.Length;for(int i=0;i<mats.Length;i++)ma.GetArrayElementAtIndex(i).objectReferenceValue=mats[i];ss.ApplyModifiedPropertiesWithoutUndo();
            var canvas=Object.FindFirstObjectByType<Canvas>();var pause=Object.FindFirstObjectByType<PauseMenu>();var so=new SerializedObject(pause);var old=(GameObject)so.FindProperty("pausePanel").objectReferenceValue;Object.DestroyImmediate(old);
            var root=Rect("PausePanel",canvas.transform,0,0,1,1);Image(root,new Color(0,0,0,.65f));Ref(so,"pausePanel",root.gameObject);
            var body=Rect("PauseCard",root,.5f,.5f,.5f,.5f);body.sizeDelta=new Vector2(620,600);Image(body,new Color(.055f,.075f,.095f));var fit=body.gameObject.AddComponent<CenteredWindowLayout>();var fitSo=new SerializedObject(fit);fitSo.FindProperty("preferredSize").vector2Value=new Vector2(620,600);fitSo.ApplyModifiedPropertiesWithoutUndo();
            var menu=Rect("MenuPage",body,0,0,1,1);Ref(so,"menuPage",menu.gameObject);Text("Title",menu,"일시정지",38,.05f,.81f,.95f,.95f);
            Ref(so,"resumeButton",Button("Resume",menu,"계속하기",.64f));Ref(so,"settingsButton",Button("Settings",menu,"설정",.47f));Ref(so,"returnButton",Button("Return",menu,"광산 밖으로 귀환",.30f));Ref(so,"quitButton",Button("Quit",menu,"게임 종료",.13f));
            var settings=Rect("SettingsPage",body,0,0,1,1);Ref(so,"controlsPage",settings.gameObject);Text("Title",settings,"설정",38,.05f,.81f,.95f,.95f);Text("ShakeTitle",settings,"카메라 흔들림",28,.1f,.62f,.9f,.74f);
            var sr=Rect("ShakeMultiplier",settings,.15f,.48f,.85f,.57f);Image(sr,new Color(.1f,.15f,.18f));var slider=sr.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=2;slider.value=1;var handle=Rect("Handle",sr,0,0,0,1);handle.sizeDelta=new Vector2(28,0);var hi=Image(handle,new Color(.3f,.9f,.8f));slider.handleRect=handle;slider.targetGraphic=hi;Ref(so,"shakeSlider",slider);Text("Hint",settings,"0 끄기   ·   1 기본   ·   2 강하게",22,.05f,.35f,.95f,.45f);Ref(so,"settingsBackButton",Button("Back",settings,"돌아가기",.13f));
            var confirm=Rect("ConfirmPage",body,0,0,1,1);Ref(so,"quitPage",confirm.gameObject);Ref(so,"confirmationText",Text("Message",confirm,"",26,.08f,.35f,.92f,.91f));Ref(so,"confirmButton",Button("Confirm",confirm,"확인 / 재시도",.18f));Ref(so,"cancelButton",Button("Cancel",confirm,"취소",.03f));Ref(so,"errorText",Text("Error",confirm,"",20,.05f,.30f,.95f,.37f));
            so.ApplyModifiedPropertiesWithoutUndo();root.gameObject.SetActive(false);
            var notice=canvas.transform.Find("SaveStatus");if(notice!=null)Object.DestroyImmediate(notice.gameObject);
            var status=Text("SaveStatus",canvas.transform,"",22,.15f,.02f,.85f,.10f);status.color=new Color(1,.75f,.4f);var view=status.gameObject.AddComponent<SaveStatusView>();var vs=new SerializedObject(view);Ref(vs,"label",status);vs.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();return "Save catalogs and pause UI saved in Main / TestRoom";
    }
}
