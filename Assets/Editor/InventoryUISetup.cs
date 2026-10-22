#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InventoryUISetup
{
    static TMP_FontAsset font;
    static Color bg = new Color(.055f,.075f,.10f,.98f), tile = new Color(.10f,.135f,.17f), gold = new Color(.84f,.66f,.32f);
    static RectTransform Rect(string name, Transform parent, float x0,float y0,float x1,float y1)
    {
        var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
        r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero; return r;
    }
    static Image Image(RectTransform r, Color color, bool ray=false)
    {var i=r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=ray;return i;}
    static TMP_Text Text(string name, Transform parent,string value,float size,float x0,float y0,float x1,float y1,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var t=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.89f,.92f,.94f);t.alignment=align;t.raycastTarget=false;return t;
    }
    static Button Button(string name,Transform parent,string label,float x0,float y0,float x1,float y1,float size=25)
    {
        var r=Rect(name,parent,x0,y0,x1,y1);var i=Image(r,tile,true);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;
        var colors=b.colors;colors.highlightedColor=new Color(.85f,.9f,1);colors.pressedColor=new Color(.65f,.75f,.9f);b.colors=colors;
        if(label!="")Text("Label",r,label,size,.03f,.04f,.97f,.96f);return b;
    }
    static Image Frame(string name,Transform parent,Color c,float thickness=2)
    {
        // Hollow border: sliced built-in UI sprite, with transparent center.
        var r=Rect(name,parent,0,0,1,1);var i=Image(r,c);i.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");i.type=UnityEngine.UI.Image.Type.Sliced;i.fillCenter=false;i.pixelsPerUnitMultiplier=1f/thickness;return i;
    }
    static void Ref(SerializedObject so,string name,Object value){so.FindProperty(name).objectReferenceValue=value;}
    static void Hover(GameObject go,InventoryUI ui,string note,int slot=-1)
    {var h=go.AddComponent<InventoryHover>();var so=new SerializedObject(h);Ref(so,"owner",ui);so.FindProperty("note").stringValue=note;so.FindProperty("equippedSlot").intValue=slot;so.ApplyModifiedPropertiesWithoutUndo();}
    static TMP_InputField Search(Transform parent)
    {
        var r=Rect("NameSearch",parent,.035f,.805f,.63f,.875f);Image(r,new Color(.04f,.06f,.08f),true);
        var input=r.gameObject.AddComponent<TMP_InputField>();var viewport=Rect("Text Area",r,.035f,.10f,.87f,.90f);viewport.gameObject.AddComponent<RectMask2D>();
        var text=Text("Text",viewport,"",25,0,0,1,1,TextAlignmentOptions.MidlineLeft);
        var placeholder=Text("Placeholder",viewport,"이름 검색",25,0,0,1,1,TextAlignmentOptions.MidlineLeft);placeholder.color=new Color(.45f,.53f,.61f);
        input.textViewport=viewport;input.textComponent=(TextMeshProUGUI)text;input.placeholder=placeholder;input.fontAsset=font;input.pointSize=25;input.lineType=TMP_InputField.LineType.SingleLine;input.characterLimit=80;input.restoreOriginalTextOnEscape=false;return input;
    }
    static TMP_Dropdown Sort(Transform parent)
    {
        var r=Rect("Sorting",parent,.65f,.805f,.965f,.875f);var image=Image(r,tile,true);var drop=r.gameObject.AddComponent<TMP_Dropdown>();drop.targetGraphic=image;
        var caption=Text("Caption",r,"최근 획득순",23,.04f,.08f,.86f,.92f);Text("Arrow",r,"v",20,.86f,0,1,1);drop.captionText=caption;
        var template=Rect("Template",r,0,0,1,0);template.pivot=new Vector2(.5f,1);template.anchoredPosition=new Vector2(0,-4);template.sizeDelta=new Vector2(0,260);Image(template,bg,true);
        var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Rect("Viewport",template,0,0,1,1);Image(viewport,new Color(0,0,0,0),true);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Content",viewport,0,1,1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,42);
        var item=Rect("Item",content,0,0,1,1);var toggle=item.gameObject.AddComponent<Toggle>();toggle.targetGraphic=Image(item,tile,true);
        var check=Text("Check",item,"•",24,.02f,0,.1f,1);toggle.graphic=check;drop.itemText=Text("Item Label",item,"최근 획득순",22,.12f,0,.97f,1);
        scroll.viewport=viewport;scroll.content=content;drop.template=template;template.gameObject.SetActive(false);return drop;
    }
    // Upgrade only the left side of the existing Main inventory; never rebuild owned items/loadout data.
    static Image GrowthFrame(string name, Transform parent, Color color) => Frame(name,parent,color,.4f);
    public static string ConnectGrowthTabs()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
            throw new System.Exception("Open Main in Edit mode");
        var ui = Object.FindFirstObjectByType<InventoryUI>();
        var so = new SerializedObject(ui);
        var panel = (GameObject)so.FindProperty("panel").objectReferenceValue;
        var left = (RectTransform)panel.transform.Find("InventoryLayout/Loadout");
        if (left.Find("CharacterInformation") != null) throw new System.Exception("Growth tabs already connected");
        var oldFont = ((TMP_Text)so.FindProperty("statsText").objectReferenceValue).font;
        var characters = new HashSet<char>(oldFont.characterTable.Select(c => (char)c.unicode));
        foreach (var file in new[]{"Assets/Scripts/Inventory/InventoryUI.cs", "Assets/Scripts/Inventory/InventoryPresentation.cs", "Assets/Editor/InventoryUISetup.cs"})
            foreach (char c in System.IO.File.ReadAllText(file)) if (!char.IsControl(c)) characters.Add(c);
        const string fontPath = "Assets/UI/Inventory Growth Korean.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) != null) throw new System.Exception("Growth font already exists");
        font = TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
        string missing;
        if (!font.TryAddCharacters(new string(characters.OrderBy(c=>c).ToArray()), out missing) || !string.IsNullOrEmpty(missing))
            throw new System.Exception("Growth font missing characters: " + missing);
        font.name="Inventory Growth Korean"; font.atlasPopulationMode=AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font,fontPath);
        foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture,font);
        AssetDatabase.AddObjectToAsset(font.material,font);
        var fs = new SerializedObject(font); fs.FindProperty("m_SourceFontFilePath").stringValue=""; fs.ApplyModifiedPropertiesWithoutUndo();

        var character = Rect("CharacterInformation",left,.035f,.29f,.965f,.815f);
        foreach (string name in new[]{"VisualOnlyPlayerPreview","무기","상의","하의","액세서리","SkillHeading","EquippedE","EquippedR","EquipInstruction","OwnedMoney"})
        {
            var child=(RectTransform)left.Find(name); if(child==null) continue;
            var min=child.anchorMin; var max=child.anchorMax;
            child.SetParent(character,false); child.anchorMin=new Vector2(min.x,(min.y-.24f)/.60f);child.anchorMax=new Vector2(max.x,(max.y-.24f)/.60f);
        }
        foreach(var text in character.GetComponentsInChildren<TMP_Text>(true)) text.font=font;
        foreach (string name in new[]{"무기","상의","하의","액세서리"})
        {
            var state=character.Find(name+"/State").GetComponent<TMP_Text>();state.fontSize=16;
            state.rectTransform.anchorMin=new Vector2(.01f,.10f);state.rectTransform.anchorMax=new Vector2(.99f,.34f);
            character.Find(name+"/Marker").GetComponent<TMP_Text>().fontSize=23;
        }
        foreach(string name in new[]{"EquippedE","EquippedR"})
        {
            var text=character.Find(name+"/Name").GetComponent<TMP_Text>();text.fontSize=19;
            text.rectTransform.anchorMin=new Vector2(.04f,.08f);text.rectTransform.anchorMax=new Vector2(.96f,.34f);
        }
        var title=left.Find("Title").GetComponent<TMP_Text>();title.text="캐릭터 관리";
        title.rectTransform.anchorMin=new Vector2(.05f,.905f);title.rectTransform.anchorMax=new Vector2(.70f,.965f);
        title.font=font;
        var subtitle=left.Find("Subtitle").GetComponent<TMP_Text>();subtitle.text="M I N E R   P R O F I L E";subtitle.fontSize=14;subtitle.font=font;
        subtitle.rectTransform.anchorMin=new Vector2(.05f,.965f);subtitle.rectTransform.anchorMax=new Vector2(.70f,.99f);
        Ref(so,"levelText",Text("CharacterLevel",left,"Lv. 1",26,.72f,.92f,.95f,.98f));
        var characterButton=Button("CharacterTab",left,"캐릭터 정보",.05f,.835f,.485f,.895f,24);
        var statsButton=Button("StatsTab",left,"스탯",.505f,.835f,.95f,.895f,24);
        GrowthFrame("Border",characterButton.transform,new Color(.28f,.32f,.34f));GrowthFrame("Border",statsButton.transform,gold);
        var dot=Rect("UnspentNotification",statsButton.transform,.91f,.75f,.91f,.75f);dot.sizeDelta=new Vector2(10,10);
        var dotImage=Image(dot,new Color(1,.80f,.34f));dotImage.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Ref(so,"statNotification",dot.gameObject);Ref(so,"characterTab",characterButton);Ref(so,"statTab",statsButton);Ref(so,"characterPage",character.gameObject);
        var stats=(RectTransform)left.Find("CurrentStats");stats.anchorMin=new Vector2(.055f,.025f);stats.anchorMax=new Vector2(.945f,.285f);
        var statText=(TMP_Text)so.FindProperty("statsText").objectReferenceValue;statText.font=font;statText.fontSize=20;statText.alignment=TextAlignmentOptions.TopLeft;
        statText.rectTransform.anchorMin=new Vector2(.05f,.04f);statText.rectTransform.anchorMax=new Vector2(.95f,.84f);
        statText.rectTransform.offsetMin=statText.rectTransform.offsetMax=Vector2.zero;
        Text("AttributesHeading",stats,"F I N A L   A T T R I B U T E S",13,.05f,.88f,.95f,.985f,TextAlignmentOptions.MidlineLeft).color=gold;
        GrowthFrame("Border",stats,new Color(.32f,.29f,.20f));

        var statPage=Rect("StatInvestment",left,.035f,.29f,.965f,.815f);Ref(so,"statPage",statPage.gameObject);
        Ref(so,"unspentText",Text("UnspentPoints",statPage,"미사용 3 P",24,.02f,.91f,.46f,.995f,TextAlignmentOptions.MidlineLeft));
        Ref(so,"investmentRule",Text("InvestmentRule",statPage,"메인 준비 상태 · 투자 즉시 저장",17,.02f,.845f,.98f,.90f,TextAlignmentOptions.MidlineLeft));
        var multipliers=so.FindProperty("multiplierButtons");multipliers.arraySize=3;
        for(int i=0;i<3;i++) {float x=.51f+i*.16f;var button=Button("Multiplier"+(i==0?1:i==1?5:10),statPage,i==0?"×1":i==1?"×5":"×10",x,.915f,x+.145f,.99f,20);multipliers.GetArrayElementAtIndex(i).objectReferenceValue=button;GrowthFrame("Border",button.transform,new Color(.27f,.36f,.38f));}
        var scrollRoot=Rect("GrowthScroll",statPage,0,0,1,.82f);var growthScroll=scrollRoot.gameObject.AddComponent<ScrollRect>();growthScroll.horizontal=false;growthScroll.movementType=ScrollRect.MovementType.Clamped;growthScroll.scrollSensitivity=35;Ref(so,"growthScroll",growthScroll);
        var viewport=Rect("Viewport",scrollRoot,0,0,.975f,1);Image(viewport,new Color(.04f,.06f,.08f),true);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Content",viewport,0,1,1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(3,8,4,4);layout.spacing=12;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;growthScroll.viewport=viewport;growthScroll.content=content;
        var sb=Rect("Scrollbar",scrollRoot,.982f,0,1,1);Image(sb,new Color(.08f,.1f,.13f));var scrollbar=sb.gameObject.AddComponent<Scrollbar>();
        var sliding=Rect("SlidingArea",sb,0,0,1,1);var handle=Image(Rect("Handle",sliding,0,0,1,1),new Color(.34f,.47f,.49f),true);scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;growthScroll.verticalScrollbar=scrollbar;
        var statArray=so.FindProperty("statCards");statArray.arraySize=3;var passives=so.FindProperty("passiveCards");passives.arraySize=9;
        string[] names={"체력  VITALITY","물리  PHYSICAL","마법  ARCANE"};string[] bonuses={"투자 1P당 최대 HP +3","투자 1P당 물리 공격 스탯 +0.4","투자 1P당 마법 공격 스탯 +0.4"};
        Color[] accents={new Color(.57f,.76f,.37f),new Color(.96f,.55f,.27f),new Color(.15f,.85f,.87f)};
        for(int stat=0;stat<3;stat++)
        {
            var card=Rect("StatCard"+stat,content,0,0,1,1);Image(card,tile);card.gameObject.AddComponent<LayoutElement>().preferredHeight=218;
            Image(Rect("Accent",card,0,0,.008f,1),accents[stat]);GrowthFrame("Frame",card,new Color(.27f,.30f,.31f));
            Text("Name",card,names[stat],24,.035f,.77f,.51f,.97f,TextAlignmentOptions.MidlineLeft).color=accents[stat];
            Text("Bonus",card,bonuses[stat],17,.035f,.64f,.96f,.79f,TextAlignmentOptions.MidlineLeft).color=new Color(.62f,.68f,.73f);
            var data=statArray.GetArrayElementAtIndex(stat);
            data.FindPropertyRelative("points").objectReferenceValue=Text("Investment",card,"00 / 100P",27,.51f,.77f,.81f,.97f,TextAlignmentOptions.MidlineRight);
            var plus=Button("Invest",card,"",.845f,.785f,.975f,.96f);data.FindPropertyRelative("invest").objectReferenceValue=plus;
            data.FindPropertyRelative("amount").objectReferenceValue=Text("Amount",plus.transform,"+1",23,0,0,1,1);GrowthFrame("Border",plus.transform,new Color(.15f,.80f,.82f));
            for(int stage=0;stage<3;stage++)
            {
                int index=stat*3+stage;float bottom=.43f-stage*.20f;var row=Rect("Passive"+(stage==0?20:stage==1?50:100),card,.025f,bottom,.975f,bottom+.185f);
                var background=Image(row,new Color(.075f,.10f,.13f),true);var dataPassive=passives.GetArrayElementAtIndex(index);dataPassive.FindPropertyRelative("background").objectReferenceValue=background;
                Text("Name",row,InventoryPresentation.PassiveTitles[index],18,.025f,0,.70f,1,TextAlignmentOptions.MidlineLeft);
                dataPassive.FindPropertyRelative("state").objectReferenceValue=Text("State",row,"20P · 미해금",17,.715f,0,.98f,1,TextAlignmentOptions.MidlineRight);
                Hover(row.gameObject,ui,InventoryPresentation.PassiveDetails[index]);
            }
        }
        ((TMP_Text)so.FindProperty("tooltipText").objectReferenceValue).font=font;
        var notice=(TMP_Text)so.FindProperty("noticeText").objectReferenceValue;notice.font=font;
        character.gameObject.SetActive(true);statPage.gameObject.SetActive(false);so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return "Existing Main inventory left tabs/growth cards/common attributes connected; right side retained.";
    }

    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode required");
        var canvas=Object.FindFirstObjectByType<Canvas>();if(canvas.GetComponent<InventoryUI>()!=null)throw new System.Exception("Inventory UI already exists");
        if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Inventory"))AssetDatabase.CreateFolder("Assets/Prefabs","Inventory");
        var characters=new HashSet<char>(Enumerable.Range(32,95).Select(i=>(char)i));
        foreach(var file in System.IO.Directory.GetFiles("Assets/Scripts/Inventory","*.cs"))foreach(var c in System.IO.File.ReadAllText(file))if(c>127)characters.Add(c);
        foreach(var c in System.IO.File.ReadAllText("Assets/Editor/InventoryUISetup.cs"))if(c>127)characters.Add(c);
        foreach(var s in AssetDatabase.FindAssets("t:SkillData").Select(g=>AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))))foreach(var c in s.displayName)characters.Add(c);
        font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
        string missing;font.TryAddCharacters(new string(characters.ToArray()),out missing);if(!string.IsNullOrEmpty(missing))throw new System.Exception("Missing glyphs: "+missing);
        font.name="Inventory Korean";font.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(font,"Assets/UI/Inventory Korean.asset");foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);AssetDatabase.AddObjectToAsset(font.material,font);var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();
        var player=Object.FindFirstObjectByType<PlayerSkills>();var run=Object.FindFirstObjectByType<DungeonRunManager>();var session=run.gameObject.AddComponent<SkillInventorySession>();var ss=new SerializedObject(session);var seeds=ss.FindProperty("testStartingSkills");seeds.arraySize=4;
        var names=new[]{"EarthSmash","DrillDash","TealBeam","OreBlast"};for(int i=0;i<4;i++)seeds.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skills/"+names[i]+".asset");ss.ApplyModifiedPropertiesWithoutUndo();
        var ui=canvas.gameObject.AddComponent<InventoryUI>();var so=new SerializedObject(ui);
        Ref(so,"player",player);Ref(so,"rooms",Object.FindFirstObjectByType<DungeonRoomManager>());Ref(so,"pauseMenu",Object.FindFirstObjectByType<PauseMenu>());Ref(so,"canvasRect",canvas.transform);
        var panel=Rect("InventoryPanel",canvas.transform,0,0,1,1);Image(panel,new Color(.025f,.035f,.05f,.97f),true);Ref(so,"panel",panel.gameObject);
        var body=Rect("InventoryLayout",panel,.025f,.04f,.975f,.96f);
        var left=Rect("Loadout",body,0,0,.395f,1);Image(left,bg,true);Frame("Border",left,new Color(.29f,.29f,.24f));
        var right=Rect("OwnedItems",body,.405f,0,1,1);Image(right,bg,true);Frame("Border",right,new Color(.29f,.29f,.24f));
        Text("Title",left,"인벤토리",42,.05f,.91f,.95f,.99f);
        Text("Subtitle",left,"준비하고, 광산으로",21,.05f,.865f,.95f,.915f).color=gold;
        var preview=Rect("VisualOnlyPlayerPreview",left,.32f,.52f,.68f,.84f);Image(preview,new Color(.08f,.11f,.14f));
        var playerSprite=player.GetComponent<SpriteRenderer>();var pi=Image(Rect("PlayerSprite",preview,.2f,.22f,.8f,.76f),playerSprite.color);pi.sprite=playerSprite.sprite;pi.preserveAspect=true;
        Text("PreviewCaption",preview,"현재 플레이어",20,.03f,.03f,.97f,.18f);
        var equipment=new[]{"무기","상의","하의","액세서리"};var marks=new[]{"곡괭이","상의","하의","장신구"};
        for(int i=0;i<4;i++) {bool l=i==0||i==3;bool top=i<2;var slot=Button(equipment[i],left,"",l?.055f:.75f,top?.69f:.51f,l?.25f:.945f,top?.84f:.66f);Frame("Frame",slot.transform,new Color(.40f,.43f,.47f));Text("Marker",slot.transform,marks[i],25,.03f,.25f,.97f,.85f);Text("State",slot.transform,i==0?"임시 표식":"준비 중",18,.01f,.04f,.99f,.27f);Hover(slot.gameObject,ui,i==0?"곡괭이\n현재 무기 표식입니다.\n무기 데이터와 장착 기능은 준비 중입니다.":equipment[i]+"\n준비 중");}
        Text("SkillHeading",left,"E / R 스킬 장착",23,.05f,.435f,.95f,.485f);
        for(int i=0;i<2;i++)
        {
            var b=Button(i==0?"EquippedE":"EquippedR",left,"",i==0?.08f:.53f,.285f,i==0?.47f:.92f,.43f);var prop=so.FindProperty(i==0?"eSlot":"rSlot");prop.FindPropertyRelative("button").objectReferenceValue=b;
            Frame("Border",b.transform,new Color(.44f,.48f,.52f));var hi=Frame("SelectDestination",b.transform,new Color(.25f,1,.85f),1.3f);hi.enabled=false;prop.FindPropertyRelative("highlight").objectReferenceValue=hi;
            Text("Key",b.transform,i==0?"E":"R",26,.04f,.65f,.23f,.96f).color=gold;
            var icon=Image(Rect("Icon",b.transform,.36f,.30f,.64f,.87f),Color.white);icon.preserveAspect=true;icon.enabled=false;prop.FindPropertyRelative("icon").objectReferenceValue=icon;
            prop.FindPropertyRelative("marker").objectReferenceValue=Text("Marker",b.transform,i==0?"I":"III",35,.25f,.3f,.75f,.87f);
            var label=Text("Name",b.transform,i==0?"대지 분쇄":"청록 광선",21,.04f,.03f,.96f,.30f);label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;prop.FindPropertyRelative("name").objectReferenceValue=label;Hover(b.gameObject,ui,"비어 있음",i);
        }
        Ref(so,"restrictionText",Text("EquipInstruction",left,"스킬을 선택한 뒤 E/R 슬롯을 클릭하세요",18,.025f,.24f,.975f,.28f));
        var stats=Rect("CurrentStats",left,.08f,.075f,.92f,.23f);Image(stats,new Color(.04f,.06f,.08f));Ref(so,"statsText",Text("Stats",stats,"",23,.07f,.04f,.95f,.96f,TextAlignmentOptions.MidlineLeft));
        var tabArray=so.FindProperty("tabs");tabArray.arraySize=7;var symbols=new[]{"≡","무","상","하","장","스","재"};
        for(int i=0;i<7;i++){float x=.035f+i*.115f;var b=Button("Tab_"+i,right,symbols[i],x,.90f,x+.10f,.975f,27);var tp=tabArray.GetArrayElementAtIndex(i);tp.FindPropertyRelative("button").objectReferenceValue=b;tp.FindPropertyRelative("background").objectReferenceValue=b.GetComponent<Image>();tp.FindPropertyRelative("border").objectReferenceValue=Frame("SelectedTab",b.transform,gold);Hover(b.gameObject,ui,InventoryPresentation.Categories[i]);}
        var close=Button("Close",right,"×",.89f,.90f,.965f,.975f,32);close.GetComponent<Image>().color=new Color(.32f,.11f,.09f);Ref(so,"closeButton",close);
        var search=Search(right);Ref(so,"search",search);Ref(so,"clearSearchButton",Button("ClearSearch",search.transform,"×",.88f,.08f,.98f,.92f,23));Ref(so,"sorting",Sort(right));
        Ref(so,"listTitle",Text("ListTitle",right,"전체 아이템",26,.035f,.74f,.50f,.795f,TextAlignmentOptions.MidlineLeft));Ref(so,"countText",Text("Count",right,"",19,.48f,.74f,.965f,.795f,TextAlignmentOptions.MidlineRight));
        var scrollRoot=Rect("ItemScroll",right,.035f,.06f,.965f,.725f);var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        var viewport=Rect("Viewport",scrollRoot,0,0,.97f,1);Image(viewport,new Color(.035f,.05f,.07f,.8f),true);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("Content",viewport,0,1,1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.padding=new RectOffset(10,10,10,10);grid.spacing=new Vector2(14,14);grid.cellSize=new Vector2(200,225);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=4;var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport;scroll.content=content;Ref(so,"scroll",scroll);Ref(so,"grid",grid);
        var sb=Rect("Scrollbar",scrollRoot,.98f,0,1,1);Image(sb,new Color(.08f,.1f,.13f));var scrollbar=sb.gameObject.AddComponent<Scrollbar>();var sliding=Rect("Sliding Area",sb,0,0,1,1);var handle=Image(Rect("Handle",sliding,0,0,1,1),new Color(.35f,.40f,.45f),true);scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;
        Ref(so,"emptyText",Text("EmptyMessage",viewport,"",28,.05f,.35f,.95f,.65f));
        var card=Rect("InventorySkillCard",null,0,0,0,0);card.sizeDelta=new Vector2(200,225);Image(card,tile,true);var cb=card.gameObject.AddComponent<Button>();cb.targetGraphic=card.GetComponent<Image>();var view=card.gameObject.AddComponent<InventorySkillCard>();var cs=new SerializedObject(view);Ref(cs,"button",cb);Ref(cs,"rarityBorder",Frame("RarityBorder",card,InventoryPresentation.RarityColor(ItemRarity.Common)));var sf=Frame("SelectedFrame",Rect("SelectionInset",card,.025f,.025f,.975f,.975f),new Color(.25f,1,.85f),1.3f);sf.gameObject.SetActive(false);Ref(cs,"selectedFrame",sf);
        var ci=Image(Rect("Icon",card,.23f,.28f,.77f,.84f),Color.white);ci.preserveAspect=true;Ref(cs,"icon",ci);Ref(cs,"marker",Text("Marker",card,"I",62,.15f,.28f,.85f,.85f));
        var cn=Text("Name",card,"스킬",23,.06f,.05f,.94f,.23f);cn.textWrappingMode=TextWrappingModes.NoWrap;cn.overflowMode=TextOverflowModes.Ellipsis;Ref(cs,"nameText",cn);Ref(cs,"badge",Text("EquippedBadge",card,"",24,.055f,.81f,.25f,.97f));var ch=card.gameObject.AddComponent<InventoryHover>();Ref(cs,"hover",ch);cs.ApplyModifiedPropertiesWithoutUndo();var prefab=PrefabUtility.SaveAsPrefabAsset(card.gameObject,"Assets/Prefabs/Inventory/InventorySkillCard.prefab");Object.DestroyImmediate(card.gameObject);Ref(so,"cardPrefab",prefab.GetComponent<InventorySkillCard>());
        var tip=Rect("InventoryTooltip",canvas.transform,.5f,.5f,.5f,.5f);tip.pivot=new Vector2(0,1);tip.sizeDelta=new Vector2(450,500);Image(tip,new Color(.055f,.075f,.10f,1));Frame("Border",tip,gold);var group=tip.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;Ref(so,"tooltip",tip);var tiptext=Text("Text",tip,"",23,0,0,1,1,TextAlignmentOptions.TopLeft);tiptext.rectTransform.offsetMin=new Vector2(20,18);tiptext.rectTransform.offsetMax=new Vector2(-20,-18);Ref(so,"tooltipText",tiptext);tip.gameObject.SetActive(false);
        Ref(so,"noticeText",Text("InventoryNotice",canvas.transform,"",24,.15f,.16f,.85f,.22f));so.ApplyModifiedPropertiesWithoutUndo();panel.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Inventory scene UI, card prefab, session seeds, Korean font saved.";
    }
}
#endif

