#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DungeonResultSetup
{
    static TMP_FontAsset font;
    static Color slate=new Color(.055f,.075f,.10f,.98f),gold=new Color(.65f,.50f,.24f),teal=new Color(.17f,.95f,.9f);
    static RectTransform R(string name,Transform parent,float x0,float y0,float x1,float y1)
    { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r; }
    static Image I(RectTransform r,Color c,bool ray=false){var i=r.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=ray;return i;}
    static TMP_Text T(string name,Transform p,string text,float size,float x0,float y0,float x1,float y1)
    {var t=R(name,p,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=Mathf.Min(size,16);return t;}
    static Button B(string name,Transform p,string text,float x0,float y0,float x1,float y1,Color c)
    {var r=R(name,p,x0,y0,x1,y1);var i=I(r,c,true);var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(1,-1);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;T("Label",r,text,26,.03f,.08f,.97f,.92f);return b;}
    static RectTransform Panel(string name,Transform p,float width,float height)
    {var r=R(name,p,.5f,.5f,.5f,.5f);r.sizeDelta=new Vector2(width,height);I(r,slate,true);var o=r.gameObject.AddComponent<Outline>();o.effectColor=gold;o.effectDistance=new Vector2(1,-1);return r;}
    static void Ref(SerializedObject so,string name,Object value){so.FindProperty(name).objectReferenceValue=value;}
    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode only");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        const string fontPath="Assets/UI/Result Korean.asset";
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(!font)
        {
            var chars=new HashSet<char>(Enumerable.Range(32,95).Select(i=>(char)i));
            var old=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Reward Korean.asset");foreach(var c in old.characterTable)chars.Add((char)c.unicode);
            foreach(var path in new[]{"Assets/Editor/DungeonResultSetup.cs","Assets/Scripts/DungeonResultUI.cs","Assets/Scripts/ResultItemCard.cs","Assets/Scripts/DungeonFloorData.cs"})foreach(char c in System.IO.File.ReadAllText(path))if(c>127)chars.Add(c);
            font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
            string missing;font.TryAddCharacters(new string(chars.ToArray()),out missing);font.name="Result Korean";font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,fontPath);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);AssetDatabase.AddObjectToAsset(font.material,font);
            var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();
        }
        var root=R("DungeonResultView",null,0,0,1,1);var v=root.gameObject.AddComponent<DungeonResultView>();
        var overlay=R("Overlay",root,0,0,1,1);I(overlay,new Color(8/255f,10/255f,15/255f,.8f),true);v.overlay=overlay.gameObject;v.overlayGroup=overlay.gameObject.AddComponent<CanvasGroup>();
        var content=R("Content1440x810",overlay,.5f,.5f,.5f,.5f);content.sizeDelta=new Vector2(1440,810);v.content=content;
        v.titleRoot=R("Title",content,.5f,.5f,.5f,.5f);v.titleRoot.sizeDelta=new Vector2(1300,170);v.titleRoot.anchoredPosition=new Vector2(0,315);
        v.eyebrow=T("Eyebrow",v.titleRoot,"CHAMBER CONQUERED",17,.12f,.81f,.88f,.98f);v.eyebrow.color=gold;
        v.title=T("Title",v.titleRoot,"던전 클리어",58,.13f,.37f,.87f,.82f);
        v.title.fontStyle=FontStyles.Bold;
        v.subtitle=T("Subtitle",v.titleRoot,"DUNGEON CLEAR",22,.2f,.2f,.8f,.38f);v.subtitle.color=teal;
        v.floor=T("Floor",v.titleRoot,"",22,.1f,.03f,.9f,.2f);
        v.failureNote=T("FailureNote",v.titleRoot,"",19,.08f,-.12f,.92f,.03f);
        I(R("Left decoration",v.titleRoot,.03f,.58f,.17f,.586f),gold);I(R("Right decoration",v.titleRoot,.83f,.58f,.97f,.586f),gold);
        v.closeButton=B("Close",content,"×",.955f,.93f,.995f,.995f,new Color(.2f,.1f,.1f));
        v.rewardsRoot=Panel("Rewards",content,660,570);v.rewardsRoot.anchoredPosition=new Vector2(-350,-100);v.rewardsGroup=v.rewardsRoot.gameObject.AddComponent<CanvasGroup>();
        T("Heading",v.rewardsRoot,"획득 보상",31,.05f,.88f,.95f,.98f).alignment=TextAlignmentOptions.MidlineLeft;
        v.itemCount=T("ItemCount",v.rewardsRoot,"",20,.05f,.82f,.95f,.88f);v.itemCount.alignment=TextAlignmentOptions.MidlineLeft;
        var scroll=R("ItemsScroll",v.rewardsRoot,.05f,.35f,.95f,.81f);I(scroll,new Color(.025f,.04f,.06f),true);v.scroll=scroll.gameObject.AddComponent<ScrollRect>();
        var viewport=R("Viewport",scroll,0,0,.965f,1);viewport.gameObject.AddComponent<RectMask2D>();v.scroll.viewport=viewport;
        var list=R("Items",viewport,0,1,1,1);list.pivot=new Vector2(.5f,1);v.itemContent=list;v.scroll.content=list;v.scroll.horizontal=false;v.scroll.movementType=ScrollRect.MovementType.Clamped;
        var grid=list.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=4;grid.cellSize=new Vector2(132,110);grid.spacing=new Vector2(9,10);grid.padding=new RectOffset(4,4,4,4);
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var track=R("Scrollbar",scroll,.975f,0,1,1);I(track,new Color(.12f,.15f,.18f));var bar=track.gameObject.AddComponent<Scrollbar>();var handle=I(R("Handle",track,0,0,1,1),gold,true);bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;v.scroll.verticalScrollbar=bar;
        var card=R("ItemTemplate",list,0,0,0,0);I(card,new Color(.08f,.11f,.15f),true);var cv=card.gameObject.AddComponent<ResultItemCard>();cv.border=card.GetComponent<Image>();I(R("Inside",card,.02f,.02f,.98f,.98f),slate);
        cv.icon=I(R("Icon",card,.2f,.32f,.8f,.87f),Color.white);cv.icon.preserveAspect=true;
        cv.marker=T("Marker",card,"",36,.15f,.32f,.85f,.87f);cv.label=T("Name",card,"",18,.04f,.04f,.96f,.26f);cv.label.overflowMode=TextOverflowModes.Ellipsis;
        cv.quantity=T("Quantity",card,"",18,.45f,.24f,.96f,.42f);cv.quantity.alignment=TextAlignmentOptions.MidlineRight;v.cardTemplate=cv;card.gameObject.SetActive(false);
        v.empty=T("Empty",scroll,"획득한 아이템이 없습니다.",22,.05f,.3f,.95f,.7f);
        v.gold=T("Gold",v.rewardsRoot,"",29,.05f,.235f,.95f,.32f);v.gold.color=new Color(1,.82f,.4f);v.gold.alignment=TextAlignmentOptions.MidlineLeft;
        v.experience=T("Experience",v.rewardsRoot,"",29,.05f,.145f,.95f,.23f);v.experience.color=teal;v.experience.alignment=TextAlignmentOptions.MidlineLeft;
        v.level=T("Level",v.rewardsRoot,"",20,.05f,.075f,.95f,.14f);v.level.alignment=TextAlignmentOptions.MidlineLeft;
        var xp=R("XPTrack",v.rewardsRoot,.05f,.045f,.95f,.065f);I(xp,new Color(.02f,.03f,.04f));v.experienceFill=I(R("Fill",xp,0,0,1,1),teal);
        v.recordsRoot=Panel("Records",content,660,570);v.recordsRoot.anchoredPosition=new Vector2(350,-100);v.recordsGroup=v.recordsRoot.gameObject.AddComponent<CanvasGroup>();
        T("Heading",v.recordsRoot,"전투 기록",31,.05f,.88f,.95f,.98f).alignment=TextAlignmentOptions.MidlineLeft;
        string[] labels={"클리어 시간 / CLEAR TIME","가한 피해량 / DAMAGE DEALT","받은 피해량 / DAMAGE TAKEN","회복량 / HP HEALED","처치한 몬스터","사용한 부활"};v.statistics=new TMP_Text[6];
        for(int i=0;i<6;i++){float y=.77f-i*.10f;var row=R("Stat"+i,v.recordsRoot,.05f,y,.95f,y+.095f);I(row,new Color(.035f,.05f,.07f));T("Index",row,(i+1).ToString("00"),17,.015f,.05f,.085f,.95f).color=gold;var label=T("Label",row,labels[i],20,.1f,.05f,.66f,.95f);label.alignment=TextAlignmentOptions.MidlineLeft;if(i==0)v.timeLabel=label;v.statistics[i]=T("Value",row,"",27,.66f,.05f,.97f,.95f);v.statistics[i].alignment=TextAlignmentOptions.MidlineRight;}
        T("Next",v.recordsRoot,"◆ 다음 행동을 선택하세요 ◆",19,.05f,.20f,.95f,.26f);
        v.retryButton=B("Retry",v.recordsRoot,"다시 시작\n<size=60%>RETRY</size>",.05f,.07f,.485f,.19f,new Color(.1f,.14f,.18f));
        var arrow=R("RetryArrow",v.retryButton.transform,.06f,.35f,.16f,.7f);
        for(int k=0;k<7;k++) {float a=(45+k*40)*Mathf.Deg2Rad;var line=R("Arc"+k,arrow,.5f,.5f,.5f,.5f);line.sizeDelta=new Vector2(3,10);line.anchoredPosition=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*10;line.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);I(line,gold);}
        var tip=R("Arrowhead",arrow,.65f,.8f,.65f,.8f);tip.sizeDelta=new Vector2(9,4);I(tip,gold);
        v.lobbyButton=B("Lobby",v.recordsRoot,"로비로 이동\n<size=60%>RETURN TO LOBBY</size>",.515f,.07f,.95f,.19f,new Color(.10f,.42f,.40f));
        v.status=T("SaveStatus",v.recordsRoot,"",18,.05f,.005f,.70f,.065f);
        v.saveRetryButton=B("RetrySave",v.recordsRoot,"저장 재시도",.70f,.005f,.95f,.06f,new Color(.25f,.15f,.1f));
        v.reopenButton=B("Reopen",root,"결과 보기",.84f,.035f,.975f,.10f,slate);v.reopen=v.reopenButton.gameObject;
        if(!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))AssetDatabase.CreateFolder("Assets/Prefabs","UI");
        var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/UI/DungeonResultView.prefab");Object.DestroyImmediate(root.gameObject);
        foreach(var path in new[]{"Assets/Scenes/Main.unity","Assets/Scenes/TestRoom.unity"})
        {
            EditorSceneManager.OpenScene(path);var ui=Object.FindFirstObjectByType<DungeonResultUI>();var canvas=ui.transform;
            var old=canvas.Find("ResultPanel");if(!old)old=canvas.Find("DungeonResultPanel");
            if(old)Object.DestroyImmediate(old.gameObject);
            var existing=canvas.GetComponentInChildren<DungeonResultView>(true);if(existing)Object.DestroyImmediate(existing.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas);var so=new SerializedObject(ui);Ref(so,"view",instance.GetComponent<DungeonResultView>());so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))if(text.text.Contains("클리어 상자 보상")){text.text=text.text.Replace("클리어 상자 보상","클리어 보상");EditorUtility.SetDirty(text);}
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveScene(ui.gameObject.scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);return "Shared result prefab and Main/TestRoom connections saved";
    }
}
#endif
