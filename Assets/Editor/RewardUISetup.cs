#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RewardUISetup
{
    static TMP_FontAsset font;
    static RectTransform R(string name,Transform p,float x0,float y0,float x1,float y1)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static Image I(RectTransform r,Color color){var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static TMP_Text T(string name,Transform p,string text,float size,float x0,float y0,float x1,float y1)
    {var t=R(name,p,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.font=font;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static void Ref(SerializedObject so,string name,Object value){so.FindProperty(name).objectReferenceValue=value;}
    static MonsterRewardData Reward(string path,int money,int xp,MaterialData stone,int min,int max)
    {var d=AssetDatabase.LoadAssetAtPath<MonsterRewardData>(path);if(d!=null)return d;d=ScriptableObject.CreateInstance<MonsterRewardData>();d.money=money;d.experience=xp;d.materials=new[]{new MonsterRewardData.Drop{material=stone,chancePercent=100,minimum=min,maximum=max}};AssetDatabase.CreateAsset(d,path);return d;}
    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode required");
        var canvas=Object.FindFirstObjectByType<Canvas>();if(canvas.GetComponent<RewardHUD>()!=null)throw new System.Exception("Already connected");
        if(!AssetDatabase.IsValidFolder("Assets/Rewards"))AssetDatabase.CreateFolder("Assets","Rewards");
        var normalPath="Assets/Prefabs/Rooms/Room_Normal_01.prefab";
        var normalRoot=AssetDatabase.LoadAssetAtPath<GameObject>(normalPath);
        var sprite=normalRoot.GetComponentInChildren<EnemyHealth>(true).GetComponentInChildren<SpriteRenderer>(true).sprite;
        var stone=AssetDatabase.LoadAssetAtPath<MaterialData>("Assets/Rewards/Stone.asset");if(stone==null){stone=ScriptableObject.CreateInstance<MaterialData>();stone.id="Stone";stone.displayName="돌";stone.description="광산의 돌 몬스터에서 얻은 돌 조각입니다.";stone.icon=sprite;stone.rarity=ItemRarity.Common;AssetDatabase.CreateAsset(stone,"Assets/Rewards/Stone.asset");}
        var normal=Reward("Assets/Rewards/StoneMonsterReward.asset",10,10,stone,1,2);var boss=Reward("Assets/Rewards/StoneGolemReward.asset",50,50,stone,5,5);
        int wired=0;
        foreach(var path in new[]{normalPath,"Assets/Prefabs/Rooms/Room_Normal_02 Variant.prefab","Assets/Prefabs/Rooms/Room_Boss_Test Variant.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);try{
                foreach(var enemy in root.GetComponentsInChildren<EnemyHealth>(true)){
                    var reward=enemy.GetComponent<EnemyReward>();if(reward==null)reward=enemy.gameObject.AddComponent<EnemyReward>();var so=new SerializedObject(reward);var field=so.FindProperty("reward");
                    bool isBoss=enemy.GetComponent<StoneGolemBossAI>()!=null;
                    if(field.objectReferenceValue==null||(isBoss&&field.objectReferenceValue==normal)){field.objectReferenceValue=isBoss?boss:normal;so.ApplyModifiedPropertiesWithoutUndo();wired++;}
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var chars=new HashSet<char>(Enumerable.Range(32,95).Select(x=>(char)x));
        var old=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Inventory Korean.asset");foreach(var c in old.characterTable)chars.Add((char)c.unicode);
        foreach(var path in new[]{"Assets/Scripts/Inventory/InventoryUI.cs","Assets/Scripts/Inventory/InventoryPresentation.cs","Assets/Scripts/Inventory/RewardHUD.cs","Assets/Scripts/DungeonResultUI.cs","Assets/Editor/RewardUISetup.cs"})foreach(char c in System.IO.File.ReadAllText(path))if(c>127)chars.Add(c);
        font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);string missing;font.TryAddCharacters(new string(chars.ToArray()),out missing);if(!string.IsNullOrEmpty(missing))throw new System.Exception(missing);
        font.name="Reward Korean";font.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(font,"Assets/UI/Reward Korean.asset");foreach(var a in font.atlasTextures)AssetDatabase.AddObjectToAsset(a,font);AssetDatabase.AddObjectToAsset(font.material,font);var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();
        var run=Object.FindFirstObjectByType<DungeonRunManager>();if(run.GetComponent<RewardInventorySession>()==null)run.gameObject.AddComponent<RewardInventorySession>();
        var inv=canvas.transform.Find("InventoryPanel");foreach(var text in inv.GetComponentsInChildren<TMP_Text>(true)){text.font=font;EditorUtility.SetDirty(text);}
        canvas.transform.Find("InventoryTooltip").GetComponentInChildren<TMP_Text>(true).font=font;
        string cardPath="Assets/Prefabs/Inventory/InventorySkillCard.prefab";var card=PrefabUtility.LoadPrefabContents(cardPath);try{
            foreach(var text in card.GetComponentsInChildren<TMP_Text>(true))text.font=font;
            var q=T("Quantity",card.transform,"",24,.48f,.80f,.96f,.98f);q.alignment=TextAlignmentOptions.MidlineRight;q.enableAutoSizing=true;q.fontSizeMin=12;q.fontSizeMax=24;
            var cs=new SerializedObject(card.GetComponent<InventorySkillCard>());Ref(cs,"quantityText",q);cs.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(card,cardPath);
        }finally{PrefabUtility.UnloadPrefabContents(card);}
        var rh=canvas.gameObject.AddComponent<RewardHUD>();var hs=new SerializedObject(rh);
        var hudMoney=T("MoneyHUD",canvas.transform,"돈 0",23,.018f,.735f,.25f,.77f);hudMoney.alignment=TextAlignmentOptions.MidlineLeft;hudMoney.transform.SetSiblingIndex(canvas.transform.Find("PlayerStatusPanel").GetSiblingIndex()+1);Ref(hs,"hudMoney",hudMoney);
        var left=inv.Find("InventoryLayout/Loadout");Ref(hs,"inventoryMoney",T("OwnedMoney",left,"보유 돈: 0",20,.07f,.485f,.93f,.515f));
        var list=R("MaterialAcquisitionNotifications",canvas.transform,.76f,.29f,.975f,.64f);list.SetSiblingIndex(hudMoney.transform.GetSiblingIndex()+1);
        var rows=hs.FindProperty("rows");rows.arraySize=5;
        for(int i=0;i<5;i++){
            var row=R("Acquisition"+i,list,0,1,1,1);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,58);I(row,new Color(.035f,.07f,.085f,.9f));var group=row.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            var icon=I(R("Icon",row,.03f,.16f,.16f,.84f),new Color(.75f,.8f,.82f));icon.preserveAspect=true;var label=T("AcquiredMaterial",row,"",23,.20f,.08f,.96f,.92f);label.alignment=TextAlignmentOptions.MidlineLeft;
            var p=rows.GetArrayElementAtIndex(i);p.FindPropertyRelative("rect").objectReferenceValue=row;p.FindPropertyRelative("group").objectReferenceValue=group;p.FindPropertyRelative("icon").objectReferenceValue=icon;p.FindPropertyRelative("label").objectReferenceValue=label;row.gameObject.SetActive(false);
        }
        hs.ApplyModifiedPropertiesWithoutUndo();
        var result=canvas.GetComponent<DungeonResultUI>();var rs=new SerializedObject(result);var panel=(GameObject)rs.FindProperty("resultPanel").objectReferenceValue;var rc=(RectTransform)panel.transform.Find("ResultCard");rc.sizeDelta=new Vector2(580,510);
        ((RectTransform)rc.Find("ResultTitle")).anchoredPosition=new Vector2(0,180);((RectTransform)rc.Find("FinalRoom")).anchoredPosition=new Vector2(0,105);((RectTransform)rc.Find("UsedRevives")).anchoredPosition=new Vector2(0,55);((RectTransform)rc.Find("RetryButton")).anchoredPosition=new Vector2(0,-175);
        Ref(rs,"rewardSummary",T("RewardSummary",rc,"",25,.05f,.28f,.95f,.51f));rs.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);EditorSceneManager.SaveScene(canvas.gameObject.scene);return "Saved reward settings, cards, notifications and summary; assignments="+wired;
    }
}
#endif
