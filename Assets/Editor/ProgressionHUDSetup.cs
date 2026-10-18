#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ProgressionHUDSetup
{
    static TMP_FontAsset font;
    static RectTransform R(string name,Transform p,float x0,float y0,float x1,float y1)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Place(r,x0,y0,x1,y1);return r;}
    static void Place(RectTransform r,float x0,float y0,float x1,float y1)
    {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;}
    static Image I(RectTransform r,Color c){var i=r.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
    static TMP_Text T(string name,Transform p,string text,float size,float x0,float y0,float x1,float y1)
    {var t=R(name,p,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static void Ref(SerializedObject s,string field,Object o){s.FindProperty(field).objectReferenceValue=o;}
    static void Frame(RectTransform p){var i=I(R("Border",p,0,0,1,1),new Color(.34f,.42f,.43f));i.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");i.type=Image.Type.Sliced;i.fillCenter=false;}
    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode only");
        var canvas=Object.FindFirstObjectByType<Canvas>();if(canvas.transform.Find("PlayerStatusPanel")!=null)throw new System.Exception("Already configured");
        var chars=new HashSet<char>(Enumerable.Range(32,95).Select(x=>(char)x));
        foreach(var path in new[]{"Assets/Scripts/PlayerHUD.cs","Assets/Scripts/Inventory/InventoryUI.cs","Assets/Editor/ProgressionHUDSetup.cs"})foreach(char c in System.IO.File.ReadAllText(path))if(c>127)chars.Add(c);
        font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
        string missing;font.TryAddCharacters(new string(chars.ToArray()),out missing);if(!string.IsNullOrEmpty(missing))throw new System.Exception(missing);
        font.name="Progression Korean";font.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(font,"Assets/UI/Progression Korean.asset");foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);AssetDatabase.AddObjectToAsset(font.material,font);var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();
        var data=ScriptableObject.CreateInstance<LevelProgressionData>();AssetDatabase.CreateAsset(data,"Assets/Skills/LevelProgression.asset");
        var run=Object.FindFirstObjectByType<DungeonRunManager>();var progression=run.gameObject.AddComponent<PlayerProgression>();var ps=new SerializedObject(progression);Ref(ps,"configuration",data);ps.ApplyModifiedPropertiesWithoutUndo();
        var hud=canvas.GetComponent<PlayerHUD>();var so=new SerializedObject(hud);
        var panel=R("PlayerStatusPanel",canvas.transform,.015f,.775f,.25f,.98f);panel.SetSiblingIndex(1);I(panel,new Color(.035f,.055f,.07f,.92f));Frame(panel);
        // A simple helmet made from existing UI shapes, no gameplay component cloned.
        var helmet=R("Helmet",panel,.04f,.73f,.15f,.93f);I(helmet,new Color(.9f,.7f,.3f));I(R("Brim",helmet,-.10f,0,1.10f,.20f),new Color(.68f,.49f,.18f));I(R("Lamp",helmet,.36f,.30f,.64f,.75f),new Color(1f,.97f,.7f));
        Ref(so,"levelText",T("Level",panel,"Lv.",27,.17f,.73f,.47f,.96f));
        var hb=(Slider)so.FindProperty("healthBar").objectReferenceValue;hb.transform.SetParent(panel,false);Place((RectTransform)hb.transform,.045f,.49f,.955f,.70f);
        foreach(string n in new[]{"Background","Fill Area"})Place((RectTransform)hb.transform.Find(n),0,0,1,1);
        hb.fillRect.offsetMin=hb.fillRect.offsetMax=Vector2.zero;hb.transform.Find("Handle Slide Area").gameObject.SetActive(false);
        var ht=(TMP_Text)so.FindProperty("healthText").objectReferenceValue;ht.transform.SetParent(panel,false);Place(ht.rectTransform,.05f,.49f,.95f,.70f);ht.font=font;ht.fontSize=24;ht.alignment=TextAlignmentOptions.Center;ht.raycastTarget=false;
        var exp=R("ExperienceBar",panel,.045f,.34f,.955f,.44f);I(exp,new Color(.06f,.13f,.15f));var fill=I(R("Fill",exp,0,0,1,1),new Color(.15f,.78f,.70f));Ref(so,"experienceFill",fill);Ref(so,"experienceText",T("Experience",panel,"EXP",17,.05f,.32f,.95f,.46f));
        T("LivesLabel",panel,"남은 생명",18,.04f,.07f,.36f,.25f);
        var lives=so.FindProperty("lifeTalismans");lives.arraySize=3;
        for(int i=0;i<3;i++){var tal=R("LifeTalisman"+(i+1),panel,.39f+i*.13f,.07f,.47f+i*.13f,.27f);var im=I(tal,new Color(.25f,1,.85f));lives.GetArrayElementAtIndex(i).objectReferenceValue=im;I(R("Inscription",tal,.40f,.23f,.60f,.78f),new Color(.04f,.15f,.15f));}
        var revive=(TMP_Text)so.FindProperty("reviveText").objectReferenceValue;revive.gameObject.SetActive(false);
        var status=(TMP_Text)so.FindProperty("statusText").objectReferenceValue;status.transform.SetParent(panel,false);Place(status.rectTransform,.77f,.06f,.98f,.28f);status.font=font;status.fontSize=15;status.raycastTarget=false;
        var slots=(RectTransform)Object.FindFirstObjectByType<PlayerSkillHUD>().transform;Place(slots,.29f,.023f,.71f,.112f);
        var e=(RectTransform)slots.Find("E Slot");var r=(RectTransform)slots.Find("R Slot");Place(e,.34f,0,.66f,1);Place(r,.68f,0,1,1);
        foreach(var slot in new[]{e,r}){
            Place((RectTransform)slot.Find("Icon"),.08f,.22f,.30f,.72f);Place((RectTransform)slot.Find("marker"),.08f,.22f,.30f,.72f);
            Place((RectTransform)slot.Find("keyText"),.03f,.73f,.18f,.99f);Place((RectTransform)slot.Find("nameText"),.32f,.48f,.98f,.92f);Place((RectTransform)slot.Find("remainingText"),.34f,.08f,.96f,.46f);
            var name=slot.Find("nameText").GetComponent<TMP_Text>();name.enableAutoSizing=true;name.fontSize=22;name.fontSizeMin=15;name.fontSizeMax=22;Frame(slot);
        }
        var q=R("Q Potion",slots,0,0,.32f,1);I(q,e.GetComponent<Image>().color);Frame(q);T("Key",q,"Q",22,.03f,.73f,.18f,.99f);T("Name",q,"물약",22,.32f,.48f,.98f,.92f);
        var bottle=R("PotionIcon",q,.12f,.25f,.27f,.65f);I(bottle,new Color(.7f,.16f,.22f));I(R("Neck",bottle,.28f,1,.72f,1.22f),new Color(.8f,.77f,.65f));I(R("Liquid",bottle,.12f,.12f,.88f,.55f),new Color(1,.35f,.4f));
        var pt=(TMP_Text)so.FindProperty("potionText").objectReferenceValue;pt.transform.SetParent(q,false);Place(pt.rectTransform,.31f,.07f,.98f,.46f);pt.font=font;pt.fontSize=22;pt.alignment=TextAlignmentOptions.Center;pt.raycastTarget=false;
        var notice=R("LevelUpNotice",canvas.transform,.37f,.70f,.63f,.80f);notice.SetSiblingIndex(panel.GetSiblingIndex()+1);I(notice,new Color(.03f,.12f,.13f,.88f));var group=notice.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;Ref(so,"levelNotice",group);Ref(so,"noticeText",T("Message",notice,"",27,0,0,1,1));notice.gameObject.SetActive(false);
        so.ApplyModifiedPropertiesWithoutUndo();
        var inv=new SerializedObject(canvas.GetComponent<InventoryUI>());var stats=(TMP_Text)inv.FindProperty("statsText").objectReferenceValue;stats.font=font;stats.fontSize=21;stats.enableAutoSizing=true;stats.fontSizeMin=15;stats.fontSizeMax=21;
        foreach(var graphic in panel.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);EditorSceneManager.SaveScene(canvas.gameObject.scene);return "Progression, status HUD and Q/E/R saved";
    }
}
#endif
