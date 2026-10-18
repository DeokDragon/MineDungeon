#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MineEntranceSetup
{
    static TMP_FontAsset font;
    static RectTransform R(string n,Transform p,float x,float y,float x1,float y1){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static Image I(RectTransform r,Color c,bool ray=false){var i=r.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=ray;return i;}
    static TMP_Text T(string n,Transform p,string value,int size,float x,float y,float x1,float y1){var t=R(n,p,x,y,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.text=value;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static void Border(Transform p,string name,Color c){var i=I(R(name,p,0,0,1,1),c);i.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");i.type=Image.Type.Sliced;i.fillCenter=false;}
    static Button B(string n,Transform p,string label,float x,float y,float x1,float y1){var r=R(n,p,x,y,x1,y1);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=I(r,new Color(.1f,.13f,.17f),true);if(label!="")T("Label",r,label,25,.03f,.03f,.97f,.97f);return b;}
    static void Ref(SerializedObject so,string n,Object v){so.FindProperty(n).objectReferenceValue=v;}
    static GameObject Shape(string n,Transform p,Sprite sprite,Vector2 pos,Vector2 size,Color c,int order,bool wall=false){var go=new GameObject(n);go.transform.SetParent(p,false);go.transform.position=pos;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=c;sr.sortingOrder=order;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);if(wall){go.layer=LayerMask.NameToLayer("Wall");go.AddComponent<BoxCollider2D>();}return go;}
    public static string Build()
    {
        if(Application.isPlaying)throw new System.Exception("Edit mode only");var canvas=Object.FindFirstObjectByType<Canvas>();if(canvas.GetComponent<MineEntranceUI>()!=null)throw new System.Exception("Already built");
        if(!AssetDatabase.IsValidFolder("Assets/Dungeons"))AssetDatabase.CreateFolder("Assets","Dungeons");
        var characters=new HashSet<char>(Enumerable.Range(32,95).Select(c=>(char)c));foreach(var c in AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Reward Korean.asset").characterTable)characters.Add((char)c.unicode);
        foreach(var file in new[]{"Assets/Scripts/DungeonFloorData.cs","Assets/Scripts/MineEntranceUI.cs","Assets/Scripts/ChestRewardPreviewData.cs","Assets/Editor/MineEntranceSetup.cs"})foreach(char c in System.IO.File.ReadAllText(file))if(c>127)characters.Add(c);
        font=TMP_FontAsset.CreateFontAsset("C:/Windows/Fonts/NotoSansKR-VF.ttf",0,64,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);string missing;font.TryAddCharacters(new string(characters.ToArray()),out missing);if(!string.IsNullOrEmpty(missing))throw new System.Exception(missing);
        font.name="Mine Entrance Korean";font.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(font,"Assets/UI/Mine Entrance Korean.asset");foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);AssetDatabase.AddObjectToAsset(font.material,font);var fs=new SerializedObject(font);fs.FindProperty("m_SourceFontFilePath").stringValue="";fs.ApplyModifiedPropertiesWithoutUndo();
        var roomManager=Object.FindFirstObjectByType<DungeonRoomManager>();var rm=new SerializedObject(roomManager);var player=Object.FindFirstObjectByType<PlayerHealth>();var inventory=canvas.GetComponent<InventoryUI>();
        var square=player.GetComponent<SpriteRenderer>().sprite;var boss=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rooms/Room_Boss_Test Variant.prefab").GetComponentInChildren<EnemyHealth>(true).GetComponentInChildren<SpriteRenderer>(true).sprite;
        var floor1=ScriptableObject.CreateInstance<DungeonFloorData>();floor1.id="RockMine01";floor1.floorNumber=1;floor1.displayName="암석 갱도";floor1.previewImage=boss;
        var floor2=ScriptableObject.CreateInstance<DungeonFloorData>();floor2.id="FutureMine02";floor2.floorNumber=2;floor2.displayName="추가 갱도";floor2.previewImage=square;
        for(int i=0;i<4;i++){
            var table=ScriptableObject.CreateInstance<ChestRewardPreviewData>();table.entries=new[]{new ChestRewardPreviewData.Entry{skill=AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skills/EarthSmash.asset"),displayOrder=0,chancePercent=10+10*i},new ChestRewardPreviewData.Entry{material=AssetDatabase.LoadAssetAtPath<MaterialData>("Assets/Rewards/Stone.asset"),displayOrder=10,chancePercent=100,minimum=1,maximum=3}};AssetDatabase.CreateAsset(table,"Assets/Dungeons/Floor1ChestPreview_"+i+".asset");
            floor1.difficulties[i]=new DungeonFloorData.Difficulty{requiredLevel=new[]{1,2,3,5}[i],implemented=i==0,chestPreview=table};floor2.difficulties[i]=new DungeonFloorData.Difficulty{requiredLevel=new[]{2,3,4,5}[i]};
        }
        var originals=rm.FindProperty("normalRoomPrefabs");floor1.difficulties[0].normalRooms=new DungeonRoom[originals.arraySize];for(int i=0;i<originals.arraySize;i++)floor1.difficulties[0].normalRooms[i]=(DungeonRoom)originals.GetArrayElementAtIndex(i).objectReferenceValue;
        floor1.difficulties[0].bossRoom=(DungeonRoom)rm.FindProperty("bossRoomPrefab").objectReferenceValue;floor1.difficulties[0].normalRoomCount=rm.FindProperty("normalRoomCount").intValue;
        AssetDatabase.CreateAsset(floor1,"Assets/Dungeons/Floor1_RockMine.asset");AssetDatabase.CreateAsset(floor2,"Assets/Dungeons/Floor2_Preview.asset");
        var lobby=new GameObject("MainMineEntrance");Shape("MainGround",lobby.transform,square,Vector2.zero,new Vector2(30,20),new Color(.15f,.18f,.18f),-20);
        Shape("NorthWall",lobby.transform,square,new Vector2(0,9),new Vector2(30,1),new Color(.10f,.13f,.14f),-10,true);Shape("SouthWall",lobby.transform,square,new Vector2(0,-9),new Vector2(30,1),new Color(.10f,.13f,.14f),-10,true);Shape("WestWall",lobby.transform,square,new Vector2(-14,0),new Vector2(1,18),new Color(.10f,.13f,.14f),-10,true);Shape("EastWall",lobby.transform,square,new Vector2(14,0),new Vector2(1,18),new Color(.10f,.13f,.14f),-10,true);
        var entrance=new GameObject("MineInteractionPoint");entrance.transform.SetParent(lobby.transform);entrance.transform.position=new Vector3(4,2,0);
        Shape("MineOpening",lobby.transform,square,new Vector2(4,3),new Vector2(4,3),new Color(.025f,.035f,.04f),-5);Shape("LeftBeam",lobby.transform,square,new Vector2(1.8f,3),new Vector2(.35f,3.5f),new Color(.40f,.29f,.18f),-4);Shape("RightBeam",lobby.transform,square,new Vector2(6.2f,3),new Vector2(.35f,3.5f),new Color(.40f,.29f,.18f),-4);Shape("TopBeam",lobby.transform,square,new Vector2(4,4.7f),new Vector2(4.8f,.4f),new Color(.40f,.29f,.18f),-4);
        var marker=GameObject.Find("TestMarker");if(marker!=null)Object.DestroyImmediate(marker);player.transform.position=Vector3.zero;
        var cf=Object.FindFirstObjectByType<CameraFollow>();var cs=new SerializedObject(cf);cs.FindProperty("mapMin").vector2Value=new Vector2(-15,-10);cs.FindProperty("mapMax").vector2Value=new Vector2(15,10);cs.ApplyModifiedPropertiesWithoutUndo();
        var oldPrep=canvas.transform.Find("PreparationPanel");if(oldPrep!=null)Object.DestroyImmediate(oldPrep.gameObject);var oldStart=canvas.transform.Find("InventoryPanel/InventoryLayout/Loadout/StartDungeon");if(oldStart!=null)Object.DestroyImmediate(oldStart.gameObject);
        var ui=canvas.gameObject.AddComponent<MineEntranceUI>();var so=new SerializedObject(ui);Ref(so,"entrance",entrance.transform);Ref(so,"lobbyRoot",lobby);Ref(so,"player",player);Ref(so,"rooms",roomManager);Ref(so,"inventory",inventory);Ref(so,"pauseMenu",canvas.GetComponent<PauseMenu>());
        var floors=so.FindProperty("floors");floors.arraySize=2;floors.GetArrayElementAtIndex(0).objectReferenceValue=floor1;floors.GetArrayElementAtIndex(1).objectReferenceValue=floor2;
        Ref(so,"prompt",T("MineInteractionPrompt",canvas.transform,"",26,.35f,.15f,.65f,.20f));
        var panel=R("MineSelectionPanel",canvas.transform,.025f,.04f,.975f,.96f);I(panel,new Color(.06f,.075f,.09f,1),true);Border(panel,"Border",new Color(.40f,.40f,.32f));Ref(so,"panel",panel.gameObject);
        T("Title",panel,"광산 입장",34,.02f,.89f,.32f,.98f);var left=R("Floors",panel,.025f,.10f,.33f,.87f);var fbuttons=so.FindProperty("floorButtons");fbuttons.arraySize=2;
        for(int i=0;i<2;i++){var b=B("Floor"+(i+1),left,"",.02f,.71f-i*.29f,.98f,.98f-i*.29f);T("Label",b.transform,"",27,.04f,.08f,.96f,.92f);Border(b.transform,"SelectionBorder",new Color(.15f,.85f,.76f));fbuttons.GetArrayElementAtIndex(i).objectReferenceValue=b;}
        Ref(so,"title",T("SelectedTitle",panel,"",32,.36f,.89f,.92f,.97f));Ref(so,"closeButton",B("Close",panel,"×",.94f,.91f,.99f,.985f));
        var art=R("RepresentativeArea",panel,.36f,.55f,.97f,.87f);I(art,new Color(.065f,.10f,.12f));var artImage=I(R("ReplaceableImage",art,.24f,.13f,.76f,.94f),new Color(.45f,.55f,.58f));artImage.preserveAspect=true;Ref(so,"representative",artImage);T("TemporaryArt",art,"교체 가능한 임시 대표 이미지",18,.02f,.02f,.98f,.16f);
        Ref(so,"requiredText",T("RequiredLevel",panel,"",24,.37f,.50f,.97f,.55f));var diffs=so.FindProperty("difficultyButtons");diffs.arraySize=4;
        for(int i=0;i<4;i++){var b=B("Difficulty"+i,panel,DungeonFloorData.DifficultyName(i),.36f+i*.155f,.42f,.505f+i*.155f,.49f);Border(b.transform,"SelectionBorder",new Color(.15f,.85f,.76f));diffs.GetArrayElementAtIndex(i).objectReferenceValue=b;}
        T("ChestTitle",panel,"클리어 상자 보상",25,.36f,.365f,.67f,.415f);Ref(so,"previewStatus",T("PreviewStatus",panel,"",17,.36f,.325f,.97f,.37f));
        var viewport=R("RewardViewport",panel,.36f,.14f,.97f,.32f);I(viewport,new Color(.025f,.04f,.055f),true);viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var content=R("Content",viewport,0,0,0,1);content.pivot=new Vector2(0,.5f);var layout=content.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=12;layout.padding=new RectOffset(6,6,4,4);layout.childForceExpandWidth=false;layout.childControlWidth=true;layout.childControlHeight=true;var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;scroll.viewport=viewport;Ref(so,"rewardContent",content);Ref(so,"rewardScroll",scroll);
        var card=R("CardTemplate",content,0,0,0,1);I(card,new Color(.10f,.13f,.17f),true);var le=card.gameObject.AddComponent<LayoutElement>();le.preferredWidth=165;le.minWidth=165;Border(card,"Border",Color.gray);var icon=I(R("Icon",card,.3f,.32f,.7f,.82f),new Color(.7f,.8f,.8f));icon.preserveAspect=true;T("Marker",card,"",34,.3f,.32f,.7f,.82f);T("Name",card,"",19,.02f,.08f,.98f,.30f);T("Chance",card,"",20,.04f,.80f,.96f,.99f);card.gameObject.AddComponent<MineRewardHover>();Ref(so,"rewardCardTemplate",card.gameObject);card.gameObject.SetActive(false);
        Ref(so,"currentLevelText",T("CurrentLevel",panel,"",23,.36f,.025f,.67f,.095f));var enter=B("Enter",panel,"",.75f,.025f,.97f,.105f);Ref(so,"enterButton",enter);Ref(so,"entryLabel",T("Label",enter.transform,"입장하기",28,0,0,1,1));Ref(so,"notice",T("EntryNotice",panel,"",21,.57f,.105f,.98f,.14f));so.ApplyModifiedPropertiesWithoutUndo();panel.gameObject.SetActive(false);
        canvas.transform.Find("InventoryTooltip").GetComponentInChildren<TMP_Text>(true).font=font;
        var retry=canvas.transform.Find("DungeonResultPanel/ResultCard/RetryButton/RetryLabel").GetComponent<TMP_Text>();retry.font=font;retry.text="메인으로 돌아가기";
        AssetDatabase.SaveAssets();var scene=canvas.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity",true);
        var builds=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};builds.AddRange(EditorBuildSettings.scenes.Where(s=>s.path!="Assets/Scenes/Main.unity"));EditorBuildSettings.scenes=builds.ToArray();EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");return "Main and TestRoom migrated; entry scene Main";
    }
}
#endif
