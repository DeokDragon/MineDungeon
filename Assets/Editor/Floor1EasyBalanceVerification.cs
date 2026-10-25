using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit, isolated automated probe. This is not a human playtime/balance certification.
[InitializeOnLoad]
public static class Floor1EasyBalanceVerification
{
    const string Prefix="EasyBalance.", Override="MineDungeon.SaveTestPath";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly int[,] Counts={{3,2,1,2,1},{2,2,2,2,1},{3,1,2,2,1},{3,2,2,1,1},{3,2,2,2,1},{2,3,1,2,1},{3,2,1,2,1},{2,1,2,3,1},{4,2,1,2,1},{3,2,2,2,1}};
    public static string ApplyInitial()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var floor=AssetDatabase.LoadAssetAtPath<DungeonFloorData>("Assets/Dungeons/Floor1_RockMine.asset");
        var source=floor.monsterProfile;var draft=MonsterProfileEditorStore.Draft(source);var easy=draft.GetDifficulty(0);
        var changes=new List<MonsterProfileEditorStore.Change>();var temps=new List<UnityEngine.Object>{draft};var report=new List<string>();
        try
        {
            report.Add("Before floor HP/ATK "+source.hpMultiplier+"/"+source.attackMultiplier+"; easy HP/ATK "+easy.hpMultiplier+"/"+easy.attackMultiplier);
            if(source.hpMultiplier!=1||source.attackMultiplier!=1)throw new Exception("Shared floor multipliers need review");
            easy.hpMultiplier=1;easy.attackMultiplier=.8f;easy.adjustments=new MonsterFloorProfile.Adjustment[5];
            easy.experienceConfigured=true;easy.experience=new[]{4,5,5,4,6};easy.bossExperience=45;
            easy.bossBalanceConfigured=true;easy.bossHealth=800;easy.bossDamage=new[]{14,16,18,12,14,8,12,10,12};
            for(int i=0;i<10;i++)
            {
                var map=source.GetDifficulty(0).maps[i];var edit=MonsterProfileEditorStore.Draft(map);temps.Add(edit);
                var templates=new EnemyHealth[5];foreach(var entry in map.entries)templates[(int)MonsterProfileRuntime.Kind(entry.template)]=entry.template;
                int[] have=new int[5];var kept=new List<MonsterMapProfile.Entry>();var spare=new Queue<MonsterMapProfile.Entry>();
                foreach(var entry in edit.entries){int k=(int)MonsterProfileRuntime.Kind(entry.template);if(have[k]<Counts[i,k]){have[k]++;kept.Add(entry);}else spare.Enqueue(entry);}
                for(int k=0;k<5;k++)while(have[k]<Counts[i,k]){var entry=spare.Dequeue();entry.template=templates[k];entry.embeddedPath="";kept.Add(entry);have[k]++;}
                edit.entries=kept.ToArray();var validation=MonsterProfileEditorStore.ValidateDraft(edit);
                if(validation.Errors.Count>0)throw new Exception(map.name+": "+string.Join(";",validation.Errors));
                report.Add(map.name+" "+string.Join("/",have.Select(n=>n.ToString()).ToArray())+" EXP="+draft.ExpectedExperience(0,edit)+" errors0; warnings="+string.Join(";",validation.Warnings));
                changes.Add(new MonsterProfileEditorStore.Change(edit,map,MonsterProfileEditorStore.Json(map)));
            }
            foreach(var entry in source.GetDifficulty(0).maps[0].entries.GroupBy(e=>MonsterProfileRuntime.Kind(e.template)).Select(g=>g.First()))
            {draft.Calculate(0,entry.template,out int hp,out int damage);report.Add(MonsterProfileRuntime.Kind(entry.template)+" before="+entry.template.MaxHealth+"/"+MonsterProfileRuntime.Attack(entry.template)+" after="+hp+"/"+damage);}
            changes.Add(new MonsterProfileEditorStore.Change(draft,source,MonsterProfileEditorStore.Json(source)));
            MonsterProfileEditorStore.SaveMany(changes.ToArray());
            var root=SessionState.GetString(Prefix+"Root","");File.WriteAllLines(root+"/applied.txt",report);
            return "1F easy profile and10 independent layouts applied; spawn validation errors0. "+root;
        }
        finally{foreach(var temp in temps)UnityEngine.Object.DestroyImmediate(temp);}
    }
    static Floor1EasyBalanceVerification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
    static EasyBalanceInputProbe input;
    static PlayerHealth player;static PlayerAttack attack;static PlayerMovement movement;static DungeonRoomManager rooms;static DungeonRunManager run;
    static List<string> events;static Dictionary<int,float> firstHit;static HashSet<int> registered;
    static float start,roomStart,bossStart,p2Start,nextStatus;static int number,frames,slowFrames;static float maxFrame;static bool qLast,spaceLast;
    static bool manual, functional; static Dictionary<string,Vector3> performance;
    static bool safer; static StoneGolemBossAI.Pattern lastBossPattern;
    static DungeonRoom lastRoom;static EnemyHealth target; static int lastFrame=-1; static Vector3 lastPosition; static float movedAt;
    static object Get(object o,string f)=>o.GetType().GetField(f,Fields).GetValue(o);
    public static string Start(string label)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SelectedMapTestSafety.Active)throw new Exception("Edit mode and no selected test required");
        if(string.IsNullOrEmpty(label)||label.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new Exception("Safe unique test label required");
        var root=SessionState.GetString(Prefix+"Root","");if(string.IsNullOrEmpty(root)){root=Path.GetFullPath("Library/Floor1EasyBalance/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);SessionState.SetString(Prefix+"Root",root);}
        var owned=new DirectoryInfo(root);Guid ownedId;
        if(owned.Parent.FullName!=Path.GetFullPath("Library/Floor1EasyBalance")||!Guid.TryParseExact(owned.Name,"N",out ownedId))throw new Exception("Owned Library root required");
        SessionState.SetFloat(Prefix+"OldTimeScale",Time.timeScale);SessionState.SetBool(Prefix+"OldAudio",AudioListener.pause);
        SessionState.SetBool(Prefix+"Manual",false);
        string path=root+"/"+label+"/character.json";Directory.CreateDirectory(Path.GetDirectoryName(path));
        if(File.Exists(path))throw new Exception("New label required");
        ProgressionVerification.WriteFixture(path,new CharacterSnapshot());
        SessionState.SetString(Prefix+"OldOverride",SessionState.GetString(Override,""));SessionState.SetBool(Prefix+"OldOptions",EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetBool(Prefix+"OldBackground",Application.runInBackground);SessionState.SetString(Prefix+"OldStart",EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"");
        SessionState.SetString(Prefix+"Label",label);SessionState.SetString(Prefix+"Path",path);SessionState.SetString(Prefix+"State","queued");

        SessionState.SetString(Override,path);EditorSettings.enterPlayModeOptionsEnabled=false;Application.runInBackground=true;
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");EditorApplication.isPlaying=true;
        return "Queued isolated automation "+label;
    }
    [MenuItem("Tools/MineDungeon/검증/1층 쉬움 수동 격리 플레이")]
    public static void StartHuman()
    {
        Start("manual-"+Guid.NewGuid().ToString("N").Substring(0,8));SessionState.SetBool(Prefix+"Manual",true);
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.ExitingPlayMode&&SessionState.GetBool(Prefix+"Manual",false)&&SessionState.GetString(Prefix+"State","")=="running")Finish("manual_stopped");
        if(state!=PlayModeStateChange.EnteredEditMode||string.IsNullOrEmpty(SessionState.GetString(Prefix+"State","")))return;
        SessionState.SetString(Override,SessionState.GetString(Prefix+"OldOverride",""));EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool(Prefix+"OldOptions",false);
        Application.runInBackground=SessionState.GetBool(Prefix+"OldBackground",false);var old=SessionState.GetString(Prefix+"OldStart","");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(old)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(old);
        Time.timeScale=SessionState.GetFloat(Prefix+"OldTimeScale",1);AudioListener.pause=SessionState.GetBool(Prefix+"OldAudio",false);
        SessionState.SetString(Prefix+"State","");
    }
    static void Setup()
    {
        run=DungeonRunManager.Instance;rooms=UnityEngine.Object.FindFirstObjectByType<DungeonRoomManager>();player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        if(run==null||rooms==null||player==null||!rooms.EditorTestReady)return;
        if(CharacterSaveService.Current.SavePath!=SessionState.GetString(Prefix+"Path",""))throw new Exception("Save isolation mismatch");
        attack=player.GetComponent<PlayerAttack>();movement=player.GetComponent<PlayerMovement>();
        var p=new SerializedObject(attack);p.FindProperty("attackDamage").intValue=12;p.ApplyModifiedPropertiesWithoutUndo();
        var powers=new SerializedObject(player.GetComponent<PlayerSkillAttackPower>());powers.FindProperty("physicalAttack").floatValue=0;powers.FindProperty("magicAttack").floatValue=0;powers.ApplyModifiedPropertiesWithoutUndo();
        if(attack.GetComponent<PlayerSkills>().Equipped(0)!=null||attack.GetComponent<PlayerSkills>().Equipped(1)!=null||run.GetComponent<PlayerProgression>().Level!=1)throw new Exception("Character fixture mismatch");
        if(Get(attack,"weaponPivot")==null)throw new Exception("Player weapon pivot unavailable");
        manual=SessionState.GetBool(Prefix+"Manual",false);functional=SessionState.GetString(Prefix+"Label","").Contains("functional");
        if(!manual){input=player.gameObject.AddComponent<EasyBalanceInputProbe>();input.Frame=Step;}
        performance=new Dictionary<string,Vector3>();
        events=new List<string>{"mode=post-Update actor command probe, timeScale1; NOT HUMAN PLAYTIME unless manual flag", "character=Lv1 points3 unspent; HP="+player.MaxHealth+" speed="+movement.FinalMoveSpeed+" base12 physical0 magic0 no skills; potions="+run.RemainingPotions+" revives="+run.RemainingRevives};
        safer=SessionState.GetString(Prefix+"Label","").Contains("reviewed");lastBossPattern=StoneGolemBossAI.Pattern.None;
        events.Add("manual="+manual+" functional_invincible="+functional);
        events.Add("strategy="+(safer?"reviewed defensive timing; no balance/character change":"baseline early-dodge probe"));
        player.Damaged+=()=>{var b=rooms.CurrentRoom?rooms.CurrentRoom.GetComponentInChildren<StoneGolemBossAI>():null;events.Add("player_hit,room="+rooms.CurrentRoomNumber+",time="+(Time.time-start)+",HP="+player.CurrentHealth+",boss="+(b?b.ActivePattern.ToString():"none")+",enhanced="+(b&&b.IsEnhanced));};
        run.ResourcesChanged+=()=>events.Add("resources,time="+(Time.time-start)+",HP="+player.CurrentHealth+",potions="+run.RemainingPotions+",revives="+run.RemainingRevives);
        firstHit=new Dictionary<int,float>();registered=new HashSet<int>();lastRoom=null;target=null;frames=slowFrames=0;maxFrame=0;bossStart=p2Start=0;
        UnityEngine.Random.InitState(62501);start=Time.time;roomStart=start;lastPosition=player.transform.position;movedAt=start;lastFrame=-1;
        if(!rooms.StartDungeon(AssetDatabase.LoadAssetAtPath<DungeonFloorData>("Assets/Dungeons/Floor1_RockMine.asset"),0))throw new Exception("Floor start refused");
        UnityEngine.Object.FindFirstObjectByType<MineEntranceUI>()?.CompleteEntry();
        if(functional)player.GrantInvincibility(10000);
        SessionState.SetString(Prefix+"State","running");
    }
    static void Register(DungeonRoom room)
    {
        lastRoom=room;number=rooms.CurrentRoomNumber;roomStart=Time.time;
        events.Add("room_start,"+number+","+room.name+","+room.GetComponentsInChildren<EnemyHealth>().Count(e=>!e.IsDead)+","+(Time.time-start)+",HP="+player.CurrentHealth+",potions="+run.RemainingPotions+",revives="+run.RemainingRevives);
        foreach(var hp in room.GetComponentsInChildren<EnemyHealth>())
        {
            int id=hp.GetInstanceID();registered.Add(id);
            hp.Damaged+=e=>{if(!firstHit.ContainsKey(id))firstHit[id]=Time.time;};
            hp.Died+=e=>{events.Add("kill,"+number+","+e.name+",first_hit_to_death="+(firstHit.ContainsKey(id)?(Time.time-firstHit[id]).ToString("F3"):"unobserved")+",elapsed="+(Time.time-start));};
        }
        room.Controller.Cleared+=c=>{events.Add("room_clear,"+number+","+room.name+",seconds="+(Time.time-roomStart));if(number==4)events.Add("boss_phase_times,p1="+(p2Start>0?p2Start-bossStart:0)+",p2="+(p2Start>0?Time.time-p2Start:0)+",total="+(Time.time-bossStart));};
        if(number==4){bossStart=Time.time;var boss=room.GetComponentInChildren<StoneGolemBossAI>();events.Add("boss_hp="+boss.GetComponent<EnemyHealth>().MaxHealth);}
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        string current=SessionState.GetString(Prefix+"State","");
        if(current=="running"&&SessionState.GetBool(Prefix+"Manual",false)){Step();return;}
        if(current!="queued")return;
        try{Setup();}catch(Exception e){if(events==null)events=new List<string>();events.Add("ERROR "+e);Finish("error:"+e.Message);}
    }
    static void Step()
    {
        var state=SessionState.GetString(Prefix+"State","");if(!EditorApplication.isPlaying||string.IsNullOrEmpty(state))return;
        try
        {
            if(state=="queued"){Setup();return;}if(state!="running"||run==null)return;
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;frames++;maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime);if(Time.unscaledDeltaTime>.05f)slowFrames++;
            if(Time.timeScale>0 && Time.time-start>2 && !rooms.IsTransitioning && !player.CombatLocked)
            {
                string action=movement.IsDodging?"dodge":attack.IsAttacking?"attack":player.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude>.01f?"move":"idle/manual";
                Vector3 sample;performance.TryGetValue(action,out sample);sample.x++;sample.y+=Time.unscaledDeltaTime;sample.z=Mathf.Max(sample.z,Time.unscaledDeltaTime);performance[action]=sample;
            }
            if(run.IsRunEnded){Finish(run.IsRunCleared?"cleared":"failed");return;}
            if(SessionState.GetString(Prefix+"Label","").StartsWith("perf-")&&Time.time-start>=30){Finish("30s Editor performance sample; not balance completion");return;}
            if(Time.time-start>720){Finish("timeout");return;}
            if(rooms.IsTransitioning||rooms.CurrentRoom==null||player.IsDead||player.CombatLocked){Feed(Vector2.zero,false,false,false,player.transform.position);return;}
            var room=rooms.CurrentRoom;if(lastRoom!=room)Register(room);
            var boss=room.GetComponentInChildren<StoneGolemBossAI>();if(boss!=null&&boss.Phase==2&&p2Start==0){p2Start=Time.time;events.Add("phase2,"+(p2Start-start));}
            if(manual){SessionState.SetString(Prefix+"Status","manual room="+number+" elapsed="+(Time.time-start).ToString("F1")+" HP="+player.CurrentHealth);return;}
            var enemies=room.GetComponentsInChildren<EnemyHealth>().Where(e=>!e.IsDead).ToArray();
            if(enemies.Length==0)
            {var exit=(RoomExit)new SerializedObject(room).FindProperty("roomExit").objectReferenceValue;Vector2 dir=room.GetComponent<RoomWalkableLayout>().Direction(player.transform.position,exit.transform.position,.42f);Feed(dir,false,false,false,exit.transform.position);return;}
            if(target==null||target.IsDead||target.GetComponentInParent<DungeonRoom>()!=room)target=enemies.OrderBy(e=>Vector2.Distance(player.transform.position,e.transform.position)).First();
            Vector2 delta=target.transform.position-player.transform.position;float radius=target.GetComponent<CircleCollider2D>()?target.GetComponent<CircleCollider2D>().radius*Mathf.Abs(target.transform.lossyScale.x):.5f;
            if(Vector3.Distance(lastPosition,player.transform.position)>.08f){lastPosition=player.transform.position;movedAt=Time.time;}
            if(Time.time-movedAt>90 && delta.magnitude>radius+2){Finish("bot_navigation_stalled");return;}
            var layout=room.GetComponent<RoomWalkableLayout>();Vector2 direction=delta.magnitude>radius+1.15f?layout.Direction(player.transform.position,target.transform.position,.42f):Vector2.zero;
            bool dodge=boss!=null&&(boss.State==StoneGolemBossAI.BossState.Warning||boss.State==StoneGolemBossAI.BossState.SecondaryWarning)&&delta.magnitude<6&&!movement.IsDodging&&!spaceLast;
            if(dodge)direction=-delta.normalized;
            if(safer)
            {
                bool canDodge=!movement.IsDodging&&!spaceLast&&player.CombatTime>=(float)Get(movement,"nextDodgeTime");
                dodge=false;
                if(boss!=null)
                {
                    if(boss.ActivePattern!=lastBossPattern){lastBossPattern=boss.ActivePattern;events.Add("boss_pattern,time="+(Time.time-start)+",phase="+boss.Phase+",pattern="+boss.ActivePattern+",enhanced="+boss.IsEnhanced+",queued="+boss.QueuedPattern);}
                    var bossState=boss.State;var pattern=boss.ActivePattern;float timer=(float)Get(boss,"timer");
                    Vector2 tangent=new Vector2(-delta.y,delta.x).normalized;
                    bool warning=bossState==StoneGolemBossAI.BossState.Warning||bossState==StoneGolemBossAI.BossState.SecondaryWarning;
                    if(pattern==StoneGolemBossAI.Pattern.LeapSlam&&(warning||bossState==StoneGolemBossAI.BossState.Jump))
                    {
                        direction=bossState==StoneGolemBossAI.BossState.SecondaryWarning?-delta.normalized:tangent;
                        dodge=canDodge&&bossState==StoneGolemBossAI.BossState.Jump&&timer>(float)Get(boss,"leapDuration")-.18f;
                    }
                    else if(pattern==StoneGolemBossAI.Pattern.GroundSmash&&warning)
                    {
                        float safe=(float)Get(boss,"smashRadius")*(boss.IsEnhanced?(float)Get(boss,"secondaryRadiusMultiplier"):1f)+.8f;
                        direction=delta.magnitude<safe?-delta.normalized:Vector2.zero;
                        dodge=canDodge&&delta.magnitude<safe&&timer>(float)Get(boss,"smashWarning")-.2f;
                    }
                    else if(pattern==StoneGolemBossAI.Pattern.RockCharge&&(warning||bossState==StoneGolemBossAI.BossState.Charge))
                    {direction=tangent;dodge=canDodge&&warning&&timer>(float)Get(boss,"chargeWarning")-.22f;}
                    else if(pattern==StoneGolemBossAI.Pattern.StoneFan&&(warning||bossState==StoneGolemBossAI.BossState.FanFollowup))
                    {direction=tangent;dodge=canDodge&&timer>(float)Get(boss,"fanWarning")-.18f;}
                    else if(pattern==StoneGolemBossAI.Pattern.Rockfall){direction=tangent;}
                    if(!layout.ContainsBody((Vector2)player.transform.position+direction*.7f,.42f))
                    {var alternative=-direction;if(layout.ContainsBody((Vector2)player.transform.position+alternative*.7f,.42f))direction=alternative;}
                }
                else
                {
                    var stone=target.GetComponent<StoneMonsterAI>();var coal=target.GetComponent<CoalMonsterAI>();
                    if(delta.magnitude<2.6f&&((stone&&stone.IsJumping)||(coal&&coal.IsAttacking)))
                    {direction=-delta.normalized;dodge=canDodge;}
                }
            }
            bool potion=player.CurrentHealth<=65&&run.RemainingPotions>0&&!attack.IsAttacking&&!movement.IsDodging&&!qLast;
            Feed(direction,!potion&&!dodge&&(boss==null||!safer||delta.magnitude<radius+1.6f),potion,dodge,target.transform.position);
            if(Time.time>nextStatus){nextStatus=Time.time+5;SessionState.SetString(Prefix+"Status","room="+number+" remaining="+room.Controller.RemainingEnemies+" HP="+player.CurrentHealth+" time="+(Time.time-start).ToString("F1")+" boss="+(boss?boss.GetComponent<EnemyHealth>().CurrentHealth:0));}
        }
        catch(Exception e){if(events!=null)events.Add("ERROR "+e);Finish("error:"+e.Message);}
    }
    static void Feed(Vector2 direction,bool hit,bool potion,bool dodge,Vector3 aim)
    {
        if(input==null)return;
        input.Move=direction;input.Aim=(Vector2)aim-(Vector2)player.transform.position;
        input.Held=hit;input.Potion=potion;input.Dodge=dodge;qLast=potion;spaceLast=dodge;
    }
    static void Finish(string outcome)
    {
        if(SessionState.GetString(Prefix+"State","")=="finished")return;SessionState.SetString(Prefix+"State","finished");
        if(events==null)events=new List<string>();events.Add("outcome="+outcome);
        if(run!=null&&player!=null){events.Add("elapsed="+(Time.time-start)+" potions="+(3-run.RemainingPotions)+" revives="+(2-run.RemainingRevives)+" HP="+player.CurrentHealth);if(run.Result!=null)events.Add("run_result="+JsonUtility.ToJson(run.Result));if(rooms&&rooms.CurrentRoom){var b=rooms.CurrentRoom.GetComponentInChildren<StoneGolemBossAI>();if(b)events.Add("boss_remaining="+b.GetComponent<EnemyHealth>().CurrentHealth+" phase="+b.Phase+" boss_elapsed="+(Time.time-bossStart));}events.Add("exp="+run.GetComponent<RewardInventorySession>().RunRewardExperience+" finalLevel="+run.GetComponent<PlayerProgression>().Level);}
        events.Add("Editor frame samples="+frames+" >50ms="+slowFrames+" max="+maxFrame+" (Editor automation overhead; not player build FPS)");
        if(performance!=null)foreach(var pair in performance)events.Add("frame_action="+pair.Key+",count="+pair.Value.x+",mean_ms="+(pair.Value.y/Mathf.Max(1,pair.Value.x)*1000)+",max_ms="+(pair.Value.z*1000));
        var folder=Path.GetDirectoryName(SessionState.GetString(Prefix+"Path",""));File.WriteAllLines(folder+"/measurement.txt",events);SessionState.SetString(Prefix+"Status",outcome+"; "+folder);
        if(input!=null){input.Frame=null;input.enabled=false;}
        CharacterSaveService.Current?.Flush();if(!manual)EditorApplication.isPlaying=false;
    }
}
