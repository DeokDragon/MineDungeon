using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

// Editor-only integration probe. Uses real Play Mode physics and existing run/room APIs.
// CharacterSaveService's existing SessionState test-path hook isolates the user's save.
public sealed class MineMapPlayProbe
{
    public static string Status = "Not run";
    public static readonly List<string> Results = new List<string>();
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    PlayerHealth player;
    Rigidbody2D rb;
    DungeonRoomManager manager;
    RoomWalkableLayout layout;
    string mapId;
    bool previousBackground;
    void Check(bool condition,string label)
    {
        Results.Add((condition?"PASS ":"FAIL ")+label);
        if(!condition) throw new InvalidOperationException(label);
    }
    static FieldInfo Field(Type t,string name)
    {
        while(t!=null) { var f=t.GetField(name,Flags);if(f!=null)return f;t=t.BaseType; }throw new Exception(name);
    }
    static void Put(object o,string name,object value)=>Field(o.GetType(),name).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Flags).Invoke(o,args);
    public static void Begin(string id)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var save=CharacterSaveService.Current;
        string testRoot=System.IO.Path.GetFullPath("Library")+System.IO.Path.DirectorySeparatorChar;
        if(save==null || !System.IO.Path.GetFullPath(save.SavePath).StartsWith(testRoot,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure MineDungeon.SaveTestPath under Library before entering Play Mode.");
        Results.Clear();Status="Starting "+id;
        var probe=new MineMapPlayProbe();probe.mapId=id;probe.previousBackground=PlayerSettings.runInBackground;
        FindFirstObjectByType<DungeonRoomManager>().StartCoroutine(probe.Guarded(probe.Run()));
    }
    IEnumerator Guarded(IEnumerator routine)
    {
        while(true)
        {
            object next;
            try { if(!routine.MoveNext())break;next=routine.Current; }
            catch(Exception e) { Status="FAILED "+mapId+": "+e;RestoreEnvironment();SaveReport();yield break; }
            yield return next;
        }
        Status="COMPLETE "+mapId;RestoreEnvironment();SaveReport();
    }
    void RestoreEnvironment()
    {
        Time.timeScale=1;
        Application.runInBackground=previousBackground;
        PlayerSettings.runInBackground=previousBackground;
    }
    void SaveReport()
    {
        System.IO.Directory.CreateDirectory("Library/MapValidation");
        System.IO.File.WriteAllText("Library/MapValidation/Map"+mapId+"-play.txt",Status+"\n"+string.Join("\n",Results));
    }
    void Teleport(Vector2 p)
    {
        rb.position=p;player.transform.position=p;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
    }
    void FreezeEnemies(bool freeze)
    {
        foreach(var mb in layout.GetComponentsInChildren<MonoBehaviour>())
            if(mb is IMonsterAI ai) { ai.CancelAttacks();mb.enabled=!freeze; }
        foreach(var body in layout.GetComponentsInChildren<Rigidbody2D>())body.linearVelocity=Vector2.zero;
    }
    IEnumerator Run()
    {
        Time.timeScale=4;
        Application.runInBackground=true;
        player=FindFirstObjectByType<PlayerHealth>();rb=player.GetComponent<Rigidbody2D>();
        manager=FindFirstObjectByType<DungeonRoomManager>();
        var prefab=AssetDatabase.LoadAssetAtPath<DungeonRoom>(MineMapBuilder.Folder+"/Map_"+mapId+".prefab");
        var floor=Instantiate(AssetDatabase.LoadAssetAtPath<DungeonFloorData>("Assets/Dungeons/Floor1_RockMine.asset"));
        var setting=floor.GetDifficulty(0);setting.normalRooms=new[]{prefab};setting.requiredLevel=1;setting.normalRoomCount=3;
        Check(manager.StartDungeon(floor,0),"StartDungeon existing API");
        // Mirror MineEntranceUI.Enter: its lobby colliders must leave with the lobby visuals.
        var entranceUI=FindFirstObjectByType<MineEntranceUI>();
        ((GameObject)Field(typeof(MineEntranceUI),"lobbyRoot").GetValue(entranceUI)).SetActive(false);
        yield return null;
        layout=FindFirstObjectByType<RoomWalkableLayout>();
        Check(layout!=null && layout.GetComponentsInChildren<EnemyHealth>().Length==6,"Six enemies total");
        player.GrantInvincibility(1000);
        var movement=player.GetComponent<PlayerMovement>();movement.enabled=false;
        FreezeEnemies(true);
        var entrance=layout.GetComponent<DungeonRoom>().Entrance.position;
        Check(Vector2.Distance(rb.position,entrance)<.01f,"Spawn at current entrance");
        var enemies=layout.GetComponentsInChildren<EnemyHealth>();
        var saved=new Vector2[enemies.Length];
        for(int i=0;i<enemies.Length;i++)
        {
            saved[i]=enemies[i].transform.position;
            Check(layout.ContainsBody(saved[i],.65f) && layout.Reachable(entrance,saved[i],.65f),"Spawn clearance and reachability "+i);
            for(int j=0;j<i;j++)Check(Vector2.Distance(saved[i],saved[j])>1.4f,"Spawn separation "+i+"/"+j);
        }
        var targets=new List<Vector2>();
        foreach(var e in enemies)e.gameObject.SetActive(false);
        foreach(var region in layout.monsterAreas)targets.Add(layout.transform.TransformPoint(region.center));
        targets.Add((Vector2)layout.roomExit.transform.position-Vector2.up*1.5f);
        foreach(var destination in targets)
        {
            Status="Walking "+mapId+" -> "+destination;
            float until=Time.time+60;
            while(Vector2.Distance(rb.position,destination)>.4f && Time.time<until)
            {
                rb.linearVelocity=layout.Direction(rb.position,destination,.45f)*4;
                yield return new WaitForFixedUpdate();
                CheckFloorQuiet();
            }
            rb.linearVelocity=Vector2.zero;
            Check(Vector2.Distance(rb.position,destination)<.5f,"Physics walk to "+destination);
        }
        // Actual PlayerMovement normal and dodge code, aimed into the bottom boundary.
        Teleport((Vector2)entrance+Vector2.down);
        Put(movement,"moveInput",Vector2.down);
        for(int i=0;i<70;i++) { Call(movement,"FixedUpdate");yield return new WaitForFixedUpdate(); }
        Check(layout.ContainsBody(rb.position,.39f),"Normal movement stopped at rock");
        Call(movement,"StartDodge");
        for(int i=0;i<20;i++) { Call(movement,"FixedUpdate");yield return new WaitForFixedUpdate(); }
        Check(layout.ContainsBody(rb.position,.39f),"Dodge stopped at rock");
        Teleport(entrance);
        // Each species uses its normal chase loop around the pocket's lower inner corner.
        for(int i=0;i<enemies.Length;i++)
        {
            var enemy=enemies[i];
            Status="AI detour "+mapId+" "+enemy.name;
            foreach(var e in enemies)e.gameObject.SetActive(e==enemy);
            enemy.transform.position=saved[i];
            enemy.GetComponent<Rigidbody2D>().position=saved[i];Physics2D.SyncTransforms();
            foreach(var mb in enemy.GetComponents<MonoBehaviour>())if(mb is IMonsterAI ai){ai.SetTarget(player);mb.enabled=true;}
            var normal=enemy.GetComponent<NormalMonsterAI>();
            float approach=normal ? (float)Field(typeof(NormalMonsterAI),"attackDistance").GetValue(normal)+.15f : 3f;
            float until=Time.time+50;
            while((Vector2.Distance(enemy.transform.position,entrance)>approach || Physics2D.Linecast(enemy.transform.position,entrance,LayerMask.GetMask("Wall"))) && Time.time<until)
            {
                yield return new WaitForFixedUpdate();
                if(!layout.ContainsBody(enemy.transform.position,.48f))throw new Exception("AI crossed rock "+enemy.name);
            }
            Check(Vector2.Distance(enemy.transform.position,entrance)<=approach && !Physics2D.Linecast(enemy.transform.position,entrance,LayerMask.GetMask("Wall")),"Live chase reaches unobstructed attack range "+enemy.name);
            foreach(var mb in enemy.GetComponents<MonoBehaviour>())if(mb is IMonsterAI ai){ai.CancelAttacks();mb.enabled=false;}
        }
        foreach(var e in enemies)e.gameObject.SetActive(true);
        FreezeEnemies(true);
        if(mapId=="A")
        {
            // The player is on the opposite side of the entire crane foundation.
            Teleport(new Vector2(-11,0));
            foreach(var e in enemies)e.gameObject.SetActive(false);
            var opponent=enemies[0];opponent.gameObject.SetActive(true);
            opponent.transform.position=new Vector2(11,0);opponent.GetComponent<Rigidbody2D>().position=new Vector2(11,0);Physics2D.SyncTransforms();
            foreach(var mb in opponent.GetComponents<MonoBehaviour>())if(mb is IMonsterAI ai){ai.SetTarget(player);mb.enabled=true;}
            float deadline=Time.time+55;
            while(Vector2.Distance(opponent.transform.position,rb.position)>3 && Time.time<deadline)
            { yield return new WaitForFixedUpdate();CheckFloorEnemy(opponent,.49f); }
            Check(Vector2.Distance(opponent.transform.position,rb.position)<=3,"Crane opposite-side live detour");
            foreach(var e in enemies)e.gameObject.SetActive(true);FreezeEnemies(true);Teleport(entrance);
        }
        if(mapId=="02")
        {
            for(int option=0;option<2;option++)
            {
                layout.SelectExit(option);Physics2D.SyncTransforms();
                Check(layout.unusedExitBarriers[1-option].activeSelf && !layout.unusedExitBarriers[option].activeSelf,"Exactly one exit candidate enabled "+option);
                Check(!layout.ContainsBody(layout.exitCandidates[1-option],.4f),"Unused exit blocked "+option);
                Check(layout.Reachable(entrance,layout.exitCandidates[option]-Vector2.up*1.5f,.65f),"Selected exit reachable "+option);
            }
        }
        if(mapId=="06")
        {
            var attacks=AttackChecks(enemies);
            while(attacks.MoveNext())yield return attacks.Current;
        }
        // Camera: viewport corners under smoothing, shake, zoom and several aspect ratios.
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        float oldAspect=cam.aspect, oldSize=cam.orthographicSize;
        var room=layout.GetComponent<DungeonRoom>();
        foreach(float aspect in new[]{4f/3,16f/9,21f/9,4f})
        foreach(float size in new[]{3f,4.5f,12f})
        foreach(Vector2 point in new[]{room.WorldMin,room.WorldMax,new Vector2(room.WorldMin.x,room.WorldMax.y),new Vector2(room.WorldMax.x,room.WorldMin.y),(Vector2)entrance,(Vector2)layout.roomExit.transform.position})
        {
            cam.aspect=aspect;cam.orthographicSize=size;Teleport(point);follow.Shake(.8f,.5f);
            yield return null;
            Vector2 lo=cam.ViewportToWorldPoint(new Vector3(0,0,10)),hi=cam.ViewportToWorldPoint(new Vector3(1,1,10));
            Check(lo.x>=room.WorldMin.x-.001f && lo.y>=room.WorldMin.y-.001f && hi.x<=room.WorldMax.x+.001f && hi.y<=room.WorldMax.y+.001f,"Camera "+aspect+"/"+size+"/"+point);
        }
        cam.aspect=oldAspect;cam.orthographicSize=oldSize;Teleport(entrance);follow.SnapToTarget();
        // Existing defeat coroutine and current-room respawn; no direct revive invocation.
        Status="Revive and progression "+mapId;
        if(mapId=="06")
        {
            StartPendingPlayerAttack();
            var crystal=layout.GetComponentInChildren<CrystalMonsterAI>();crystal.enabled=true;crystal.SetTarget(player);
            crystal.StartCoroutine((IEnumerator)Call(crystal,"Projectile",(Vector2)crystal.transform.position,Vector2.up));
            Check(player.GetComponent<PlayerSkills>().ActiveEffectCount>0,"Pending player attack exists before defeat");
        }
        Put(player,"invincibleUntil",0f);player.TryTakeDamage(100000);
        yield return new WaitForSeconds(1.2f);
        Check(!player.IsDead && Vector2.Distance(rb.position,entrance)<.05f && DungeonRunManager.Instance.RemainingRevives==1,"First revive at current entrance");
        if(mapId=="06")
        {
            Check(player.GetComponent<PlayerSkills>().ActiveEffectCount==0,"Defeat clears player attack effects");
            var crystal=layout.GetComponentInChildren<CrystalMonsterAI>();crystal.enabled=false;
            Check((Transform)Field(typeof(NormalMonsterAI),"effects").GetValue(crystal)==null,"Defeat clears owned crystal projectile");
        }
        Vector3 snapped=cam.transform.position;follow.SnapToTarget();
        Check(Vector3.Distance(snapped,cam.transform.position)<.01f,"Revive camera already snapped");
        player.GrantInvincibility(1000);
        Put(player,"currentHealth",73);
        int potions=DungeonRunManager.Instance.RemainingPotions;
        var rewards=DungeonRunManager.Instance.GetComponent<RewardInventorySession>();
        foreach(var enemy in enemies)
        {
            enemy.TakeDamage(int.MaxValue);
            long gold=rewards.RunMoney,xp=rewards.RunRewardExperience,total=0;
            foreach(var stack in rewards.Materials)total+=stack.Quantity;
            enemy.TakeDamage(int.MaxValue);
            long after=0;foreach(var stack in rewards.Materials)after+=stack.Quantity;
            Check(gold==rewards.RunMoney && xp==rewards.RunRewardExperience && total==after,"Duplicate death grants no extra reward "+enemy.name);
        }
        yield return null;
        Check(room.IsCleared && layout.roomExit.IsOpen && room.Controller.RemainingEnemies==0,"All six deaths open existing exit");
        long runGold=rewards.RunMoney,runXp=rewards.RunRewardExperience;
        if(mapId=="06")
        {
            Put(manager,"normalRoomPrefabs",new[]{AssetDatabase.LoadAssetAtPath<DungeonRoom>(MineMapBuilder.Folder+"/Map_04.prefab")});
            StartPendingPlayerAttack();
        }
        Teleport(layout.roomExit.transform.position);
        yield return new WaitForFixedUpdate();yield return null;yield return null;
        Check(manager.CurrentRoomNumber==2,"Existing exit trigger advances next room");
        Check(DungeonRunManager.Instance.Result.kills==6,"Duplicate death attempts counted once; run kills persist");
        Check(rewards.RunMoney==runGold && rewards.RunRewardExperience==runXp,"Run gold and XP persist across room");
        Check(player.CurrentHealth==73 && DungeonRunManager.Instance.RemainingPotions==potions && DungeonRunManager.Instance.RemainingRevives==1,"HP potions revives persist across room");
        layout=FindFirstObjectByType<RoomWalkableLayout>();FreezeEnemies(true);
        if(mapId=="06")
        {
            Check(layout.name.StartsWith("Map_04"),"Heterogeneous Map06 to Map04 transition");
            Check(player.GetComponent<PlayerSkills>().ActiveEffectCount==0,"Room transition clears pending player attack");
        }
        entrance=layout.GetComponent<DungeonRoom>().Entrance.position;
        Put(player,"invincibleUntil",0f);player.TryTakeDamage(100000);
        yield return new WaitForSeconds(1.2f);
        Check(!player.IsDead && Vector2.Distance(rb.position,entrance)<.05f && DungeonRunManager.Instance.RemainingRevives==0,"Second revive at new room entrance");
        Put(player,"invincibleUntil",0f);player.TryTakeDamage(100000);
        yield return new WaitForSeconds(1.2f);
        Check(DungeonRunManager.Instance.IsRunFailed && DungeonRunManager.Instance.RemainingRevives==0,"Third defeat ends run; two revives maximum");
    }
    void CheckFloorQuiet()
    {
        if(!layout.ContainsBody(rb.position,.39f))throw new Exception("Player crossed blocking rock at "+rb.position);
    }
    IEnumerator AttackChecks(EnemyHealth[] enemies)
    {
        Status="Attack wall sweeps 06";
        foreach(var e in enemies)e.gameObject.SetActive(false);
        var skills=player.GetComponent<PlayerSkills>();
        SkillData original=skills.Equipped(0), drill=null;
        foreach(var guid in AssetDatabase.FindAssets("t:SkillData"))
        { var data=AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));if(data.kind==PlayerSkillKind.DrillDash){drill=data;break;} }
        Check(drill!=null,"Existing drill skill asset found");
        Put(skills,"eSkill",drill);skills.ResetForNewRun();Teleport(new Vector2(0,-19));
        Check(skills.TryCast(0,new Vector2(0,-30)),"Existing drill TryCast launched");
        yield return new WaitForSeconds(2);
        Check(layout.ContainsBody(rb.position,.39f) && rb.position.y>-20,"Player drill stops at rock");
        skills.CancelAll();Put(skills,"eSkill",original);Teleport(new Vector2(0,-17));
        foreach(var e in enemies)
        {
            var iron=e.GetComponent<IronMonsterAI>();
            if(!iron)continue;
            e.gameObject.SetActive(true);iron.enabled=false;iron.SetTarget(player);
            var body=e.GetComponent<Rigidbody2D>();body.position=new Vector2(-10,0);e.transform.position=body.position;
            Put(iron,"origin",body.position);Put(iron,"direction",Vector2.left);
            var attack=(IEnumerator)Call(iron,"Attack");
            while(attack.MoveNext())
            {
                yield return attack.Current;
                CheckFloorEnemy(e,.59f);
                if(Mathf.Abs(body.position.y)>.01f)throw new Exception("Iron charge curved");
            }
            Check(body.position.x<-12 && body.position.x>-13.5f,"Iron straight charge terminates at wall");
            iron.CancelAttacks();e.gameObject.SetActive(false);
        }
        foreach(var e in enemies)
        {
            var stone=e.GetComponent<StoneMonsterAI>();if(!stone)continue;
            e.gameObject.SetActive(true);stone.enabled=false;stone.SetTarget(player);
            var body=e.GetComponent<Rigidbody2D>();body.position=new Vector2(0,-8);e.transform.position=body.position;Physics2D.SyncTransforms();
            Put(stone,"jumpStartPosition",body.position);Put(stone,"landingPosition",new Vector2(-6,-8));Put(stone,"timer",0f);
            // Force a wall-crossing requested landing to exercise the existing jump body sweep.
            for(int i=0;i<20;i++){Call(stone,"Jump");yield return new WaitForFixedUpdate();CheckFloorEnemy(e,.49f);}
            Check(body.position.x>-2.51f,"Stone jump sweep rejects wall crossing");
            stone.CancelAttacks();e.gameObject.SetActive(false);break;
        }
        foreach(var e in enemies)
        {
            var crystal=e.GetComponent<CrystalMonsterAI>();if(!crystal)continue;
            e.gameObject.SetActive(true);crystal.enabled=false;crystal.SetTarget(player);
            var body=e.GetComponent<Rigidbody2D>();body.position=new Vector2(0,-8);e.transform.position=body.position;Physics2D.SyncTransforms();
            var shot=(IEnumerator)Call(crystal,"Projectile",body.position,Vector2.left);
            int steps=0;float furthest=0;
            while(shot.MoveNext())
            {
                yield return shot.Current;steps++;
                var effects=(Transform)Field(typeof(NormalMonsterAI),"effects").GetValue(crystal);
                if(effects)foreach(var t in effects.GetComponentsInChildren<StoneGolemTelegraph>())furthest=Mathf.Min(furthest,t.transform.position.x);
                if(steps>100)throw new Exception("Projectile did not terminate");
            }
            Check(steps<30 && furthest>=-3.01f,"Crystal projectile terminates at rock before full range");
            crystal.CancelAttacks();e.gameObject.SetActive(false);
        }
        foreach(var e in enemies)e.gameObject.SetActive(true);
        FreezeEnemies(true);Teleport(new Vector2(0,-17));
    }
    void CheckFloorEnemy(EnemyHealth e,float radius)
    {
        if(!layout.ContainsBody(e.transform.position,radius))throw new Exception("Attack crossed rock: "+e.name);
    }
    void StartPendingPlayerAttack()
    {
        var skills=player.GetComponent<PlayerSkills>();skills.ResetForNewRun();
        foreach(var guid in AssetDatabase.FindAssets("t:SkillData"))
        {
            var data=AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if(data.kind!=PlayerSkillKind.OreBlast)continue;
            Put(skills,"eSkill",data);
            Check(skills.TryCast(0,rb.position+Vector2.up*3),"Pending ore blast launched");return;
        }
        throw new Exception("Existing ore blast asset missing");
    }
}
